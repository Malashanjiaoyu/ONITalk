using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    /// <summary>
    /// Bounded, thread-safe session history used by the in-game chat window.
    /// This is intentionally separate from the short per-pair prompt memory.
    /// </summary>
    public sealed class DialogueHistory {
        private readonly Queue<DialogueMessage> messages;
        private readonly object sync;
        private int capacity;

        public DialogueHistory(int capacity = 100) {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            messages = new Queue<DialogueMessage>(capacity);
            sync = new object();
        }

        public int Capacity {
            get {
                lock (sync)
                    return capacity;
            }
        }

        public int Count {
            get {
                lock (sync)
                    return messages.Count;
            }
        }

        public bool Add(DialogueMessage message) {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!message.IsValid)
                return false;

            lock (sync) {
                messages.Enqueue(message);
                TrimToCapacity();
            }
            return true;
        }

        public void Clear() {
            lock (sync)
                messages.Clear();
        }

        public void SetCapacity(int newCapacity) {
            if (newCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(newCapacity));
            lock (sync) {
                capacity = newCapacity;
                TrimToCapacity();
            }
        }

        public IReadOnlyList<DialogueMessage> Snapshot() {
            lock (sync)
                return messages.ToArray();
        }

        private void TrimToCapacity() {
            while (messages.Count > capacity)
                messages.Dequeue();
        }
    }
}
