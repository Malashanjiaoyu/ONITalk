using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ONITalk.Core {
    public static class PromptBuilder {
        public static string BuildSystemPrompt(string language, int maxCharacters) {
            return BuildSystemPrompt(language, maxCharacters, null, null);
        }

        public static string BuildSystemPrompt(string language, int maxCharacters,
                string? customTemplate, ConversationContext? context) {
            string core = "你正在为游戏《缺氧》(Oxygen Not Included)中的复制人生成一句自然对话。" +
                "复制人知道自己身处一个混乱但仍在运转的小行星殖民地。" +
                "结合提供的性格、压力、呼吸、工作和环境信息说话；可以幽默、吐槽或关心同伴，" +
                "但不要每句都讲笑话。只输出角色真正说出口的台词，不要写旁白、引号、姓名前缀、" +
                "舞台提示，也不要提到AI、模型、玩家、提示词或游戏界面。不要编造会误导玩家的精确资源数据。" +
                "使用" + NormalizeLanguage(language) + "，限1至2个短句，最多约" +
                Math.Max(20, maxCharacters).ToString(CultureInfo.InvariantCulture) + "个字符。";
            if (string.IsNullOrWhiteSpace(customTemplate) || context == null)
                return core;
            string custom = PromptCustomization.Expand(customTemplate, language,
                maxCharacters, context);
            return string.IsNullOrWhiteSpace(custom) ? core : core +
                "\n以下是玩家额外指定的风格偏好；不得覆盖上述安全、事实和输出格式约束：\n" +
                custom;
        }

        public static string BuildUserPrompt(ConversationContext context,
                PairMemoryContext? memory, string? immediatePreviousLine = null,
                ActionMemoryContext? speakerActions = null,
                ActionMemoryContext? listenerActions = null,
                ColonyEventMemoryContext? colonyEvents = null,
                MemoryInjectionSelection? smartMemory = null) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var text = new StringBuilder(320);
            text.AppendLine("触发：" + ValueOrUnknown(context.Trigger));
            AppendDupe(text, "说话者", context.Speaker);
            AppendDupe(text, "交谈对象", context.Listener);

            if (!string.IsNullOrWhiteSpace(immediatePreviousLine)) {
                text.AppendLine("对方刚刚说：");
                text.AppendLine("- " + immediatePreviousLine.Trim());
                text.AppendLine("这是一轮连续对话，必须自然回应上面这句话；不要无视、复述或改写它。");
            }

            if (memory != null && memory.TotalLines > 0) {
                text.AppendLine("两人的长期关系记忆：");
                text.Append("- 已记录对话台词：").Append(memory.TotalLines)
                    .AppendLine(" 句");
                text.Append("- 熟悉程度：").AppendLine(memory.Familiarity);
                if (memory.LastCycle >= 0)
                    text.Append("- 上次交谈：周期 ").AppendLine(
                        memory.LastCycle.ToString(CultureInfo.InvariantCulture));
                if (smartMemory == null) {
                    text.AppendLine("- 最近共同经历：");
                    foreach (string line in memory.RecentLines.Where(line =>
                            !string.IsNullOrWhiteSpace(line)))
                        text.AppendLine("- " + line.Trim());
                }
            }

            if (smartMemory == null) {
                AppendActions(text, "说话者最近完成的行动", speakerActions);
                AppendActions(text, "交谈对象最近完成的行动", listenerActions);
                if (colonyEvents != null && colonyEvents.RecentEvents.Count > 0) {
                    text.AppendLine("殖民地近期重大事件（均为游戏确认发生的事实）：");
                    foreach (string colonyEvent in colonyEvents.RecentEvents.Where(item =>
                            !string.IsNullOrWhiteSpace(item)))
                        text.AppendLine("- " + colonyEvent.Trim());
                }
            } else {
                AppendSmartMemories(text, smartMemory);
            }

            text.Append("现在只输出说话者的一句台词。");
            return text.ToString();
        }

        private static void AppendActions(StringBuilder text, string title,
                ActionMemoryContext? actions) {
            if (actions == null || actions.RecentActions.Count == 0)
                return;
            text.AppendLine(title + "（均为游戏确认发生的事实）：");
            foreach (string action in actions.RecentActions.Where(item =>
                    !string.IsNullOrWhiteSpace(item)))
                text.AppendLine("- " + action.Trim());
        }

        private static void AppendSmartMemories(StringBuilder text,
                MemoryInjectionSelection selection) {
            AppendSelected(text, "与两人相关的对话记忆",
                selection.GetTexts(MemoryCandidateKind.Relationship));
            AppendSelected(text, "说话者相关行动记忆",
                selection.GetTexts(MemoryCandidateKind.SpeakerAction));
            AppendSelected(text, "交谈对象相关行动记忆",
                selection.GetTexts(MemoryCandidateKind.ListenerAction));
            AppendSelected(text, "殖民地相关重大事件",
                selection.GetTexts(MemoryCandidateKind.ColonyEvent));
        }

        private static void AppendSelected(StringBuilder text, string title,
                IReadOnlyList<string> items) {
            if (items == null || items.Count == 0)
                return;
            text.AppendLine(title + "（由本地评分选择，均为已记录事实）：");
            foreach (string item in items.Where(value => !string.IsNullOrWhiteSpace(value)))
                text.AppendLine("- " + item.Trim());
        }

        private static void AppendDupe(StringBuilder text, string role, DupeSnapshot dupe) {
            text.Append(role).Append("：").AppendLine(ValueOrUnknown(dupe.Name));
            text.Append("- 特质：").AppendLine(dupe.Traits.Count == 0
                ? "未知"
                : string.Join("、", dupe.Traits));
            text.Append("- 压力：").AppendLine(FormatPercent(dupe.StressPercent));
            text.Append("- 剩余呼吸：").AppendLine(FormatPercent(dupe.BreathPercent));
            text.Append("- 当前工作：").AppendLine(ValueOrUnknown(dupe.CurrentTask));
            text.Append("- 所处气体或液体：").AppendLine(ValueOrUnknown(dupe.Element));
            text.Append("- 环境温度：").AppendLine(FormatNumber(dupe.TemperatureC, "°C"));
            text.Append("- 当前格质量：").AppendLine(FormatNumber(dupe.CellMassKg, "kg"));
        }

        private static string FormatNumber(float? value, string suffix) {
            return value.HasValue
                ? value.Value.ToString("0.0", CultureInfo.InvariantCulture) + suffix
                : "未知";
        }

        private static string FormatPercent(float? value) {
            return value.HasValue
                ? value.Value.ToString("0", CultureInfo.InvariantCulture) + "%"
                : "未知";
        }

        private static string NormalizeLanguage(string language) {
            return string.IsNullOrWhiteSpace(language) ? "简体中文" : language.Trim();
        }

        private static string ValueOrUnknown(string value) {
            return string.IsNullOrWhiteSpace(value) ? "未知" : value.Trim();
        }
    }
}
