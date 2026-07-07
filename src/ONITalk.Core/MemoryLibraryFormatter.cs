using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ONITalk.Core {
    public static class MemoryLibraryFormatter {
        public static string Format(string colonyName, string colonyId,
                IEnumerable<PairMemorySnapshot>? pairs,
                IEnumerable<ActionMemorySnapshot>? actions,
                IEnumerable<ColonyEventMemorySnapshot>? events,
                int maximumPerSection = 30) {
            PairMemorySnapshot[] pairList = (pairs ??
                    Array.Empty<PairMemorySnapshot>())
                .Where(item => item != null)
                .OrderByDescending(item => item.LastUpdatedSequence).ToArray();
            ActionMemorySnapshot[] actionList = (actions ??
                    Array.Empty<ActionMemorySnapshot>())
                .Where(item => item != null)
                .OrderByDescending(item => item.LastUpdatedSequence).ToArray();
            ColonyEventMemorySnapshot[] eventList = (events ??
                    Array.Empty<ColonyEventMemorySnapshot>())
                .Where(item => item != null)
                .OrderByDescending(item => item.LastUpdatedSequence).ToArray();
            int limit = Math.Max(1, maximumPerSection);

            var text = new StringBuilder(2048);
            text.AppendLine(CoreText.Format("MEMORY_LIBRARY.COLONY",
                "殖民地：{0}", Value(colonyName, CoreText.Get(
                    "MEMORY_LIBRARY.UNNAMED_COLONY", "未命名殖民地"))));
            text.AppendLine(CoreText.Format("MEMORY_LIBRARY.ID", "ID：{0}",
                Value(colonyId, CoreText.Get("MEMORY_LIBRARY.UNKNOWN", "未知"))));
            text.AppendLine(CoreText.Format("MEMORY_LIBRARY.COUNTS",
                "关系：{0} 对 · 已记录台词：{1} 句 · 行动：{2} 条 · 事件：{3} 条",
                pairList.Length, pairList.Sum(item => Math.Max(0, item.TotalLines)),
                actionList.Length, eventList.Length));
            text.AppendLine(CoreText.Get("MEMORY_LIBRARY.SUMMARY_NOTE",
                "以下为最新摘要；导出 JSON 包含完整记忆。") + "\n");

            AppendPairs(text, pairList, limit);
            AppendActions(text, actionList, limit);
            AppendEvents(text, eventList, limit);
            return text.ToString();
        }

        private static void AppendPairs(StringBuilder text,
                IReadOnlyList<PairMemorySnapshot> pairs, int limit) {
            text.AppendLine(CoreText.Get("MEMORY_LIBRARY.RELATIONSHIPS_HEADING",
                "【关系对话】"));
            if (pairs.Count == 0)
                text.AppendLine("- " + CoreText.Get("MEMORY_LIBRARY.NO_RECORDS",
                    "暂无记录"));
            foreach (PairMemorySnapshot pair in pairs.Take(limit)) {
                text.Append("- ").Append(PairLabel(pair)).Append(" · ")
                    .Append(CoreText.Format("MEMORY_LIBRARY.LINE_COUNT", "{0} 句",
                        Math.Max(0, pair.TotalLines)));
                if (pair.LastCycle >= 0)
                    text.Append(" · ").Append(CoreText.Format(
                        "MEMORY_LIBRARY.CYCLE", "周期 {0}", pair.LastCycle));
                text.AppendLine();
                ConversationMemoryLine? latest = pair.RecentLines?
                    .LastOrDefault(item => item != null &&
                        !string.IsNullOrWhiteSpace(item.Text));
                if (latest != null)
                    text.AppendLine("  " + CoreText.Format("MEMORY_LIBRARY.LATEST",
                        "最近：{0}：{1}", latest.Speaker, latest.Text));
            }
            AppendOmitted(text, pairs.Count, limit);
            text.AppendLine();
        }

        private static void AppendActions(StringBuilder text,
                IReadOnlyList<ActionMemorySnapshot> actions, int limit) {
            text.AppendLine(CoreText.Get("MEMORY_LIBRARY.ACTIONS_HEADING",
                "【行动记忆】"));
            if (actions.Count == 0)
                text.AppendLine("- " + CoreText.Get("MEMORY_LIBRARY.NO_RECORDS",
                    "暂无记录"));
            foreach (ActionMemorySnapshot action in actions.Take(limit)) {
                text.Append("- ").Append(action.Actor).Append(" · ")
                    .Append(PersistentActionMemory.FormatSummary(action))
                    .Append(" · ").Append(CoreText.Format("MEMORY_LIBRARY.IMPORTANCE",
                        "重要性 {0}", action.Importance.ToString("0.00",
                            CultureInfo.InvariantCulture)));
                if (action.IsRoutine)
                    text.Append(" · ").Append(CoreText.Get(
                        "MEMORY_LIBRARY.ROUTINE_CONSTRUCTION", "日常工程"));
                text.AppendLine();
            }
            AppendOmitted(text, actions.Count, limit);
            text.AppendLine();
        }

        private static void AppendEvents(StringBuilder text,
                IReadOnlyList<ColonyEventMemorySnapshot> events, int limit) {
            text.AppendLine(CoreText.Get("MEMORY_LIBRARY.EVENTS_HEADING",
                "【殖民地事件】"));
            if (events.Count == 0)
                text.AppendLine("- " + CoreText.Get("MEMORY_LIBRARY.NO_RECORDS",
                    "暂无记录"));
            foreach (ColonyEventMemorySnapshot colonyEvent in events.Take(limit)) {
                text.Append("- ").Append(CoreText.Format("MEMORY_LIBRARY.CYCLE",
                        "周期 {0}", colonyEvent.Cycle)).Append(" · ")
                    .Append(colonyEvent.Content).Append(" · ")
                    .AppendLine(CoreText.Format("MEMORY_LIBRARY.IMPORTANCE",
                        "重要性 {0}", colonyEvent.Importance.ToString("0.00",
                            CultureInfo.InvariantCulture)));
            }
            AppendOmitted(text, events.Count, limit);
        }

        private static void AppendOmitted(StringBuilder text, int count, int limit) {
            if (count > limit)
                text.AppendLine("  " + CoreText.Format("MEMORY_LIBRARY.OMITTED",
                    "……另有 {0} 条未在摘要中显示", count - limit));
        }

        private static string PairLabel(PairMemorySnapshot pair) {
            ConversationMemoryLine? line = pair.RecentLines?.LastOrDefault(item =>
                item != null && (!string.IsNullOrWhiteSpace(item.Speaker) ||
                !string.IsNullOrWhiteSpace(item.Listener)));
            if (line != null)
                return Value(line.Speaker, CoreText.Get("MEMORY_LIBRARY.UNKNOWN", "未知")) +
                    " ↔ " + Value(line.Listener, CoreText.Get(
                        "MEMORY_LIBRARY.UNKNOWN", "未知"));
            return Value(pair.PairKey, CoreText.Get("MEMORY_LIBRARY.UNKNOWN_RELATION",
                "未知关系")).Replace('\u001f', '↔');
        }

        private static string Value(string? value, string fallback) {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
