using System;
using System.Collections.Generic;
using ONITalk.Core;

namespace ONITalk.Bridge {
    internal sealed class CompletedActionSnapshot {
        internal CompletedActionSnapshot(string actor, string category, string target,
                float importance, bool isRoutine) {
            Actor = actor;
            Category = category;
            Target = target;
            Importance = importance;
            IsRoutine = isRoutine;
        }

        internal string Actor { get; }

        internal string Category { get; }

        internal string Target { get; }

        internal float Importance { get; }

        internal bool IsRoutine { get; }
    }

    internal static class ActionMemoryCapture {
        internal static CompletedActionSnapshot? TryCapture(Workable? workable,
                WorkerBase? worker) {
            if (workable == null || worker == null || worker.gameObject == null)
                return null;

            MinionIdentity? identity = worker.gameObject.GetComponent<MinionIdentity>();
            if (identity == null)
                return null;
            if (!ActionCategoryClassifier.TryClassify(GetTypeNames(workable.GetType()),
                    out string category,
                    out float importance))
                return null;

            bool isRoutine = string.Equals(category, "完成建造",
                StringComparison.Ordinal) && IsRoutineConstruction(workable);
            if (isRoutine)
                importance = ActionMemoryPolicy.RoutineImportance(1);

            return new CompletedActionSnapshot(identity.GetProperName(), category,
                GetTargetName(workable, category), importance, isRoutine);
        }

        private static bool IsRoutineConstruction(Workable workable) {
            try {
                Building? building = workable.gameObject.GetComponent<Building>();
                BuildingDef? definition = building?.Def;
                return definition != null &&
                    (definition.DragBuild || definition.IsTilePiece);
            } catch {
                return false;
            }
        }

        private static IEnumerable<string> GetTypeNames(Type type) {
            for (Type? current = type; current != null && current != typeof(Workable);
                    current = current.BaseType)
                yield return current.Name;
        }

        private static string GetTargetName(Workable workable, string fallback) {
            try {
                KSelectable? selectable = workable.gameObject.GetComponent<KSelectable>();
                string name = selectable == null ? string.Empty : selectable.GetName();
                if (!string.IsNullOrWhiteSpace(name))
                    return Clean(name);
            } catch {
            }

            try {
                string topic = workable.GetConversationTopic();
                if (!string.IsNullOrWhiteSpace(topic))
                    return Clean(topic);
            } catch {
            }
            return fallback;
        }

        private static string Clean(string value) {
            string cleaned = global::STRINGS.UI.StripLinkFormatting(
                value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "未知目标" : cleaned;
        }
    }
}
