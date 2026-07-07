using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ONITalk.Core;
using ONITalk.LocalizationSupport;

namespace ONITalk.Infrastructure {
    internal sealed class ProviderValidationResult {
        internal ProviderValidationResult(bool success, string message) {
            Success = success;
            Message = message;
        }

        internal bool Success { get; }

        internal string Message { get; }
    }

    internal static class ProviderConnectionValidator {
        internal static async Task<ProviderValidationResult> ValidateAsync(
                ProviderConfiguration source, CancellationToken cancellationToken) {
            ProviderConfiguration settings = ProviderProfileCatalog.Normalize(source);
            ProviderProfile profile = ProviderProfileCatalog.Get(settings.Provider);
            if (profile.IsOffline)
                return new ProviderValidationResult(true,
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.VALIDATION.OFFLINE_VALID));
            if (string.IsNullOrWhiteSpace(settings.Endpoint))
                return new ProviderValidationResult(false,
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.VALIDATION.ENDPOINT_REQUIRED));
            if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out Uri? endpoint) ||
                    (endpoint.Scheme != Uri.UriSchemeHttp &&
                    endpoint.Scheme != Uri.UriSchemeHttps))
                return new ProviderValidationResult(false,
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.VALIDATION.ENDPOINT_INVALID));
            if (string.IsNullOrWhiteSpace(settings.Model))
                return new ProviderValidationResult(false,
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.VALIDATION.MODEL_REQUIRED));
            if (profile.ApiKeyRequired && string.IsNullOrWhiteSpace(settings.ApiKey))
                return new ProviderValidationResult(false, ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.VALIDATION.API_KEY_REQUIRED,
                    ProviderConfigurationOptionsEntry.GetProfileName(profile)));

            var config = new ONITalkConfig {
                Provider = settings.Provider,
                Endpoint = settings.Endpoint,
                Model = settings.Model,
                ApiKey = settings.ApiKey,
                RequestTimeoutSeconds = 15,
                Temperature = 0f,
                MaxTokens = 8
            };
            config.Normalize();
            using (var client = new OpenAICompatibleClient(config))
                return await client.ValidateAsync(cancellationToken);
        }
    }

    internal sealed class OpenAICompatibleClient : IDisposable {
        private readonly ONITalkConfig config;
        private readonly HttpClient http;
        private readonly SemaphoreSlim requestGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private int disposed;

        internal OpenAICompatibleClient(ONITalkConfig config) {
            this.config = config;
            http = new HttpClient {
                Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds)
            };
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            shutdown.Cancel();
            http.Dispose();
        }

        /// <summary>
        /// Generates one line. Returns null instead of queueing another paid request when a
        /// previous request is still running.
        /// </summary>
        internal async Task<string?> TryGenerateAsync(string systemPrompt, string userPrompt,
                CancellationToken cancellationToken) {
            if (Volatile.Read(ref disposed) != 0)
                throw new ObjectDisposedException(nameof(OpenAICompatibleClient));

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, shutdown.Token)) {
                bool entered = await requestGate.WaitAsync(0, linked.Token).ConfigureAwait(false);
                if (!entered)
                    return null;

                try {
                    ProviderReply reply = await SendAsync(systemPrompt, userPrompt,
                        config.MaxTokens, linked.Token).ConfigureAwait(false);
                    return reply.Content;
                } finally {
                    requestGate.Release();
                }
            }
        }

        internal async Task<ProviderValidationResult> ValidateAsync(
                CancellationToken cancellationToken) {
            var timer = Stopwatch.StartNew();
            try {
                ProviderReply reply = await SendAsync(
                    "这是 ONITalk 的连接测试。只回复 OK。",
                    "只回复两个字母：OK", 8, cancellationToken).ConfigureAwait(false);
                timer.Stop();
                string model = string.IsNullOrWhiteSpace(reply.Model)
                    ? config.Model
                    : reply.Model;
                return new ProviderValidationResult(true,
                    ONITalkLocalization.Format(STRINGS.ONITALK.UI.VALIDATION.SUCCESS,
                        model, timer.ElapsedMilliseconds));
            } catch (OperationCanceledException) {
                return new ProviderValidationResult(false,
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.VALIDATION.CANCELLED));
            } catch (Exception error) {
                return new ProviderValidationResult(false, ToSafeMessage(error));
            }
        }

        private async Task<ProviderReply> SendAsync(string systemPrompt,
                string userPrompt, int maximumTokens,
                CancellationToken cancellationToken) {
            JObject payload = BuildPayload(systemPrompt, userPrompt, maximumTokens);
            using (var request = new HttpRequestMessage(HttpMethod.Post,
                    config.Endpoint)) {
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer", config.ApiKey);
                request.Content = new StringContent(payload.ToString(Formatting.None),
                    Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await http.SendAsync(request,
                        cancellationToken).ConfigureAwait(false)) {
                    string body = await response.Content.ReadAsStringAsync().
                        ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("HTTP " +
                            (int)response.StatusCode + "：" + ReadProviderError(body));

                    JObject root;
                    try {
                        root = JObject.Parse(body);
                    } catch (JsonException error) {
                        throw new InvalidOperationException(
                            ONITalkLocalization.Get(
                                STRINGS.ONITALK.UI.VALIDATION.INVALID_JSON), error);
                    }
                    string? content = ReadMessageContent(root["choices"]?[0]?
                        ["message"]?["content"]);
                    if (string.IsNullOrWhiteSpace(content))
                        throw new InvalidOperationException(
                            ONITalkLocalization.Get(
                                STRINGS.ONITALK.UI.VALIDATION.EMPTY_RESPONSE));
                    return new ProviderReply(content, (string?)root["model"]);
                }
            }
        }

        private JObject BuildPayload(string systemPrompt, string userPrompt,
                int maximumTokens) {
            ProviderProfile profile = ProviderProfileCatalog.Get(config.Provider);
            var payload = new JObject {
                ["model"] = config.Model,
                ["messages"] = new JArray {
                    new JObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JObject { ["role"] = "user", ["content"] = userPrompt }
                },
                ["temperature"] = Math.Min(profile.MaximumTemperature,
                    config.Temperature),
                ["max_tokens"] = Math.Max(1, maximumTokens),
                ["stream"] = false
            };
            if (string.Equals(config.Provider, "deepseek",
                    StringComparison.OrdinalIgnoreCase))
                payload["thinking"] = new JObject { ["type"] = "disabled" };
            return payload;
        }

        private string ToSafeMessage(Exception error) {
            string message;
            if (error is HttpRequestException)
                message = error.Message;
            else if (error is InvalidOperationException)
                message = error.Message;
            else
                message = ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.VALIDATION.CONNECTION_FAILED,
                    error.GetType().Name);
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                message = message.Replace(config.ApiKey, "***");
            return message.Length <= 240 ? message : message.Substring(0, 240) + "…";
        }

        private static string ReadProviderError(string body) {
            try {
                JObject root = JObject.Parse(body);
                string? message = (string?)root["error"]?["message"] ??
                    (string?)root["message"];
                if (!string.IsNullOrWhiteSpace(message))
                    return message.Trim();
            } catch (JsonException) {
            }
            return ONITalkLocalization.Get(
                STRINGS.ONITALK.UI.VALIDATION.PROVIDER_REJECTED);
        }

        private static string? ReadMessageContent(JToken? token) {
            if (token == null)
                return null;
            if (token.Type == JTokenType.String)
                return (string?)token;
            if (token is JArray parts) {
                var text = new StringBuilder();
                foreach (JToken part in parts) {
                    string? value = (string?)part["text"];
                    if (!string.IsNullOrWhiteSpace(value))
                        text.Append(value);
                }
                return text.ToString();
            }
            return null;
        }

        private sealed class ProviderReply {
            internal ProviderReply(string content, string? model) {
                Content = content;
                Model = model ?? string.Empty;
            }

            internal string Content { get; }

            internal string Model { get; }
        }
    }
}
