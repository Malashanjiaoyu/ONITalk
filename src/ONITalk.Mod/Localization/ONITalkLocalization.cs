using System;
using System.Collections.Generic;
using System.IO;
using ONITalk.Core;
using ONITalk.Infrastructure;

namespace ONITalk.LocalizationSupport {
    internal static class ONITalkLocalization {
        private const string ExpectedPrefix = "STRINGS.ONITALK.";
        private const string SampleSuffix = "OPTIONS.ENABLED.NAME";

        private static readonly IReadOnlyDictionary<string, string> LanguageAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["zh-CN"] = "zh",
                ["zh-Hans"] = "zh",
                ["es-ES"] = "es",
                ["es-MX"] = "es"
            };

        private static string contentPath = string.Empty;
        private static InterfaceLanguage configuredLanguage = InterfaceLanguage.FollowGame;
        private static bool localizationReady;
        private static bool registered;

        internal static string DialogueLanguageName { get; private set; } = "English";

        internal static string Get(LocString value) {
            string key = value.key.String;
            return !string.IsNullOrEmpty(key) && Strings.TryGet(key, out StringEntry entry)
                ? entry.String
                : (string)value;
        }

        internal static string Format(LocString value, params object[] arguments) {
            return string.Format(Get(value), arguments);
        }

        internal static void Register(string modContentPath) {
            contentPath = modContentPath ?? string.Empty;
            if (registered)
                return;

            global::Localization.RegisterForTranslation(typeof(STRINGS));
            LocString.CreateLocStringKeys(typeof(STRINGS), null);
            registered = true;
        }

        internal static void ApplyCurrentLanguage() {
            if (!registered)
                return;

            localizationReady = true;
            ApplyConfiguredLanguage();
        }

        internal static void Configure(InterfaceLanguage language) {
            configuredLanguage = language;
            if (localizationReady)
                ApplyConfiguredLanguage();
        }

        private static void ApplyConfiguredLanguage() {

            try {
                string gameCode = global::Localization.GetCurrentLanguageCode();
                string code = ResolveLanguageCode(configuredLanguage, gameCode);
                string templatePath = Path.Combine(contentPath, "translations",
                    "template.pot");
                Dictionary<string, string> translations = LoadPoTranslations(
                    templatePath, true);
                string appliedCode = "en";

                if (!string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)) {
                    string path = Path.Combine(contentPath, "translations", code + ".po");
                    if (File.Exists(path)) {
                        Dictionary<string, string> localized = LoadPoTranslations(path,
                            false);
                        foreach (KeyValuePair<string, string> pair in localized)
                            translations[pair.Key] = pair.Value;
                        appliedCode = code;
                    } else {
                        Log.Info("No ONITalk translation for language=" + code +
                            "; using English fallback.");
                    }
                }
                DialogueLanguageName = appliedCode == "zh" ? "简体中文" :
                    appliedCode == "es" ? "Español" : "English";

                string actualSampleKey =
                    STRINGS.ONITALK.OPTIONS.ENABLED.NAME.key.String;
                AddLocStringAliases(translations, actualSampleKey);
                global::Localization.OverloadStrings(translations);
                foreach (KeyValuePair<string, string> pair in translations)
                    Strings.Add(pair.Key, pair.Value);
                CoreText.Resolver = ResolveCoreText;
                string resolvedSample = Strings.TryGet(actualSampleKey,
                    out StringEntry sampleEntry) ? sampleEntry.String : "<missing>";
                string resolvedRuntimeTitle = Get(STRINGS.ONITALK.UI.CHAT.TITLE);
                Log.Info("Localization active. requested=" + code +
                    ", applied=" + appliedCode +
                    ", entries=" + translations.Count +
                    ", sampleKey=" + actualSampleKey +
                    ", sampleValue=" + resolvedSample +
                    ", runtimeTitle=" + resolvedRuntimeTitle +
                    ", dialogueLanguage=" + DialogueLanguageName + ".");
            } catch (Exception error) {
                Log.Warning("Could not load localization; using English fallback. " +
                    error.Message);
            }
        }

        internal static string NormalizeLanguageCode(string code) {
            if (string.IsNullOrWhiteSpace(code))
                return "en";
            string trimmed = code.Trim();
            if (LanguageAliases.TryGetValue(trimmed, out string? mapped))
                return mapped;
            int separator = trimmed.IndexOfAny(new[] { '-', '_' });
            return separator > 0 ? trimmed.Substring(0, separator) : trimmed;
        }

        internal static string ResolveLanguageCode(InterfaceLanguage language,
                string gameCode) {
            switch (language) {
                case InterfaceLanguage.English:
                    return "en";
                case InterfaceLanguage.SimplifiedChinese:
                    return "zh";
                case InterfaceLanguage.Spanish:
                    return "es";
                default:
                    return NormalizeLanguageCode(gameCode);
            }
        }

        private static Dictionary<string, string> LoadPoTranslations(string path,
                bool useSourceText) {
            if (!File.Exists(path))
                throw new FileNotFoundException("Localization catalog was not found.", path);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            string? context = null;
            string? messageId = null;
            foreach (string rawLine in File.ReadAllLines(path)) {
                string line = rawLine.Trim();
                if (line.StartsWith("msgctxt ", StringComparison.Ordinal)) {
                    context = ReadQuotedValue(line.Substring(8));
                    messageId = null;
                } else if (context != null && line.StartsWith("msgid ",
                        StringComparison.Ordinal)) {
                    messageId = ReadQuotedValue(line.Substring(6));
                } else if (context != null && line.StartsWith("msgstr ",
                        StringComparison.Ordinal)) {
                    string value = ReadQuotedValue(line.Substring(7));
                    if (useSourceText && string.IsNullOrWhiteSpace(value))
                        value = messageId ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(value))
                        result[context] = value;
                    context = null;
                    messageId = null;
                }
            }
            if (result.Count == 0)
                throw new InvalidDataException("Translation file contains no usable entries.");
            return result;
        }

        private static string ReadQuotedValue(string value) {
            string trimmed = value.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '"' ||
                    trimmed[trimmed.Length - 1] != '"')
                return string.Empty;
            return trimmed.Substring(1, trimmed.Length - 2)
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        private static void AddLocStringAliases(
                IDictionary<string, string> translations, string actualSampleKey) {
            string expectedSampleKey = ExpectedPrefix + SampleSuffix;
            if (string.IsNullOrEmpty(actualSampleKey) ||
                    string.Equals(actualSampleKey, expectedSampleKey,
                        StringComparison.Ordinal) ||
                    !actualSampleKey.EndsWith(SampleSuffix, StringComparison.Ordinal))
                return;

            string actualPrefix = actualSampleKey.Substring(0,
                actualSampleKey.Length - SampleSuffix.Length);
            var snapshot = new List<KeyValuePair<string, string>>(translations);
            foreach (KeyValuePair<string, string> pair in snapshot) {
                if (pair.Key.StartsWith(ExpectedPrefix, StringComparison.Ordinal)) {
                    string alias = actualPrefix + pair.Key.Substring(ExpectedPrefix.Length);
                    translations[alias] = pair.Value;
                }
            }
        }

        private static string? ResolveCoreText(string key) {
            string fullKey = "STRINGS.ONITALK.CORE." + key;
            return Strings.TryGet(fullKey, out StringEntry entry)
                ? entry.String
                : null;
        }
    }
}
