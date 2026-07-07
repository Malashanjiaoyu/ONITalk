using HarmonyLib;

namespace ONITalk.LocalizationSupport {
    [HarmonyPatch(typeof(global::Localization), nameof(global::Localization.Initialize))]
    internal static class LocalizationPatches {
        private static void Postfix() {
            ONITalkLocalization.ApplyCurrentLanguage();
        }
    }
}
