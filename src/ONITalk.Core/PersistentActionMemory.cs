using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ONITalk.Core {
    public sealed class ActionMemorySnapshot {
        public string Actor { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public int FirstCycle { get; set; } = -1;

        public int LastCycle { get; set; } = -1;

        public int Count { get; set; } = 1;

        public float Importance { get; set; } = 0.5f;

        public bool IsRoutine { get; set; }

        public long LastUpdatedSequence { get; set; }
    }

    public sealed class ActionMemoryContext {
        internal ActionMemoryContext(IReadOnlyList<string> recentActions) {
            RecentActions = recentActions ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> RecentActions { get; }
    }

    public sealed class ActionMemoryUpdate {
        internal ActionMemoryUpdate(bool isNew, int count, string summary) {
            IsNew = isNew;
            Count = count;
            Summary = summary;
        }

        public bool IsNew { get; }

        public int Count { get; }

        public string Summary { get; }
    }

    public sealed class PersistentActionMemory {
        private readonly List<ActionMemorySnapshot> records =
            new List<ActionMemorySnapshot>();
        private readonly object sync = new object();
        private int capacityPerActor;
        private int aggregationWindowCycles;
        private long nextSequence;

        public PersistentActionMemory(int capacityPerActor = 30,
                int aggregationWindowCycles = 2) {
            Configure(capacityPerActor, aggregationWindowCycles);
        }

        public int Count {
            get {
                lock (sync)
                    return records.Count;
            }
        }

        public void Configure(int newCapacityPerActor, int newAggregationWindowCycles) {
            if (newCapacityPerActor < 1)
                throw new ArgumentOutOfRangeException(nameof(newCapacityPerActor));
            if (newAggregationWindowCycles < 0)
                throw new ArgumentOutOfRangeException(nameof(newAggregationWindowCycles));

            lock (sync) {
                capacityPerActor = newCapacityPerActor;
                aggregationWindowCycles = newAggregationWindowCycles;
                TrimAllActors();
            }
        }

        public ActionMemoryUpdate? Record(string actor, string category, string target,
                int cycle, float importance, bool isRoutine = false) {
            if (string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(category))
                return null;

            string normalizedActor = actor.Trim();
            string normalizedCategory = category.Trim();
            string normalizedTarget = string.IsNullOrWhiteSpace(target)
                ? normalizedCategory
                : target.Trim();
            float normalizedImportance = isRoutine
                ? ActionMemoryPolicy.RoutineImportance(1)
                : Math.Max(0f, Math.Min(0.95f, importance));

            lock (sync) {
                ActionMemorySnapshot? existing = records
                    .Where(item => string.Equals(item.Actor, normalizedActor,
                            StringComparison.Ordinal) &&
                        string.Equals(item.Category, normalizedCategory,
                            StringComparison.Ordinal) &&
                        string.Equals(item.Target, normalizedTarget,
                            StringComparison.Ordinal) &&
                        cycle >= item.LastCycle &&
                        cycle - item.LastCycle <= aggregationWindowCycles)
                    .OrderByDescending(item => item.LastUpdatedSequence)
                    .FirstOrDefault();

                bool isNew = existing == null;
                if (existing == null) {
                    existing = new ActionMemorySnapshot {
                        Actor = normalizedActor,
                        Category = normalizedCategory,
                        Target = normalizedTarget,
                        FirstCycle = cycle,
                        LastCycle = cycle,
                        Count = 1,
                        Importance = normalizedImportance,
                        IsRoutine = isRoutine,
                        LastUpdatedSequence = ++nextSequence
                    };
                    records.Add(existing);
                } else {
                    existing.LastCycle = cycle;
                    existing.Count++;
                    existing.IsRoutine = existing.IsRoutine || isRoutine;
                    existing.Importance = existing.IsRoutine
                        ? ActionMemoryPolicy.RoutineImportance(existing.Count)
                        : Math.Min(0.95f, Math.Max(existing.Importance,
                            normalizedImportance) + 0.02f);
                    existing.LastUpdatedSequence = ++nextSequence;
                }

                TrimActor(normalizedActor);
                return new ActionMemoryUpdate(isNew, existing.Count,
                    FormatSummary(existing));
            }
        }

        public ActionMemoryContext GetContext(string actor, int maximum) {
            if (string.IsNullOrWhiteSpace(actor) || maximum <= 0)
                return new ActionMemoryContext(Array.Empty<string>());

            lock (sync) {
                string[] recent = records
                    .Where(item => string.Equals(item.Actor, actor.Trim(),
                        StringComparison.Ordinal) &&
                        ActionMemoryPolicy.IsEligibleForInjection(item))
                    .OrderByDescending(item => item.LastCycle)
                    .ThenByDescending(item => item.Importance)
                    .ThenByDescending(item => item.LastUpdatedSequence)
                    .Take(maximum)
                    .Select(FormatSummary)
                    .ToArray();
                return new ActionMemoryContext(recent);
            }
        }

        public IReadOnlyList<ActionMemorySnapshot> Export() {
            lock (sync) {
                return records.OrderBy(item => item.LastUpdatedSequence)
                    .Select(Clone).ToArray();
            }
        }

        public void Import(IEnumerable<ActionMemorySnapshot>? snapshots) {
            lock (sync) {
                records.Clear();
                nextSequence = 0;
                if (snapshots == null)
                    return;

                foreach (ActionMemorySnapshot source in snapshots) {
                    if (source == null || string.IsNullOrWhiteSpace(source.Actor) ||
                            string.IsNullOrWhiteSpace(source.Category))
                        continue;
                    ActionMemorySnapshot copy = Clone(source);
                    copy.Actor = copy.Actor.Trim();
                    copy.Category = copy.Category.Trim();
                    copy.Target = string.IsNullOrWhiteSpace(copy.Target)
                        ? copy.Category
                        : copy.Target.Trim();
                    copy.Count = Math.Max(1, copy.Count);
                    copy.IsRoutine = copy.IsRoutine ||
                        ActionMemoryPolicy.LooksLikeLegacyRoutineConstruction(
                            copy.Category, copy.Target);
                    copy.Importance = copy.IsRoutine
                        ? ActionMemoryPolicy.RoutineImportance(copy.Count)
                        : Math.Max(0f, Math.Min(0.95f, copy.Importance));
                    if (copy.LastUpdatedSequence <= 0)
                        copy.LastUpdatedSequence = ++nextSequence;
                    else
                        nextSequence = Math.Max(nextSequence, copy.LastUpdatedSequence);
                    records.Add(copy);
                }
                TrimAllActors();
            }
        }

        private static ActionMemorySnapshot Clone(ActionMemorySnapshot source) {
            return new ActionMemorySnapshot {
                Actor = source.Actor ?? string.Empty,
                Category = source.Category ?? string.Empty,
                Target = source.Target ?? string.Empty,
                FirstCycle = source.FirstCycle,
                LastCycle = source.LastCycle,
                Count = source.Count,
                Importance = source.Importance,
                IsRoutine = source.IsRoutine,
                LastUpdatedSequence = source.LastUpdatedSequence
            };
        }

        public static string FormatSummary(ActionMemorySnapshot memory) {
            if (memory == null)
                return string.Empty;
            string cycle = memory.FirstCycle == memory.LastCycle
                ? "周期 " + memory.LastCycle.ToString(CultureInfo.InvariantCulture)
                : "周期 " + memory.FirstCycle.ToString(CultureInfo.InvariantCulture) +
                    "–" + memory.LastCycle.ToString(CultureInfo.InvariantCulture);
            string result = cycle + "：" + (memory.IsRoutine
                ? "完成连续铺设"
                : memory.Category);
            if (!string.Equals(memory.Target, memory.Category, StringComparison.Ordinal))
                result += "（" + memory.Target + "）";
            if (memory.Count > 1)
                result += "，共 " + memory.Count.ToString(CultureInfo.InvariantCulture) +
                    " 次";
            return result;
        }

        private void TrimAllActors() {
            foreach (string actor in records.Select(item => item.Actor)
                    .Distinct(StringComparer.Ordinal).ToArray())
                TrimActor(actor);
        }

        private void TrimActor(string actor) {
            int excess = records.Count(item => string.Equals(item.Actor, actor,
                StringComparison.Ordinal)) - capacityPerActor;
            if (excess <= 0)
                return;

            ActionMemorySnapshot[] oldest = records
                .Where(item => string.Equals(item.Actor, actor, StringComparison.Ordinal))
                .OrderBy(item => ActionMemoryPolicy.IsEligibleForInjection(item) ? 1 : 0)
                .ThenBy(item => item.Importance)
                .ThenBy(item => item.LastUpdatedSequence)
                .Take(excess)
                .ToArray();
            foreach (ActionMemorySnapshot item in oldest)
                records.Remove(item);
        }
    }
}
