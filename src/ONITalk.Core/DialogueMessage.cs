using System;

namespace ONITalk.Core {
    /// <summary>
    /// One user-visible line in the colony-wide ONITalk history.
    /// </summary>
    public sealed class DialogueMessage {
        public DialogueMessage(DateTimeOffset timestamp, string speaker, string listener,
                string text) {
            Timestamp = timestamp;
            Speaker = (speaker ?? string.Empty).Trim();
            Listener = (listener ?? string.Empty).Trim();
            Text = (text ?? string.Empty).Trim();
        }

        public DateTimeOffset Timestamp { get; }

        public string Speaker { get; }

        public string Listener { get; }

        public string Text { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Speaker) &&
            !string.IsNullOrWhiteSpace(Text);
    }
}
