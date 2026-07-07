using System;
using System.Text;

namespace ONITalk.Core {
    public static class LineSanitizer {
        private static readonly char[] QuoteCharacters = {
            '\"', '\'', '“', '”', '‘', '’', '「', '」', '『', '』'
        };

        public static string Clean(string? raw, string? speakerName, int maxCharacters) {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string line = CollapseWhitespace(raw).Trim().Trim(QuoteCharacters).Trim();
            if (!string.IsNullOrWhiteSpace(speakerName)) {
                string name = speakerName.Trim();
                if (line.StartsWith(name + "：", StringComparison.Ordinal) ||
                        line.StartsWith(name + ":", StringComparison.Ordinal))
                    line = line.Substring(name.Length + 1).Trim();
            }

            line = line.Trim(QuoteCharacters).Trim();
            int limit = Math.Max(10, maxCharacters);
            if (line.Length > limit)
                line = line.Substring(0, limit - 1).TrimEnd() + "…";
            return line;
        }

        private static string CollapseWhitespace(string value) {
            var result = new StringBuilder(value.Length);
            bool previousWasWhitespace = false;
            foreach (char character in value) {
                bool isWhitespace = char.IsWhiteSpace(character);
                if (!isWhitespace || !previousWasWhitespace)
                    result.Append(isWhitespace ? ' ' : character);
                previousWasWhitespace = isWhitespace;
            }
            return result.ToString();
        }
    }
}

