using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    public static class EchoDialogueGenerator {
        public static string Generate(ConversationContext context) {
            return Generate(context, Array.Empty<string>(), null);
        }

        public static string Generate(ConversationContext context,
                IReadOnlyList<string>? recentHistory) {
            return Generate(context, recentHistory, null);
        }

        public static string Generate(ConversationContext context,
                IReadOnlyList<string>? recentHistory, string? immediatePreviousLine) {
            return Generate(context, recentHistory, immediatePreviousLine, "简体中文");
        }

        public static string Generate(ConversationContext context,
                IReadOnlyList<string>? recentHistory, string? immediatePreviousLine,
                string? language) {
            DupeSnapshot speaker = context.Speaker;
            string listener = context.Listener.Name;
            string[] candidates;

            if (IsLanguage(language, "English"))
                return SelectCandidate(context, BuildEnglishCandidates(speaker, listener,
                    immediatePreviousLine), recentHistory);
            if (IsLanguage(language, "Español") || IsLanguage(language, "Spanish"))
                return SelectCandidate(context, BuildSpanishCandidates(speaker, listener,
                    immediatePreviousLine), recentHistory);

            if (!string.IsNullOrWhiteSpace(immediatePreviousLine)) {
                candidates = BuildReplyCandidates(speaker, listener);
            } else if (speaker.BreathPercent.HasValue && speaker.BreathPercent.Value < 25f) {
                candidates = new[] {
                    "先别聊，我的肺正在申请离职。",
                    listener + "，有氧气的话现在分享正合适。",
                    "我不是沉默，我是在节省最后几口气。",
                    "等我喘上气，再继续这场重要会议。"
                };
            } else if (speaker.StressPercent.HasValue && speaker.StressPercent.Value > 70f) {
                candidates = new[] {
                    "我现在的压力，比高压气管里的东西还实在。",
                    listener + "，你有没有一种整个基地都在冒红字的感觉？",
                    "再来一项工作，我的压力反应就要抢先下班了。",
                    "我很冷静，只是压力计不太同意。",
                    "今天的压力，已经足够拿来发电了。"
                };
            } else if (speaker.TemperatureC.HasValue && speaker.TemperatureC.Value > 50f) {
                candidates = new[] {
                    "这里热得连抱怨都需要额外散热。",
                    listener + "，如果我开始冒蒸汽，记得先关机器。",
                    "这个温度很适合烤东西，比如我们。",
                    "我建议把这里改名叫工业桑拿。"
                };
            } else {
                candidates = new[] {
                    listener + "，如果你不是来接班的，至少带点好消息。",
                    "今天的氧气闻起来，有点像预算不足。",
                    listener + "，你觉得这根管子真的接对了吗？",
                    "基地还没爆炸，看来今天效率不错。",
                    "我刚路过厕所，它看我的眼神不太友善。",
                    listener + "，下次开会能不能选个有氧气的地方？",
                    "我相信工程图，主要是因为我看不懂它。",
                    "这里每一声警报，都很有参与感。",
                    listener + "，你忙完以后能顺便拯救一下殖民地吗？",
                    "好消息：至少今天坏掉的不是全部东西。"
                };
            }

            return SelectCandidate(context, candidates, recentHistory);
        }

        private static string[] BuildEnglishCandidates(DupeSnapshot speaker,
                string listener, string? previous) {
            if (!string.IsNullOrWhiteSpace(previous))
                return new[] { "All right, " + listener + ", I'll remember that.",
                    "That sounds reasonable, at least compared with the alarms.",
                    "Let's do that and hope the colony agrees." };
            if (speaker.BreathPercent.HasValue && speaker.BreathPercent.Value < 25f)
                return new[] { "Let's talk after I find some oxygen.",
                    listener + ", now would be a great time to share some air.",
                    "I'm not quiet, I'm saving my last few breaths." };
            if (speaker.StressPercent.HasValue && speaker.StressPercent.Value > 70f)
                return new[] { "My stress meter has started disagreeing with me.",
                    "One more job and my stress response clocks out first.",
                    listener + ", does the whole colony feel like a red alert?" };
            if (speaker.TemperatureC.HasValue && speaker.TemperatureC.Value > 50f)
                return new[] { "It is hot enough here to roast us.",
                    listener + ", turn off the machines if I start steaming.",
                    "Can we rename this place the industrial sauna?" };
            return new[] { "The colony has not exploded, so today is going well.",
                listener + ", are you sure that pipe is connected correctly?",
                "Good news: not everything is broken today.",
                "Every alarm here really wants to feel involved." };
        }

        private static string[] BuildSpanishCandidates(DupeSnapshot speaker,
                string listener, string? previous) {
            if (!string.IsNullOrWhiteSpace(previous))
                return new[] { "De acuerdo, " + listener + ", lo recordaré.",
                    "Suena razonable, al menos comparado con las alarmas.",
                    "Hagámoslo y esperemos que la colonia esté de acuerdo." };
            if (speaker.BreathPercent.HasValue && speaker.BreathPercent.Value < 25f)
                return new[] { "Hablemos cuando encuentre algo de oxígeno.",
                    listener + ", este sería un buen momento para compartir aire.",
                    "No estoy callado, estoy guardando mis últimas bocanadas." };
            if (speaker.StressPercent.HasValue && speaker.StressPercent.Value > 70f)
                return new[] { "Mi medidor de estrés ya no está de acuerdo conmigo.",
                    "Un trabajo más y mi estrés terminará el turno antes que yo.",
                    listener + ", ¿toda la colonia parece una alerta roja?" };
            if (speaker.TemperatureC.HasValue && speaker.TemperatureC.Value > 50f)
                return new[] { "Aquí hace suficiente calor para asarnos.",
                    listener + ", apaga las máquinas si empiezo a echar vapor.",
                    "¿Podemos llamar a esto sauna industrial?" };
            return new[] { "La colonia no ha explotado, así que el día va bien.",
                listener + ", ¿seguro que esa tubería está bien conectada?",
                "Buenas noticias: hoy no está roto absolutamente todo.",
                "Cada alarma de aquí quiere participar." };
        }

        private static bool IsLanguage(string? value, string expected) {
            return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string[] BuildReplyCandidates(DupeSnapshot speaker, string listener) {
            if (speaker.BreathPercent.HasValue && speaker.BreathPercent.Value < 25f) {
                return new[] {
                    listener + "，我听见了，但先让我喘口气。",
                    "这事等我找到氧气再认真回答。",
                    "你继续说，我先把呼吸这件小事处理一下。"
                };
            }
            if (speaker.StressPercent.HasValue && speaker.StressPercent.Value > 70f) {
                return new[] {
                    "你说得轻松，我的压力计可不这么想。",
                    listener + "，这话我记下了，和下一次警报放在一起。",
                    "行，我尽量相信你，虽然基地不太配合。"
                };
            }
            return new[] {
                "行，" + listener + "，这话我先记下了。",
                "你这么一说，我反而更想检查一下那根管子。",
                "听起来有道理，至少比警报声更让人安心。",
                listener + "，那就这么办，希望基地也同意。",
                "我明白了，不过出了问题这次算我们两个人的。"
            };
        }

        private static string SelectCandidate(ConversationContext context,
                IReadOnlyList<string> candidates, IReadOnlyList<string>? recentHistory) {
            int historyCount = recentHistory?.Count ?? 0;
            int start = (StableSeed(context.Speaker.Name, context.Listener.Name) +
                historyCount) % candidates.Count;

            for (int offset = 0; offset < candidates.Count; offset++) {
                string candidate = candidates[(start + offset) % candidates.Count];
                if (!WasRecentlyUsed(candidate, recentHistory))
                    return candidate;
            }
            return candidates[start];
        }

        private static bool WasRecentlyUsed(string candidate,
                IReadOnlyList<string>? recentHistory) {
            if (recentHistory == null)
                return false;

            foreach (string line in recentHistory) {
                if (!string.IsNullOrWhiteSpace(line) &&
                        line.IndexOf(candidate, StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        private static int StableSeed(string first, string second) {
            int result = 17;
            foreach (char character in (first ?? string.Empty) + "\u001f" +
                    (second ?? string.Empty))
                result = unchecked(result * 31 + character);
            return result & int.MaxValue;
        }
    }
}
