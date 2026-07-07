using System;
using System.Collections.Generic;
using Klei.AI;
using ONITalk.Core;
using UnityEngine;

namespace ONITalk.Bridge {
    internal static class SnapshotFactory {
        internal static DupeSnapshot Capture(MinionIdentity identity) {
            if (identity == null)
                return new DupeSnapshot();

            GameObject duplicant = identity.gameObject;
            var snapshot = new DupeSnapshot {
                Name = identity.GetProperName(),
                Traits = GetTraits(duplicant),
                StressPercent = GetAmountPercent(Db.Get().Amounts.Stress.Lookup(duplicant)),
                BreathPercent = GetAmountPercent(Db.Get().Amounts.Breath.Lookup(duplicant)),
                CurrentTask = GetCurrentTask(duplicant)
            };

            int cell = Grid.PosToCell(duplicant);
            if (Grid.IsValidCell(cell)) {
                Element? element = Grid.Element[cell];
                snapshot.Element = element == null
                    ? "未知"
                    : global::STRINGS.UI.StripLinkFormatting(element.name);
                snapshot.TemperatureC = Grid.Temperature[cell] - 273.15f;
                snapshot.CellMassKg = Grid.Mass[cell];
            }
            return snapshot;
        }

        private static float? GetAmountPercent(AmountInstance amount) {
            if (amount == null)
                return null;
            float maximum = amount.GetMax();
            if (maximum <= 0f)
                return null;
            return Mathf.Clamp(amount.value / maximum * 100f, 0f, 100f);
        }

        private static string GetCurrentTask(GameObject duplicant) {
            ChoreDriver driver = duplicant.GetComponent<ChoreDriver>();
            Chore? chore = driver == null ? null : driver.GetCurrentChore();
            if (chore == null)
                return duplicant.HasTag(GameTags.Idle) ? "空闲" : "未知";

            string name = chore.GetType().Name;
            return name.EndsWith("Chore", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Chore".Length)
                : name;
        }

        private static IReadOnlyList<string> GetTraits(GameObject duplicant) {
            Traits traits = duplicant.GetComponent<Traits>();
            if (traits == null || traits.TraitList == null || traits.TraitList.Count == 0)
                return Array.Empty<string>();

            var names = new List<string>(traits.TraitList.Count);
            foreach (Trait trait in traits.TraitList) {
                if (trait != null && !string.IsNullOrWhiteSpace(trait.Name))
                    names.Add(global::STRINGS.UI.StripLinkFormatting(trait.Name));
            }
            return names;
        }
    }
}
