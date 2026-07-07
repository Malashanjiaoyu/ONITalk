using System;
using System.Collections.Generic;
using System.Linq;

namespace ONITalk.Core {
    public sealed class ProviderProfile {
        internal ProviderProfile(string id, string displayName, string endpoint,
                string defaultModel, bool apiKeyRequired, float maximumTemperature,
                string description) {
            Id = id;
            DisplayName = displayName;
            Endpoint = endpoint;
            DefaultModel = defaultModel;
            ApiKeyRequired = apiKeyRequired;
            MaximumTemperature = maximumTemperature;
            Description = description;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Endpoint { get; }

        public string DefaultModel { get; }

        public bool ApiKeyRequired { get; }

        public float MaximumTemperature { get; }

        public string Description { get; }

        public bool IsOffline => string.Equals(Id, "echo",
            StringComparison.OrdinalIgnoreCase);

        public bool IsCustom => string.Equals(Id, "custom",
            StringComparison.OrdinalIgnoreCase);
    }

    public sealed class ProviderConfiguration {
        public string Provider { get; set; } = "deepseek";

        public string Endpoint { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public ProviderConfiguration Clone() {
            return new ProviderConfiguration {
                Provider = Provider ?? string.Empty,
                Endpoint = Endpoint ?? string.Empty,
                Model = Model ?? string.Empty,
                ApiKey = ApiKey ?? string.Empty
            };
        }
    }

    public static class ProviderProfileCatalog {
        private static readonly ProviderProfile[] Profiles = {
            new ProviderProfile("deepseek", "DeepSeek",
                "https://api.deepseek.com/chat/completions", "deepseek-v4-flash",
                true, 2f, "中文表现和性价比较好。"),
            new ProviderProfile("openai", "OpenAI",
                "https://api.openai.com/v1/chat/completions", "gpt-5-mini",
                true, 2f, "OpenAI 官方 Chat Completions 接口。"),
            new ProviderProfile("openrouter", "OpenRouter",
                "https://openrouter.ai/api/v1/chat/completions",
                "openai/gpt-5-mini", true, 2f,
                "使用一个接口选择多个厂商的模型。"),
            new ProviderProfile("gemini", "Google Gemini",
                "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                "gemini-3.5-flash", true, 2f,
                "Google Gemini 的 OpenAI 兼容接口。"),
            new ProviderProfile("siliconflow", "SiliconFlow 硅基流动",
                "https://api.siliconflow.cn/v1/chat/completions",
                "Qwen/Qwen3-8B", true, 2f,
                "国内可用的多模型兼容平台。"),
            new ProviderProfile("ollama", "Ollama 本地模型",
                "http://localhost:11434/v1/chat/completions", "qwen3:8b",
                false, 2f, "在本机运行模型，不需要真实 API Key。"),
            new ProviderProfile("bailian", "阿里云百炼 / 通义千问",
                "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                "qwen-plus", true, 2f,
                "可将地址改为对应地域和 Workspace 的专属 Endpoint。"),
            new ProviderProfile("groq", "Groq",
                "https://api.groq.com/openai/v1/chat/completions",
                "llama-3.3-70b-versatile", true, 2f,
                "强调低延迟的 OpenAI 兼容推理服务。"),
            new ProviderProfile("claude", "Anthropic Claude（兼容层）",
                "https://api.anthropic.com/v1/chat/completions",
                "claude-sonnet-4-6", true, 1f,
                "使用 Anthropic 官方 OpenAI 兼容层；高级能力后续再接原生 API。"),
            new ProviderProfile("custom", "自定义兼容接口", string.Empty,
                string.Empty, false, 2f,
                "适用于 Kimi、GLM、MiniMax、LM Studio 等兼容接口。"),
            new ProviderProfile("echo", "离线台词", string.Empty, string.Empty,
                false, 2f, "不调用任何远程接口，使用内置离线台词。")
        };

        public static IReadOnlyList<ProviderProfile> All => Profiles;

        public static ProviderProfile Get(string? id) {
            string normalized = NormalizeId(id);
            return Profiles.First(profile => string.Equals(profile.Id, normalized,
                StringComparison.OrdinalIgnoreCase));
        }

        public static string NormalizeId(string? id) {
            string normalized = string.IsNullOrWhiteSpace(id)
                ? "deepseek"
                : id.Trim().ToLowerInvariant();
            if (normalized == "openai-compatible")
                return "custom";
            return Profiles.Any(profile => string.Equals(profile.Id, normalized,
                    StringComparison.OrdinalIgnoreCase))
                ? normalized
                : "custom";
        }

        public static bool IsKnownEndpoint(string? value) {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            string normalized = value.Trim().TrimEnd('/');
            return Profiles.Any(profile => !string.IsNullOrWhiteSpace(profile.Endpoint) &&
                string.Equals(profile.Endpoint.TrimEnd('/'), normalized,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsKnownDefaultModel(string? value) {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return Profiles.Any(profile => !string.IsNullOrWhiteSpace(
                    profile.DefaultModel) && string.Equals(profile.DefaultModel,
                    value.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static ProviderConfiguration SwitchProfile(
                ProviderConfiguration? current, string nextProvider) {
            ProviderConfiguration result = current?.Clone() ??
                new ProviderConfiguration();
            ProviderProfile next = Get(nextProvider);
            result.Provider = next.Id;
            if (!next.IsCustom) {
                result.Endpoint = next.Endpoint;
                result.Model = next.DefaultModel;
            }
            return result;
        }

        public static ProviderConfiguration Normalize(ProviderConfiguration? source) {
            ProviderConfiguration result = source?.Clone() ??
                new ProviderConfiguration();
            ProviderProfile profile = Get(result.Provider);
            result.Provider = profile.Id;
            result.Endpoint = (result.Endpoint ?? string.Empty).Trim();
            result.Model = (result.Model ?? string.Empty).Trim();
            result.ApiKey = (result.ApiKey ?? string.Empty).Trim();
            if (!profile.IsCustom && !profile.IsOffline) {
                if (string.IsNullOrWhiteSpace(result.Endpoint))
                    result.Endpoint = profile.Endpoint;
                if (string.IsNullOrWhiteSpace(result.Model))
                    result.Model = profile.DefaultModel;
            }
            return result;
        }
    }
}
