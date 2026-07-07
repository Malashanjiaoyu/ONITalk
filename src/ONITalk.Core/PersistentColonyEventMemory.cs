using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ONITalk.Core {
    public sealed class ColonyEventMemorySnapshot {
        public string Category { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int Cycle { get; set; } = -1;

        public float Importance { get; set; } = 0.8f;

        public long LastUpdatedSequence { get; set; }
    }

    public sealed class ColonyEventMemoryContext {
        internal ColonyEventMemoryContext(IReadOnlyList<string> recentEvents) {
            RecentEvents = recentEvents ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> RecentEvents { get; }
    }

    public sealed class PersistentColonyEventMemory {
        private readonly List<ColonyEventMemorySnapshot> records =
            new List<ColonyEventMemorySnapshot>();
        private readonly object sync = new object();
        private int capacity;
        private long nextSequence;

        public PersistentColonyEventMemory(int capacity = 50) {
            Configure(capacity);
        }

        public int Count {
            get {
                lock (sync)
                    return records.Count;
            }
        }

        public void Configure(int newCapacity) {
            if (newCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(newCapacity));
            lock (sync) {
                capacity = newCapacity;
                Trim();
            }
        }

        public bool Record(string category, string content, int cycle, float importance) {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(content))
                return false;

            lock (sync) {
                if (records.Any(item => item.Cycle == cycle &&
                        string.Equals(item.Content, content.Trim(), StringComparison.Ordinal)))
                    return false;
                records.Add(new ColonyEventMemorySnapshot {
                    Category = category.Trim(),
                    Content = content.Trim(),
                    Cycle = cycle,
                    Importance = Math.Max(0f, Math.Min(1f, importance)),
                    LastUpdatedSequence = ++nextSequence
                });
                Trim();
                return true;
            }
        }

        public ColonyEventMemoryContext GetContext(int maximum) {
            if (maximum <= 0)
                return new ColonyEventMemoryContext(Array.Empty<string>());
            lock (sync) {
                string[] events = records
                    .OrderByDescending(item => item.Cycle)
                    .ThenByDescending(item => item.Importance)
                    .ThenByDescending(item => item.LastUpdatedSequence)
                    .Take(maximum)
                    .Select(item => "周期 " + item.Cycle.ToString(
                        CultureInfo.InvariantCulture) + "：" + item.Content)
                    .ToArray();
                return new ColonyEventMemoryContext(events);
            }
        }

        public IReadOnlyList<ColonyEventMemorySnapshot> Export() {
            lock (sync)
                return records.OrderBy(item => item.LastUpdatedSequence)
                    .Select(Clone).ToArray();
        }

        public void Import(IEnumerable<ColonyEventMemorySnapshot>? snapshots) {
            lock (sync) {
                records.Clear();
                nextSequence = 0;
                if (snapshots != null) {
                    foreach (ColonyEventMemorySnapshot source in snapshots) {
                        if (source == null || string.IsNullOrWhiteSpace(source.Content))
                            continue;
                        ColonyEventMemorySnapshot copy = Clone(source);
                        copy.Importance = Math.Max(0f, Math.Min(1f, copy.Importance));
                        if (copy.LastUpdatedSequence <= 0)
                            copy.LastUpdatedSequence = ++nextSequence;
                        else
                            nextSequence = Math.Max(nextSequence,
                                copy.LastUpdatedSequence);
                        records.Add(copy);
                    }
                }
                Trim();
            }
        }

        private static ColonyEventMemorySnapshot Clone(ColonyEventMemorySnapshot source) {
            return new ColonyEventMemorySnapshot {
                Category = source.Category ?? string.Empty,
                Content = source.Content ?? string.Empty,
                Cycle = source.Cycle,
                Importance = source.Importance,
                LastUpdatedSequence = source.LastUpdatedSequence
            };
        }

        private void Trim() {
            int excess = records.Count - capacity;
            if (excess <= 0)
                return;
            ColonyEventMemorySnapshot[] removable = records
                .OrderBy(item => item.Importance >= 0.95f ? 1 : 0)
                .ThenBy(item => item.LastUpdatedSequence)
                .Take(excess)
                .ToArray();
            foreach (ColonyEventMemorySnapshot item in removable)
                records.Remove(item);
        }
    }
}
