using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    public sealed class ConversationGate {
        private readonly TimeSpan globalCooldown;
        private readonly TimeSpan pairCooldown;
        private readonly Dictionary<string, DateTimeOffset> pairLastUsed;
        private readonly object sync;
        private DateTimeOffset? globalLastUsed;

        public ConversationGate(TimeSpan pairCooldown, TimeSpan globalCooldown) {
            if (pairCooldown < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(pairCooldown));
            if (globalCooldown < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(globalCooldown));

            this.pairCooldown = pairCooldown;
            this.globalCooldown = globalCooldown;
            pairLastUsed = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
            sync = new object();
        }

        public static string CreatePairKey(string first, string second) {
            first = first ?? string.Empty;
            second = second ?? string.Empty;
            return string.CompareOrdinal(first, second) <= 0
                ? first + "\u001f" + second
                : second + "\u001f" + first;
        }

        public bool TryAcquire(string pairKey, DateTimeOffset now) {
            if (string.IsNullOrWhiteSpace(pairKey))
                return false;

            lock (sync) {
                if (globalLastUsed.HasValue && now - globalLastUsed.Value < globalCooldown)
                    return false;

                if (pairLastUsed.TryGetValue(pairKey, out DateTimeOffset lastPairUse) &&
                        now - lastPairUse < pairCooldown)
                    return false;

                globalLastUsed = now;
                pairLastUsed[pairKey] = now;
                return true;
            }
        }
    }
}

