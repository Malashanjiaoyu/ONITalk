using HarmonyLib;
using ONITalk.Bridge;
using ONITalk.Runtime;

namespace ONITalk.Patches {
    [HarmonyPatch(typeof(Workable), nameof(Workable.CompleteWork))]
    internal static class WorkableCompleteWorkPatch {
        internal static void Prefix(Workable __instance, WorkerBase worker,
                out CompletedActionSnapshot? __state) {
            __state = ONITalkService.Instance?.Config.ActionMemoryEnabled == true
                ? ActionMemoryCapture.TryCapture(__instance, worker)
                : null;
        }

        internal static void Postfix(CompletedActionSnapshot? __state) {
            if (__state != null)
                ONITalkService.Instance?.RecordCompletedAction(__state);
        }
    }
}
