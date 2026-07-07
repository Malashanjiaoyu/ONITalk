namespace ONITalk.Core {
    public sealed class ConversationContext {
        public ConversationContext(DupeSnapshot speaker, DupeSnapshot listener, string trigger) {
            Speaker = speaker;
            Listener = listener;
            Trigger = trigger;
        }

        public DupeSnapshot Speaker { get; }

        public DupeSnapshot Listener { get; }

        public string Trigger { get; }
    }
}

