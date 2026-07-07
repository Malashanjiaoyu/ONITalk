using System;
using System.Collections.Generic;
using System.Linq;

namespace ONITalk.Core {
    public sealed class ConversationMemoryLine {
        public int Cycle { get; set; }

        public string Speaker { get; set; } = string.Empty;

        public string Listener { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }

    public sealed class PairMemorySnapshot {
        public string PairKey { get; set; } = string.Empty;

        public int TotalLines { get; set; }

        public int LastCycle { get; set; } = -1;

        public long LastUpdatedSequence { get; set; }

        public List<ConversationMemoryLine> RecentLines { get; set; } =
            new List<ConversationMemoryLine>();
    }

    public sealed class PairMemoryContext {
        internal PairMemoryContext(int totalLines, int lastCycle,
                IReadOnlyList<string> recentLines) {
            TotalLines = Math.Max(0, totalLines);
            LastCycle = lastCycle;
            RecentLines = recentLines ?? Array.Empty<string>();
        }

        public int TotalLines { get; }

        public int LastCycle { get; }

        public IReadOnlyList<string> RecentLines { get; }

        public string Familiarity {
            get {
                if (TotalLines <= 0)
                    return "尚无共同对话记忆";
                if (TotalLines <= 2)
                    return "刚开始交谈";
                if (TotalLines <= 7)
                    return "逐渐熟悉";
                return "经常交谈";
            }
        }
    }

    public sealed class PersistentConversationMemory {
        private readonly Dictionary<string, PairMemorySnapshot> pairs =
            new Dictionary<string, PairMemorySnapshot>(StringComparer.Ordinal);
        private readonly object sync = new object();
        private int linesPerPair;
        private int maxPairs;
        private long nextSequence;

        public PersistentConversationMemory(int linesPerPair = 8, int maxPairs = 200) {
            Configure(linesPerPair, maxPairs);
        }

        public void Configure(int newLinesPerPair, int newMaxPairs) {
            if (newLinesPerPair < 1)
                throw new ArgumentOutOfRangeException(nameof(newLinesPerPair));
            if (newMaxPairs < 1)
                throw new ArgumentOutOfRangeException(nameof(newMaxPairs));

            lock (sync) {
                linesPerPair = newLinesPerPair;
                maxPairs = newMaxPairs;
                foreach (PairMemorySnapshot pair in pairs.Values)
                    TrimLines(pair);
                TrimPairs();
            }
        }

        public void Record(string pairKey, string speaker, string listener, string line,
                int cycle) {
            if (string.IsNullOrWhiteSpace(pairKey) ||
                    string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(line))
                return;

            lock (sync) {
                if (!pairs.TryGetValue(pairKey, out PairMemorySnapshot? pair)) {
                    pair = new PairMemorySnapshot { PairKey = pairKey };
                    pairs.Add(pairKey, pair);
                }

                pair.TotalLines++;
                pair.LastCycle = cycle;
                pair.LastUpdatedSequence = ++nextSequence;
                pair.RecentLines.Add(new ConversationMemoryLine {
                    Cycle = cycle,
                    Speaker = speaker.Trim(),
                    Listener = listener?.Trim() ?? string.Empty,
                    Text = line.Trim()
                });
                TrimLines(pair);
                TrimPairs();
            }
        }

        public PairMemoryContext GetContext(string pairKey) {
            lock (sync) {
                if (string.IsNullOrWhiteSpace(pairKey) ||
                        !pairs.TryGetValue(pairKey, out PairMemorySnapshot? pair))
                    return new PairMemoryContext(0, -1, Array.Empty<string>());

                string[] lines = pair.RecentLines
                    .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Text))
                    .Select(item => item.Speaker + "：" + item.Text.Trim())
                    .ToArray();
                return new PairMemoryContext(pair.TotalLines, pair.LastCycle, lines);
            }
        }

        public IReadOnlyList<PairMemorySnapshot> Export() {
            lock (sync) {
                return pairs.Values
                    .OrderBy(item => item.LastUpdatedSequence)
                    .Select(Clone)
                    .ToArray();
            }
        }

        public void Import(IEnumerable<PairMemorySnapshot>? snapshots) {
            lock (sync) {
                pairs.Clear();
                nextSequence = 0;
                if (snapshots == null)
                    return;

                foreach (PairMemorySnapshot source in snapshots) {
                    if (source == null || string.IsNullOrWhiteSpace(source.PairKey))
                        continue;

                    PairMemorySnapshot copy = Clone(source);
                    copy.PairKey = copy.PairKey.Trim();
                    copy.TotalLines = Math.Max(copy.TotalLines, copy.RecentLines.Count);
                    if (copy.LastUpdatedSequence <= 0)
                        copy.LastUpdatedSequence = ++nextSequence;
                    else
                        nextSequence = Math.Max(nextSequence, copy.LastUpdatedSequence);
                    TrimLines(copy);
                    pairs[copy.PairKey] = copy;
                }
                TrimPairs();
            }
        }

        private static PairMemorySnapshot Clone(PairMemorySnapshot source) {
            return new PairMemorySnapshot {
                PairKey = source.PairKey ?? string.Empty,
                TotalLines = Math.Max(0, source.TotalLines),
                LastCycle = source.LastCycle,
                LastUpdatedSequence = source.LastUpdatedSequence,
                RecentLines = (source.RecentLines ?? new List<ConversationMemoryLine>())
                    .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Text))
                    .Select(item => new ConversationMemoryLine {
                        Cycle = item.Cycle,
                        Speaker = item.Speaker ?? string.Empty,
                        Listener = item.Listener ?? string.Empty,
                        Text = item.Text.Trim()
                    })
                    .ToList()
            };
        }

        private void TrimLines(PairMemorySnapshot pair) {
            int excess = pair.RecentLines.Count - linesPerPair;
            if (excess > 0)
                pair.RecentLines.RemoveRange(0, excess);
        }

        private void TrimPairs() {
            if (pairs.Count <= maxPairs)
                return;

            string[] oldest = pairs.Values
                .OrderBy(item => item.LastUpdatedSequence)
                .Take(pairs.Count - maxPairs)
                .Select(item => item.PairKey)
                .ToArray();
            foreach (string pairKey in oldest)
                pairs.Remove(pairKey);
        }
    }
}
