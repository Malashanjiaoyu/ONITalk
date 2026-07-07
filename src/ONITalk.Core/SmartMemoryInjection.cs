using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ONITalk.Core {
    public enum MemoryInjectionPreset {
        轻量,
        平衡,
        丰富,
        自定义
    }

    public enum MemoryCandidateKind {
        Relationship,
        SpeakerAction,
        ListenerAction,
        ColonyEvent
    }

    public sealed class MemoryInjectionCandidate {
        public string Id { get; set; } = string.Empty;

        public MemoryCandidateKind Kind { get; set; }

        public string Text { get; set; } = string.Empty;

        public string MatchText { get; set; } = string.Empty;

        public int Cycle { get; set; } = -1;

        public float Importance { get; set; } = 0.5f;

        public long Sequence { get; set; }
    }

    public sealed class MemoryScoreBreakdown {
        public float Relevance { get; set; }

        public float Recency { get; set; }

        public float Importance { get; set; }

        public float Participant { get; set; }

        public float TypePriority { get; set; }
    }

    public sealed class SelectedMemory {
        internal SelectedMemory(MemoryInjectionCandidate candidate, float score,
                int estimatedTokens, MemoryScoreBreakdown breakdown) {
            Candidate = candidate;
            Score = score;
            EstimatedTokens = estimatedTokens;
            Breakdown = breakdown;
        }

        public MemoryInjectionCandidate Candidate { get; }

        public float Score { get; }

        public int EstimatedTokens { get; }

        public MemoryScoreBreakdown Breakdown { get; }
    }

    public sealed class MemoryInjectionSelection {
        internal MemoryInjectionSelection(string contextLabel, int tokenBudget,
                int candidateCount, IReadOnlyList<SelectedMemory> items) {
            ContextLabel = contextLabel;
            TokenBudget = tokenBudget;
            CandidateCount = candidateCount;
            Items = items;
            EstimatedTokens = items.Sum(item => item.EstimatedTokens);
        }

        public string ContextLabel { get; }

        public int TokenBudget { get; }

        public int EstimatedTokens { get; }

        public int CandidateCount { get; }

        public IReadOnlyList<SelectedMemory> Items { get; }

        public IReadOnlyList<string> GetTexts(MemoryCandidateKind kind) {
            return Items.Where(item => item.Candidate.Kind == kind)
                .Select(item => item.Candidate.Text).ToArray();
        }

        public string ToPreviewText() {
            var text = new StringBuilder(640);
            text.AppendLine(CoreText.Format("MEMORY_PREVIEW.CONTEXT", "上下文：{0}",
                ContextLabel));
            text.AppendLine(CoreText.Format("MEMORY_PREVIEW.BUDGET",
                "预算：约 {0} / {1} tokens", EstimatedTokens, TokenBudget));
            text.AppendLine(CoreText.Format("MEMORY_PREVIEW.COUNTS",
                "候选：{0}，选中：{1}", CandidateCount, Items.Count));
            if (Items.Count == 0) {
                text.AppendLine(CoreText.Get("MEMORY_PREVIEW.NONE",
                    "没有记忆达到当前预算和配额要求。"));
                return text.ToString();
            }

            foreach (SelectedMemory item in Items) {
                text.AppendLine();
                text.AppendLine(CoreText.Format("MEMORY_PREVIEW.ITEM_HEADER",
                    "[{0}] 评分 {1} · 约 {2} tokens", KindLabel(item.Candidate.Kind),
                    item.Score.ToString("0.000", CultureInfo.InvariantCulture),
                    item.EstimatedTokens));
                text.AppendLine(item.Candidate.Text);
                MemoryScoreBreakdown score = item.Breakdown;
                text.AppendLine(CoreText.Format("MEMORY_PREVIEW.BREAKDOWN",
                    "相关 {0}  时间 {1}  重要 {2}  人物 {3}  类型 {4}",
                    score.Relevance.ToString("0.00", CultureInfo.InvariantCulture),
                    score.Recency.ToString("0.00", CultureInfo.InvariantCulture),
                    score.Importance.ToString("0.00", CultureInfo.InvariantCulture),
                    score.Participant.ToString("0.00", CultureInfo.InvariantCulture),
                    score.TypePriority.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            return text.ToString();
        }

        private static string KindLabel(MemoryCandidateKind kind) {
            switch (kind) {
                case MemoryCandidateKind.Relationship:
                    return CoreText.Get("MEMORY_PREVIEW.KIND_RELATIONSHIP", "关系对话");
                case MemoryCandidateKind.SpeakerAction:
                    return CoreText.Get("MEMORY_PREVIEW.KIND_SPEAKER_ACTION", "说话者行动");
                case MemoryCandidateKind.ListenerAction:
                    return CoreText.Get("MEMORY_PREVIEW.KIND_LISTENER_ACTION", "听者行动");
                case MemoryCandidateKind.ColonyEvent:
                    return CoreText.Get("MEMORY_PREVIEW.KIND_COLONY_EVENT", "殖民地事件");
                default:
                    return CoreText.Get("MEMORY_PREVIEW.KIND_MEMORY", "记忆");
            }
        }
    }

    public static class MemoryTokenEstimator {
        public static int Estimate(string? text) {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            int cjk = 0;
            int other = 0;
            foreach (char character in text) {
                if (IsCjk(character))
                    cjk++;
                else if (!char.IsWhiteSpace(character))
                    other++;
            }
            return Math.Max(1, cjk + (other + 3) / 4);
        }

        private static bool IsCjk(char value) {
            return value >= '\u3400' && value <= '\u9fff';
        }
    }

    public static class SmartMemoryInjectionEngine {
        public static MemoryInjectionSelection Select(string contextLabel,
                string searchText, int currentCycle,
                IEnumerable<MemoryInjectionCandidate>? candidates,
                MemoryInjectionPreset preset, int customTokenBudget) {
            int budget = ResolveBudget(preset, customTokenBudget);
            MemoryInjectionCandidate[] unique = (candidates ??
                    Array.Empty<MemoryInjectionCandidate>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Text))
                .GroupBy(item => item.Kind + "\u001f" + item.Text,
                    StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(item => item.Sequence).First())
                .ToArray();
            HashSet<string> queryTerms = ExtractTerms(searchText);
            ScoredCandidate[] scored = unique.Select(candidate => Score(candidate,
                    queryTerms, currentCycle))
                .OrderByDescending(item => item.Selection.Score)
                .ThenByDescending(item => item.Candidate.Sequence)
                .ToArray();

            Dictionary<MemoryCandidateKind, int> quotas = ResolveQuotas(preset, budget);
            var selected = new List<SelectedMemory>();
            var selectedIds = new HashSet<string>(StringComparer.Ordinal);
            var counts = Enum.GetValues(typeof(MemoryCandidateKind))
                .Cast<MemoryCandidateKind>()
                .ToDictionary(kind => kind, _ => 0);
            int used = 0;

            // Give every available memory kind one fair opportunity before the
            // highest-scoring remaining candidates consume the rest of the budget.
            foreach (MemoryCandidateKind kind in Enum.GetValues(
                    typeof(MemoryCandidateKind))) {
                ScoredCandidate? best = scored.FirstOrDefault(item =>
                    item.Candidate.Kind == kind);
                if (best != null && TryAdd(best, budget, ref used, selected,
                        selectedIds))
                    counts[kind]++;
            }

            foreach (ScoredCandidate item in scored) {
                MemoryCandidateKind kind = item.Candidate.Kind;
                if (counts[kind] >= quotas[kind])
                    continue;
                if (TryAdd(item, budget, ref used, selected, selectedIds))
                    counts[kind]++;
            }

            return new MemoryInjectionSelection(contextLabel ?? string.Empty, budget,
                unique.Length, selected.OrderByDescending(item => item.Score).ToArray());
        }

        public static int ResolveBudget(MemoryInjectionPreset preset, int custom) {
            switch (preset) {
                case MemoryInjectionPreset.轻量:
                    return 250;
                case MemoryInjectionPreset.平衡:
                    return 520;
                case MemoryInjectionPreset.丰富:
                    return 850;
                default:
                    return Math.Max(100, Math.Min(1600, custom));
            }
        }

        private static ScoredCandidate Score(MemoryInjectionCandidate candidate,
                HashSet<string> queryTerms, int currentCycle) {
            HashSet<string> memoryTerms = ExtractTerms(candidate.Text + " " +
                candidate.MatchText);
            int overlap = queryTerms.Count == 0 ? 0 : queryTerms.Count(memoryTerms.Contains);
            float relevance = queryTerms.Count == 0 || memoryTerms.Count == 0 ? 0f :
                Math.Min(1f, overlap / (float)Math.Min(queryTerms.Count,
                    memoryTerms.Count));
            int age = currentCycle < 0 || candidate.Cycle < 0 ? 0 :
                Math.Max(0, currentCycle - candidate.Cycle);
            float recency = currentCycle < 0 || candidate.Cycle < 0
                ? 0.5f
                : 1f / (1f + age / 4f);
            float importance = Math.Max(0f, Math.Min(1f, candidate.Importance));
            float participant = ParticipantPriority(candidate.Kind);
            float typePriority = TypePriority(candidate.Kind);
            var breakdown = new MemoryScoreBreakdown {
                Relevance = relevance,
                Recency = recency,
                Importance = importance,
                Participant = participant,
                TypePriority = typePriority
            };
            float total = relevance * 0.35f + recency * 0.20f +
                importance * 0.20f + participant * 0.15f + typePriority * 0.10f;
            int tokens = MemoryTokenEstimator.Estimate(candidate.Text) + 8;
            return new ScoredCandidate(candidate,
                new SelectedMemory(candidate, total, tokens, breakdown));
        }

        private static bool TryAdd(ScoredCandidate item, int budget, ref int used,
                List<SelectedMemory> selected, HashSet<string> selectedIds) {
            string id = string.IsNullOrWhiteSpace(item.Candidate.Id)
                ? item.Candidate.Kind + "\u001f" + item.Candidate.Text
                : item.Candidate.Id;
            if (selectedIds.Contains(id) || used + item.Selection.EstimatedTokens > budget)
                return false;
            selectedIds.Add(id);
            selected.Add(item.Selection);
            used += item.Selection.EstimatedTokens;
            return true;
        }

        private static Dictionary<MemoryCandidateKind, int> ResolveQuotas(
                MemoryInjectionPreset preset, int budget) {
            if (preset == MemoryInjectionPreset.轻量)
                return Quotas(2, 1, 1, 1);
            if (preset == MemoryInjectionPreset.丰富)
                return Quotas(6, 5, 5, 4);
            if (preset == MemoryInjectionPreset.平衡)
                return Quotas(4, 3, 3, 2);
            int scale = Math.Max(1, budget / 170);
            return Quotas(Math.Min(8, scale), Math.Min(6, scale),
                Math.Min(6, scale), Math.Min(5, Math.Max(1, scale / 2)));
        }

        private static Dictionary<MemoryCandidateKind, int> Quotas(int relationship,
                int speaker, int listener, int events) {
            return new Dictionary<MemoryCandidateKind, int> {
                [MemoryCandidateKind.Relationship] = relationship,
                [MemoryCandidateKind.SpeakerAction] = speaker,
                [MemoryCandidateKind.ListenerAction] = listener,
                [MemoryCandidateKind.ColonyEvent] = events
            };
        }

        private static float ParticipantPriority(MemoryCandidateKind kind) {
            switch (kind) {
                case MemoryCandidateKind.Relationship:
                case MemoryCandidateKind.SpeakerAction:
                    return 1f;
                case MemoryCandidateKind.ListenerAction:
                    return 0.9f;
                default:
                    return 0.65f;
            }
        }

        private static float TypePriority(MemoryCandidateKind kind) {
            switch (kind) {
                case MemoryCandidateKind.Relationship:
                    return 0.9f;
                case MemoryCandidateKind.ColonyEvent:
                    return 0.85f;
                default:
                    return 0.75f;
            }
        }

        private static HashSet<string> ExtractTerms(string? value) {
            var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(value))
                return terms;

            var ascii = new StringBuilder();
            var cjk = new StringBuilder();
            System.Action flushAscii = () => {
                if (ascii.Length >= 2)
                    terms.Add(ascii.ToString());
                ascii.Clear();
            };
            System.Action flushCjk = () => {
                string text = cjk.ToString();
                if (text.Length == 1)
                    terms.Add(text);
                else
                    for (int index = 0; index < text.Length - 1; index++)
                        terms.Add(text.Substring(index, 2));
                cjk.Clear();
            };

            foreach (char character in value.ToLowerInvariant()) {
                if (character >= '\u3400' && character <= '\u9fff') {
                    flushAscii();
                    cjk.Append(character);
                } else if (char.IsLetterOrDigit(character)) {
                    flushCjk();
                    ascii.Append(character);
                } else {
                    flushAscii();
                    flushCjk();
                }
            }
            flushAscii();
            flushCjk();
            return terms;
        }

        private sealed class ScoredCandidate {
            internal ScoredCandidate(MemoryInjectionCandidate candidate,
                    SelectedMemory selection) {
                Candidate = candidate;
                Selection = selection;
            }

            internal MemoryInjectionCandidate Candidate { get; }

            internal SelectedMemory Selection { get; }
        }
    }
}
