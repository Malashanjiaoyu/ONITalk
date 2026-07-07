using System;
using System.Globalization;

namespace ONITalk.Core {
    public static class CoreText {
        public static Func<string, string?>? Resolver { get; set; }

        public static string Get(string key, string fallback) {
            try {
                string? value = Resolver?.Invoke(key);
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            } catch {
                return fallback;
            }
        }

        public static string Format(string key, string fallback, params object[] values) {
            return string.Format(CultureInfo.InvariantCulture, Get(key, fallback), values);
        }
    }
}
