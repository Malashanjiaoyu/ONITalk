using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ONITalk.Core;
using ONITalk.LocalizationSupport;
using ONITalk.Runtime;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace ONITalk.Infrastructure {
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("ONITalk.json", IndentOutput: true, SharedConfigLocation: true)]
    public sealed class ONITalkConfig : IOptions {
        private const string FileName = "ONITalk.json";
        private const int CurrentSchemaVersion = 4;
        internal const string DeepSeekEndpoint =
            "https://api.deepseek.com/chat/completions";
        internal const string DeepSeekModel = "deepseek-v4-flash";

        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonProperty("interfaceLanguage")]
        [JsonConverter(typeof(InterfaceLanguageConverter))]
        [Option("STRINGS.ONITALK.OPTIONS.INTERFACE_LANGUAGE.NAME",
            "STRINGS.ONITALK.OPTIONS.INTERFACE_LANGUAGE.TOOLTIP")]
        public InterfaceLanguage InterfaceLanguage { get; set; } =
            InterfaceLanguage.FollowGame;

        [JsonProperty("enabled")]
        [Option("STRINGS.ONITALK.OPTIONS.ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.GENERAL")]
        public bool Enabled { get; set; } = true;

        [JsonIgnore]
        [DynamicOption(typeof(PromptConfigurationOptionsEntry))]
        [Option("STRINGS.ONITALK.OPTIONS.PROMPT_CONFIGURATION.NAME",
            "STRINGS.ONITALK.OPTIONS.PROMPT_CONFIGURATION.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.PROMPT")]
        public PromptConfiguration PromptConfiguration {
            get => new PromptConfiguration { Enabled = CustomPromptEnabled,
                Template = CustomPrompt };
            set { PromptConfiguration next = value ?? new PromptConfiguration();
                CustomPromptEnabled = next.Enabled; CustomPrompt = next.Template; }
        }

        [JsonProperty("customPromptEnabled")]
        public bool CustomPromptEnabled { get; set; }

        [JsonProperty("customPrompt")]
        public string CustomPrompt { get; set; } = PromptCustomization.StarterTemplate;

        [JsonIgnore]
        public bool UseDeepSeek {
            get => string.Equals(Provider, "deepseek", StringComparison.OrdinalIgnoreCase);
            set => Provider = value ? "deepseek" : "echo";
        }

        [JsonIgnore]
        [DynamicOption(typeof(ProviderConfigurationOptionsEntry))]
        [Option("STRINGS.ONITALK.OPTIONS.PROVIDER_CONFIGURATION.NAME",
            "STRINGS.ONITALK.OPTIONS.PROVIDER_CONFIGURATION.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.PROVIDER")]
        public ProviderConfiguration ProviderConfiguration {
            get => new ProviderConfiguration {
                Provider = Provider,
                Endpoint = Endpoint,
                Model = Model,
                ApiKey = ApiKey
            };
            set {
                ProviderConfiguration normalized = ProviderProfileCatalog.Normalize(value);
                Provider = normalized.Provider;
                Endpoint = normalized.Endpoint;
                Model = normalized.Model;
                ApiKey = normalized.ApiKey;
            }
        }

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = DeepSeekEndpoint;

        [JsonProperty("model")]
        public string Model { get; set; } = DeepSeekModel;

        [JsonProperty("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonProperty("language")]
        public string Language { get; set; } = "简体中文";

        [JsonProperty("ambientChatterIntervalSeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.AMBIENT_CHATTER_INTERVAL_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.AMBIENT_CHATTER_INTERVAL_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.FREQUENCY")]
        [Limit(10, 600)]
        public float AmbientChatterIntervalSeconds { get; set; } = 60f;

        [JsonProperty("pairCooldownSeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.PAIR_COOLDOWN_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.PAIR_COOLDOWN_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.FREQUENCY")]
        [Limit(5, 600)]
        public int PairCooldownSeconds { get; set; } = 45;

        [JsonProperty("globalCooldownSeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.GLOBAL_COOLDOWN_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.GLOBAL_COOLDOWN_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.FREQUENCY")]
        [Limit(1, 120)]
        public int GlobalCooldownSeconds { get; set; } = 8;

        [JsonProperty("conversationRepliesEnabled")]
        [Option("STRINGS.ONITALK.OPTIONS.CONVERSATION_REPLIES_ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.CONVERSATION_REPLIES_ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CONTINUOUS_DIALOGUE")]
        public bool ConversationRepliesEnabled { get; set; } = true;

        [JsonProperty("replyChancePercent")]
        [Option("STRINGS.ONITALK.OPTIONS.REPLY_CHANCE_PERCENT.NAME",
            "STRINGS.ONITALK.OPTIONS.REPLY_CHANCE_PERCENT.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CONTINUOUS_DIALOGUE")]
        [Limit(0, 100)]
        public int ReplyChancePercent { get; set; } = 100;

        [JsonProperty("maxConversationLines")]
        [Option("STRINGS.ONITALK.OPTIONS.MAX_CONVERSATION_LINES.NAME",
            "STRINGS.ONITALK.OPTIONS.MAX_CONVERSATION_LINES.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CONTINUOUS_DIALOGUE")]
        [Limit(1, 3)]
        public int MaxConversationLines { get; set; } = 2;

        [JsonProperty("replyDelaySeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.REPLY_DELAY_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.REPLY_DELAY_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CONTINUOUS_DIALOGUE")]
        [Limit(0.5, 5.0, 0.5)]
        public float ReplyDelaySeconds { get; set; } = 1.5f;

        [JsonProperty("testingMode")]
        [Option("STRINGS.ONITALK.OPTIONS.TESTING_MODE.NAME",
            "STRINGS.ONITALK.OPTIONS.TESTING_MODE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        public bool TestingMode { get; set; }

        [JsonProperty("testingChatterIntervalSeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.TESTING_CHATTER_INTERVAL_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.TESTING_CHATTER_INTERVAL_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(2, 60)]
        public float TestingChatterIntervalSeconds { get; set; } = 5f;

        [JsonProperty("maxConversationDistance")]
        [Option("STRINGS.ONITALK.OPTIONS.MAX_CONVERSATION_DISTANCE.NAME",
            "STRINGS.ONITALK.OPTIONS.MAX_CONVERSATION_DISTANCE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(2, 30)]
        public float MaxConversationDistance { get; set; } = 12f;

        [JsonProperty("requestTimeoutSeconds")]
        [Option("STRINGS.ONITALK.OPTIONS.REQUEST_TIMEOUT_SECONDS.NAME",
            "STRINGS.ONITALK.OPTIONS.REQUEST_TIMEOUT_SECONDS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(5, 60)]
        public int RequestTimeoutSeconds { get; set; } = 20;

        [JsonProperty("temperature")]
        [Option("STRINGS.ONITALK.OPTIONS.TEMPERATURE.NAME",
            "STRINGS.ONITALK.OPTIONS.TEMPERATURE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(0, 2, 0.1)]
        public float Temperature { get; set; } = 0.9f;

        [JsonProperty("maxCharacters")]
        [Option("STRINGS.ONITALK.OPTIONS.MAX_CHARACTERS.NAME",
            "STRINGS.ONITALK.OPTIONS.MAX_CHARACTERS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(20, 160)]
        public int MaxCharacters { get; set; } = 80;

        [JsonProperty("maxTokens")]
        [Option("STRINGS.ONITALK.OPTIONS.MAX_TOKENS.NAME",
            "STRINGS.ONITALK.OPTIONS.MAX_TOKENS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ADVANCED")]
        [Limit(20, 300)]
        public int MaxTokens { get; set; } = 100;

        [JsonProperty("chatWindowEnabled")]
        [Option("STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHAT_WINDOW")]
        public bool ChatWindowEnabled { get; set; } = true;

        [JsonProperty("chatHistoryLimit")]
        [Option("STRINGS.ONITALK.OPTIONS.CHAT_HISTORY_LIMIT.NAME",
            "STRINGS.ONITALK.OPTIONS.CHAT_HISTORY_LIMIT.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHAT_WINDOW")]
        [Limit(20, 300)]
        public int ChatHistoryLimit { get; set; } = 100;

        [JsonProperty("chatFontSize")]
        [Option("STRINGS.ONITALK.OPTIONS.CHAT_FONT_SIZE.NAME",
            "STRINGS.ONITALK.OPTIONS.CHAT_FONT_SIZE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHAT_WINDOW")]
        [Limit(10, 24)]
        public int ChatFontSize { get; set; } = 14;

        [JsonProperty("chatWindowOpacity")]
        [Option("STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_OPACITY.NAME",
            "STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_OPACITY.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHAT_WINDOW")]
        [Limit(0.05, 1.0, 0.05)]
        public float ChatWindowOpacity { get; set; } = 0.22f;

        [JsonProperty("chatWindowAutoFade")]
        [Option("STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_AUTO_FADE.NAME",
            "STRINGS.ONITALK.OPTIONS.CHAT_WINDOW_AUTO_FADE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHAT_WINDOW")]
        public bool ChatWindowAutoFade { get; set; } = true;

        [JsonProperty("chatWindowX")]
        public float ChatWindowX { get; set; } = 0.02f;

        [JsonProperty("chatWindowY")]
        public float ChatWindowY { get; set; } = 0.55f;

        [JsonProperty("chatWindowWidth")]
        public float ChatWindowWidth { get; set; } = 0.30f;

        [JsonProperty("chatWindowHeight")]
        public float ChatWindowHeight { get; set; } = 0.34f;

        [JsonProperty("chatWindowMinimized")]
        public bool ChatWindowMinimized { get; set; }

        [JsonProperty("persistentMemoryEnabled")]
        [Option("STRINGS.ONITALK.OPTIONS.PERSISTENT_MEMORY_ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.PERSISTENT_MEMORY_ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHARACTER_MEMORY")]
        public bool PersistentMemoryEnabled { get; set; } = true;

        [JsonProperty("memoryLinesPerPair")]
        [Option("STRINGS.ONITALK.OPTIONS.MEMORY_LINES_PER_PAIR.NAME",
            "STRINGS.ONITALK.OPTIONS.MEMORY_LINES_PER_PAIR.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHARACTER_MEMORY")]
        [Limit(4, 20)]
        public int MemoryLinesPerPair { get; set; } = 8;

        [JsonProperty("memoryMaxPairs")]
        [Option("STRINGS.ONITALK.OPTIONS.MEMORY_MAX_PAIRS.NAME",
            "STRINGS.ONITALK.OPTIONS.MEMORY_MAX_PAIRS.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.CHARACTER_MEMORY")]
        [Limit(20, 500)]
        public int MemoryMaxPairs { get; set; } = 200;

        [JsonProperty("actionMemoryEnabled")]
        [Option("STRINGS.ONITALK.OPTIONS.ACTION_MEMORY_ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.ACTION_MEMORY_ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ACTION_MEMORY")]
        public bool ActionMemoryEnabled { get; set; } = true;

        [JsonProperty("actionMemoryPromptLimit")]
        public int ActionMemoryPromptLimit { get; set; } = 3;

        [JsonProperty("actionMemoryCapacityPerDupe")]
        [Option("STRINGS.ONITALK.OPTIONS.ACTION_MEMORY_CAPACITY_PER_DUPE.NAME",
            "STRINGS.ONITALK.OPTIONS.ACTION_MEMORY_CAPACITY_PER_DUPE.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ACTION_MEMORY")]
        [Limit(10, 100)]
        public int ActionMemoryCapacityPerDupe { get; set; } = 30;

        [JsonProperty("actionAggregationWindowCycles")]
        [Option("STRINGS.ONITALK.OPTIONS.ACTION_AGGREGATION_WINDOW_CYCLES.NAME",
            "STRINGS.ONITALK.OPTIONS.ACTION_AGGREGATION_WINDOW_CYCLES.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ACTION_MEMORY")]
        [Limit(0, 10)]
        public int ActionAggregationWindowCycles { get; set; } = 2;

        [JsonProperty("majorEventMemoryEnabled")]
        [Option("STRINGS.ONITALK.OPTIONS.MAJOR_EVENT_MEMORY_ENABLED.NAME",
            "STRINGS.ONITALK.OPTIONS.MAJOR_EVENT_MEMORY_ENABLED.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.ACTION_MEMORY")]
        public bool MajorEventMemoryEnabled { get; set; } = true;

        [JsonProperty("majorEventPromptLimit")]
        public int MajorEventPromptLimit { get; set; } = 3;

        [JsonProperty("memoryInjectionPreset")]
        [JsonConverter(typeof(StringEnumConverter))]
        [DynamicOption(typeof(MemoryPresetOptionsEntry))]
        [Option("STRINGS.ONITALK.OPTIONS.MEMORY_PRESET.NAME",
            "STRINGS.ONITALK.OPTIONS.MEMORY_PRESET.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.MEMORY_INJECTION")]
        public MemoryInjectionPreset MemoryPreset { get; set; } =
            MemoryInjectionPreset.平衡;

        [JsonProperty("memoryTokenBudget")]
        [Option("STRINGS.ONITALK.OPTIONS.MEMORY_TOKEN_BUDGET.NAME",
            "STRINGS.ONITALK.OPTIONS.MEMORY_TOKEN_BUDGET.TOOLTIP",
            "STRINGS.ONITALK.CATEGORIES.MEMORY_INJECTION")]
        [Limit(100, 1600)]
        public int MemoryTokenBudget { get; set; } = 520;

        [JsonProperty("diagnosticBubbleOnGameStart")]
        public bool DiagnosticBubbleOnGameStart { get; set; }

        [JsonProperty("stateScanIntervalSeconds")]
        public float StateScanIntervalSeconds { get; set; } = 2f;

        [JsonProperty("lowBreathThresholdPercent")]
        public float LowBreathThresholdPercent { get; set; } = 35f;

        [JsonProperty("highStressThresholdPercent")]
        public float HighStressThresholdPercent { get; set; } = 70f;

        [JsonProperty("highTemperatureC")]
        public float HighTemperatureC { get; set; } = 45f;

        [JsonProperty("provider")]
        public string Provider { get; set; } = "deepseek";

        internal static string ConfigPath => POptions.GetConfigFilePath(typeof(ONITalkConfig));

        private static string LegacyConfigPath =>
            Path.Combine(KMod.Manager.GetDirectory(), FileName);

        public IEnumerable<IOptionsEntry> CreateOptions() {
            yield break;
        }

        public void OnOptionsChanged() {
            Normalize();
            ONITalkLocalization.Configure(InterfaceLanguage);
            ONITalkService.Initialize(this);
            ONITalkController.ApplyOptions(this);
            Log.Info("Options saved and applied. Provider=" + Provider +
                ", model=" + Model + ", customPromptEnabled=" +
                CustomPromptEnabled + ", API key configured=" +
                (!string.IsNullOrWhiteSpace(ApiKey)) + ".");
        }

        internal void Save() {
            try {
                Normalize();
                Write(ConfigPath, this);
            } catch (Exception error) {
                Log.Warning("Could not save config. " + error.Message);
            }
        }

        internal static ONITalkConfig LoadOrCreate() {
            string path = ConfigPath;
            string sourcePath = path;
            bool migrateLegacy = !File.Exists(path) && File.Exists(LegacyConfigPath);
            if (migrateLegacy)
                sourcePath = LegacyConfigPath;

            try {
                if (File.Exists(sourcePath)) {
                    string json = File.ReadAllText(sourcePath);
                    JObject root = JObject.Parse(json);
                    bool upgraded = UpgradeJson(root, out int previousSchema);
                    ONITalkConfig? loaded = root.ToObject<ONITalkConfig>();
                    if (loaded != null) {
                        loaded.Normalize();
                        if (migrateLegacy || upgraded) {
                            if (upgraded)
                                TryBackup(sourcePath, previousSchema);
                            Write(path, loaded);
                            Log.Info("Migrated config schema " + previousSchema + " -> " +
                                CurrentSchemaVersion + " at " + path);
                        } else {
                            Log.Info("Config loaded from " + path);
                        }
                        return loaded;
                    }
                }
            } catch (Exception error) {
                Log.Warning("Could not read config; using safe defaults. " + error.Message);
                return CreateDefault();
            }

            ONITalkConfig config = CreateDefault();
            try {
                Write(path, config);
                Log.Info("Created config at " + path);
            } catch (Exception error) {
                Log.Warning("Could not create config file. " + error.Message);
            }
            return config;
        }

        private static void Write(string path, ONITalkConfig config) {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private static bool UpgradeJson(JObject root, out int previousSchema) {
            previousSchema = (int?)root["schemaVersion"] ?? 1;
            if (previousSchema > CurrentSchemaVersion)
                return false;

            if (root["provider"] == null) {
                bool? useRemote = (bool?)root["useDeepSeek"] ??
                    (bool?)root["deepSeekEnabled"];
                root["provider"] = useRemote == false ? "echo" : "deepseek";
            }
            if (root["interfaceLanguage"] == null) {
                string legacyLanguage = ((string?)root["language"] ?? string.Empty).Trim();
                root["interfaceLanguage"] = legacyLanguage.IndexOf("English",
                        StringComparison.OrdinalIgnoreCase) >= 0 ? "English" :
                    legacyLanguage.IndexOf("西班牙", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    legacyLanguage.IndexOf("Espa", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "Spanish" :
                    legacyLanguage.IndexOf("中文", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "SimplifiedChinese" : "FollowGame";
            }
            if (root["memoryInjectionPreset"] is JValue preset &&
                    preset.Type == JTokenType.String) {
                string name = ((string?)preset ?? string.Empty).Trim().ToLowerInvariant();
                string? mapped = name == "light" ? "轻量" :
                    name == "balanced" ? "平衡" :
                    name == "rich" ? "丰富" :
                    name == "custom" ? "自定义" : null;
                if (mapped != null)
                    root["memoryInjectionPreset"] = mapped;
            }
            root["schemaVersion"] = CurrentSchemaVersion;
            return previousSchema < CurrentSchemaVersion;
        }

        private static void TryBackup(string sourcePath, int previousSchema) {
            try {
                string backup = sourcePath + ".pre-v" + CurrentSchemaVersion + ".bak";
                if (!File.Exists(backup))
                    File.Copy(sourcePath, backup, false);
            } catch (Exception error) {
                Log.Warning("Could not back up schema " + previousSchema +
                    " config before migration. " + error.Message);
            }
        }

        private static ONITalkConfig CreateDefault() {
            var config = new ONITalkConfig();
            config.Normalize();
            return config;
        }

        internal void Normalize() {
            SchemaVersion = CurrentSchemaVersion;
            if (!Enum.IsDefined(typeof(InterfaceLanguage), InterfaceLanguage))
                InterfaceLanguage = InterfaceLanguage.FollowGame;
            CustomPrompt = (CustomPrompt ?? string.Empty).Trim();
            if (CustomPrompt.Length > PromptCustomization.MaximumLength)
                CustomPrompt = CustomPrompt.Substring(0, PromptCustomization.MaximumLength);
            ProviderConfiguration provider = ProviderProfileCatalog.Normalize(
                new ProviderConfiguration {
                    Provider = Provider,
                    Endpoint = Endpoint,
                    Model = Model,
                    ApiKey = ApiKey
                });
            Provider = provider.Provider;
            Endpoint = provider.Endpoint;
            Model = provider.Model;
            ApiKey = provider.ApiKey;
            Language = string.IsNullOrWhiteSpace(Language) ? "简体中文" : Language.Trim();
            PairCooldownSeconds = Math.Max(5, PairCooldownSeconds);
            GlobalCooldownSeconds = Math.Max(1, GlobalCooldownSeconds);
            ReplyChancePercent = Math.Max(0, Math.Min(100, ReplyChancePercent));
            MaxConversationLines = Math.Max(1, Math.Min(3, MaxConversationLines));
            ReplyDelaySeconds = Math.Max(0.5f, Math.Min(5f, ReplyDelaySeconds));
            RequestTimeoutSeconds = Math.Max(5, RequestTimeoutSeconds);
            MaxCharacters = Math.Max(20, MaxCharacters);
            MaxTokens = Math.Max(20, MaxTokens);
            ChatHistoryLimit = Math.Max(20, Math.Min(300, ChatHistoryLimit));
            ChatFontSize = Math.Max(10, Math.Min(24, ChatFontSize));
            ChatWindowOpacity = Math.Max(0.05f, Math.Min(1f, ChatWindowOpacity));
            ChatWindowX = Math.Max(0f, Math.Min(1f, ChatWindowX));
            ChatWindowY = Math.Max(0f, Math.Min(1f, ChatWindowY));
            ChatWindowWidth = Math.Max(0.2f, Math.Min(0.8f, ChatWindowWidth));
            ChatWindowHeight = Math.Max(0.15f, Math.Min(0.8f, ChatWindowHeight));
            MemoryLinesPerPair = Math.Max(4, Math.Min(20, MemoryLinesPerPair));
            MemoryMaxPairs = Math.Max(20, Math.Min(500, MemoryMaxPairs));
            ActionMemoryPromptLimit = Math.Max(0, Math.Min(8,
                ActionMemoryPromptLimit));
            ActionMemoryCapacityPerDupe = Math.Max(10, Math.Min(100,
                ActionMemoryCapacityPerDupe));
            ActionAggregationWindowCycles = Math.Max(0, Math.Min(10,
                ActionAggregationWindowCycles));
            MajorEventPromptLimit = Math.Max(0, Math.Min(5, MajorEventPromptLimit));
            MemoryTokenBudget = SmartMemoryInjectionEngine.ResolveBudget(
                MemoryPreset, MemoryTokenBudget);
            Temperature = Math.Max(0f, Math.Min(
                ProviderProfileCatalog.Get(Provider).MaximumTemperature, Temperature));
            StateScanIntervalSeconds = Math.Max(0.5f, StateScanIntervalSeconds);
            AmbientChatterIntervalSeconds = Math.Max(StateScanIntervalSeconds,
                AmbientChatterIntervalSeconds);
            TestingChatterIntervalSeconds = Math.Max(2f, TestingChatterIntervalSeconds);
            MaxConversationDistance = Math.Max(2f, Math.Min(30f, MaxConversationDistance));
            LowBreathThresholdPercent = Math.Max(1f,
                Math.Min(100f, LowBreathThresholdPercent));
            HighStressThresholdPercent = Math.Max(1f,
                Math.Min(100f, HighStressThresholdPercent));
            HighTemperatureC = Math.Max(1f, Math.Min(200f, HighTemperatureC));
        }
    }
}
