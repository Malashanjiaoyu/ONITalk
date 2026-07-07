using System;
using System.Reflection;
using HarmonyLib;
using ONITalk.Infrastructure;
using ONITalk.Runtime;

namespace ONITalk.Patches {
    [HarmonyPatch(typeof(TechInstance), nameof(TechInstance.Purchased))]
    internal static class TechPurchasedPatch {
        private static readonly FieldInfo? TechField = AccessTools.Field(
            typeof(TechInstance), "tech");

        internal static void Postfix(TechInstance __instance) {
            if (ONITalkService.Instance?.Config.MajorEventMemoryEnabled != true)
                return;
            try {
                Tech? tech = TechField?.GetValue(__instance) as Tech;
                string name = tech == null ? "未知科技" :
                    global::STRINGS.UI.StripLinkFormatting(tech.Name);
                ONITalkService.Instance?.RecordMajorEvent("科技",
                    "殖民地完成科技研究：" + name, 0.90f);
            } catch (Exception error) {
                Log.Warning("Could not record completed research. " +
                    error.Message);
            }
        }
    }

    [HarmonyPatch(typeof(MinionIdentity), "OnDied")]
    internal static class MinionIdentityOnDiedMemoryPatch {
        internal static void Prefix(MinionIdentity __instance, out string __state) {
            __state = __instance == null ? string.Empty : __instance.GetProperName();
        }

        internal static void Postfix(string __state) {
            if (ONITalkService.Instance?.Config.MajorEventMemoryEnabled == true &&
                    !string.IsNullOrWhiteSpace(__state))
                ONITalkService.Instance.RecordMajorEvent("死亡",
                    "复制人“" + __state + "”死亡", 1.0f);
        }
    }
}
