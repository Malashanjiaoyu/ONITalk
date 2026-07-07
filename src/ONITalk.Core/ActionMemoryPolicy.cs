using System;

namespace ONITalk.Core {
    public static class ActionMemoryPolicy {
        public const int RoutinePromotionCount = 4;

        public static bool IsEligibleForInjection(ActionMemorySnapshot? memory) {
            return memory != null && (!memory.IsRoutine ||
                memory.Count >= RoutinePromotionCount);
        }

        public static float RoutineImportance(int count) {
            int completed = Math.Max(1, count);
            return Math.Min(0.45f, 0.18f + Math.Min(9, completed) * 0.03f);
        }

        public static bool LooksLikeLegacyRoutineConstruction(string? category,
                string? target) {
            if (!string.Equals(category, "完成建造", StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(target))
                return false;

            string value = target.Trim().ToLowerInvariant();
            string[] markers = {
                "砖", "梯", "电线", "导线", "管道", "轨道", "自动化线", "滑杆",
                "tile", "ladder", "wire", "conduit", "rail", "fire pole"
            };
            foreach (string marker in markers)
                if (value.Contains(marker))
                    return true;
            return false;
        }
    }
}
