using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    public sealed class ConversationMemory {
        private readonly int capacity;
        private readonly Dictionary<string, Queue<string>> histories;
        private readonly object sync;

        public ConversationMemory(int capacity = 6) {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            this.capacity = capacity;
            histories = new Dictionary<string, Queue<string>>(StringComparer.Ordinal);
            sync = new object();
        }

        public void Add(string pairKey, string line) {
            if (string.IsNullOrWhiteSpace(pairKey) || string.IsNullOrWhiteSpace(line))
                return;

            lock (sync) {
                if (!histories.TryGetValue(pairKey, out Queue<string>? history)) {
                    history = new Queue<string>(capacity);
                    histories.Add(pairKey, history);
                }

                history.Enqueue(line.Trim());
                while (history.Count > capacity)
                    history.Dequeue();
            }
        }

        public IReadOnlyList<string> Get(string pairKey) {
            lock (sync) {
                return histories.TryGetValue(pairKey, out Queue<string>? history)
                    ? history.ToArray()
                    : Array.Empty<string>();
            }
        }
    }
}

