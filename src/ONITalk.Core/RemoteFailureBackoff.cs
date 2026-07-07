using System;

namespace ONITalk.Core {
    public sealed class RemoteFailureBackoff {
        private readonly float initialSeconds;
        private readonly float maximumSeconds;
        private int consecutiveFailures;
        private float remainingSeconds;

        public RemoteFailureBackoff(float initialSeconds = 30f,
                float maximumSeconds = 300f) {
            this.initialSeconds = Math.Max(1f, initialSeconds);
            this.maximumSeconds = Math.Max(this.initialSeconds, maximumSeconds);
        }

        public bool CanAttempt => remainingSeconds <= 0f;

        public int ConsecutiveFailures => consecutiveFailures;

        public float RemainingSeconds => Math.Max(0f, remainingSeconds);

        public float RegisterFailure() {
            consecutiveFailures = Math.Min(16, consecutiveFailures + 1);
            double delay = initialSeconds * Math.Pow(2d, consecutiveFailures - 1);
            remainingSeconds = (float)Math.Min(maximumSeconds, delay);
            return remainingSeconds;
        }

        public void RegisterSuccess() {
            consecutiveFailures = 0;
            remainingSeconds = 0f;
        }

        public void Tick(float seconds) {
            if (seconds > 0f && remainingSeconds > 0f)
                remainingSeconds = Math.Max(0f, remainingSeconds - seconds);
        }
    }
}
