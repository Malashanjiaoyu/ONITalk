using System;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ONITalk.Infrastructure {
    public enum InterfaceLanguage {
        [Option("STRINGS.ONITALK.LANGUAGES.FOLLOW_GAME")]
        FollowGame,

        [Option("STRINGS.ONITALK.LANGUAGES.ENGLISH")]
        English,

        [Option("STRINGS.ONITALK.LANGUAGES.SIMPLIFIED_CHINESE")]
        SimplifiedChinese,

        [Option("STRINGS.ONITALK.LANGUAGES.SPANISH")]
        Spanish
    }

    internal sealed class InterfaceLanguageConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(InterfaceLanguage);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
                object? existingValue, JsonSerializer serializer) {
            string value = reader.Value?.ToString() ?? string.Empty;
            return Enum.TryParse(value, true, out InterfaceLanguage language) &&
                Enum.IsDefined(typeof(InterfaceLanguage), language)
                ? language
                : InterfaceLanguage.FollowGame;
        }

        public override void WriteJson(JsonWriter writer, object? value,
                JsonSerializer serializer) {
            writer.WriteValue((value is InterfaceLanguage language
                ? language
                : InterfaceLanguage.FollowGame).ToString());
        }
    }
}
