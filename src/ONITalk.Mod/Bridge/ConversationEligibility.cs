namespace ONITalk.Bridge {
    internal static class ConversationEligibility {
        internal static bool CanTalk(MinionIdentity? identity) {
            if (identity == null || identity.gameObject == null ||
                    !identity.gameObject.activeInHierarchy)
                return false;

            return !identity.gameObject.HasTag(GameTags.Asleep) &&
                !identity.gameObject.HasTag(GameTags.Dead) &&
                !identity.gameObject.HasTag(GameTags.Dying) &&
                !identity.gameObject.HasTag(GameTags.Incapacitated) &&
                !identity.gameObject.HasTag(GameTags.Entombed) &&
                !identity.gameObject.HasTag(GameTags.Stored);
        }
    }
}
