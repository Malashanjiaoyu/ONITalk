using System;

namespace ONITalk.Core {
    public static class StateTriggerEvaluator {
        public static string? Evaluate(DupeSnapshot snapshot, float lowBreathPercent,
                float highStressPercent, float highTemperatureC) {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (snapshot.BreathPercent.HasValue &&
                    snapshot.BreathPercent.Value <= lowBreathPercent)
                return "复制人呼吸不足";
            if (snapshot.StressPercent.HasValue &&
                    snapshot.StressPercent.Value >= highStressPercent)
                return "复制人压力过高";
            if (snapshot.TemperatureC.HasValue &&
                    snapshot.TemperatureC.Value >= highTemperatureC)
                return "复制人身处高温环境";
            return null;
        }
    }
}
