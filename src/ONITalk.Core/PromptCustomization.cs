using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ONITalk.Core {
    public static class PromptCustomization {
        public const int MaximumLength = 4000;
        public const string StarterTemplate =
            "Let {speaker} speak naturally to {listener} in {language}. " +
            "Use the current situation ({trigger}) and keep the line within " +
            "{maxCharacters} characters.";

        private static readonly HashSet<string> AllowedVariables =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "language", "maxCharacters", "speaker", "listener", "trigger"
            };

        public static IReadOnlyList<string> FindUnknownVariables(string? template) {
            var result = new List<string>();
            foreach (Match match in Regex.Matches(template ?? string.Empty,
                    "\\{(?<name>[A-Za-z][A-Za-z0-9]*)\\}")) {
                string name = match.Groups["name"].Value;
                if (!AllowedVariables.Contains(name) && !result.Contains(name))
                    result.Add(name);
            }
            return result;
        }

        public static string Expand(string? template, string language, int maxCharacters,
                ConversationContext context) {
            string value = (template ?? string.Empty).Trim();
            if (value.Length > MaximumLength)
                value = value.Substring(0, MaximumLength);
            return value.Replace("{language}", language ?? string.Empty)
                .Replace("{maxCharacters}", Math.Max(20, maxCharacters).ToString())
                .Replace("{speaker}", context?.Speaker?.Name ?? string.Empty)
                .Replace("{listener}", context?.Listener?.Name ?? string.Empty)
                .Replace("{trigger}", context?.Trigger ?? string.Empty);
        }
    }
}
