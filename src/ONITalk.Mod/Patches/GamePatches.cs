using HarmonyLib;
using ONITalk.Infrastructure;
using ONITalk.Runtime;

namespace ONITalk.Patches {
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class GameOnPrefabInitPatch {
        internal static void Postfix(Game __instance) {
            __instance.gameObject.AddOrGet<ONITalkController>();
            Log.Info("Game controller attached.");
        }
    }
}
