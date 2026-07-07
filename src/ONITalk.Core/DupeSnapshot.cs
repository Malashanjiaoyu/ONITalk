using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    public sealed class DupeSnapshot {
        public string Name { get; set; } = "未知复制人";

        public IReadOnlyList<string> Traits { get; set; } = Array.Empty<string>();

        public float? StressPercent { get; set; }

        public float? BreathPercent { get; set; }

        public string CurrentTask { get; set; } = "未知";

        public string Element { get; set; } = "未知";

        public float? TemperatureC { get; set; }

        public float? CellMassKg { get; set; }
    }
}

