using HarmonyLib;
using ONITalk.Runtime;

namespace ONITalk.Patches {
    // OnStartedTalking is private in current game builds, so nameof cannot access it.
    [HarmonyPatch(typeof(Chatty), "OnStartedTalking")]
    internal static class ChattyOnStartedTalkingPatch {
        internal static void Postfix(object data, Chatty __instance) {
            MinionIdentity? speaker = __instance == null
                ? null
                : __instance.GetComponent<MinionIdentity>();
            MinionIdentity? listener = data as MinionIdentity;

            if (listener == null && data is ConversationManager.StartedTalkingEvent evt &&
                    evt.talker != null)
                evt.talker.TryGetComponent(out listener);

            ONITalkService.Instance?.OnConversation(speaker, listener);
        }
    }
}
