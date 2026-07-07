using System;
using System.Collections.Concurrent;
using ONITalk.Core;
using ONITalk.Infrastructure;
using ONITalk.LocalizationSupport;
using ONITalk.UI;
using UnityEngine;

namespace ONITalk.Runtime {
    [SkipSaveFileSerialization]
    public sealed class ONITalkController : KMonoBehaviour, ISim200ms {
        private static readonly ConcurrentQueue<DialogueResult> Pending =
            new ConcurrentQueue<DialogueResult>();

        private readonly DialogueHistory history = new DialogueHistory();
        private ONITalkChatPanel? chatPanel;
        private ONITalkMemoryPreviewPanel? memoryPreviewPanel;
        private ONITalkMemoryLibraryPanel? memoryLibraryPanel;
        private bool chatPanelFailed;
        private float diagnosticElapsed;
        private bool diagnosticQueued;
        private float stateScanElapsed;
        private float chatterElapsed;
        private int nextSpeakerIndex;
        private bool triggerScanFailed;

        internal static ONITalkController? Instance { get; private set; }

        internal static void ApplyOptions(ONITalkConfig config) {
            Instance?.ApplyOptionsInternal(config);
        }

        internal static bool Enqueue(MinionIdentity speaker, string speakerName,
                string listenerName, string line, long sessionId = 0,
                int turnIndex = 0) {
            if (ReferenceEquals(speaker, null) || string.IsNullOrWhiteSpace(speakerName) ||
                    string.IsNullOrWhiteSpace(line))
                return false;
            Pending.Enqueue(new DialogueResult(speaker, speakerName, listenerName, line,
                sessionId, turnIndex));
            return true;
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            Instance = this;
            ONITalkConfig? config = ONITalkService.Instance?.Config;
            if (config != null)
                history.SetCapacity(config.ChatHistoryLimit);
            ONITalkService.Instance?.AttachColonyMemory();
        }

        protected override void OnCleanUp() {
            ONITalkService.Instance?.DetachColonyMemory();
            if (Instance == this)
                Instance = null;
            if (chatPanel != null)
                Destroy(chatPanel.gameObject);
            chatPanel = null;
            if (memoryPreviewPanel != null)
                Destroy(memoryPreviewPanel.gameObject);
            memoryPreviewPanel = null;
            if (memoryLibraryPanel != null)
                Destroy(memoryLibraryPanel.gameObject);
            memoryLibraryPanel = null;
            history.Clear();
            while (Pending.TryDequeue(out _)) {
            }
            base.OnCleanUp();
        }

        public void Sim200ms(float dt) {
            ONITalkService.Instance?.TickPersistence(dt);
            ONITalkService.Instance?.TickConversation(dt);
            TryEnsureChatPanel();
            TryQueueDiagnostic(dt);
            TryScanTriggers(dt);

            int shown = 0;
            while (shown < 3 && Pending.TryDequeue(out DialogueResult result)) {
                Show(result);
                shown++;
            }
        }

        private void ApplyOptionsInternal(ONITalkConfig config) {
            history.SetCapacity(config.ChatHistoryLimit);
            chatPanel?.ApplyOptions(config);
            ONITalkService.Instance?.AttachColonyMemory();
            if (config.ChatWindowEnabled && chatPanel == null)
                chatPanelFailed = false;
        }

        private void ClearHistory() {
            history.Clear();
        }

        private void TryEnsureChatPanel() {
            if (chatPanel != null || chatPanelFailed)
                return;

            ONITalkConfig? config = ONITalkService.Instance?.Config;
            GameScreenManager? manager = GameScreenManager.Instance;
            if (config == null || !config.ChatWindowEnabled || manager == null ||
                    manager.ssOverlayCanvas == null)
                return;

            try {
                chatPanel = ONITalkChatPanel.Create(manager.ssOverlayCanvas, config,
                    ClearHistory, ToggleMemoryPreview, ToggleMemoryLibrary);
                chatPanel.Rebuild(history.Snapshot());
                Log.Info("Chat history window attached.");
            } catch (Exception error) {
                chatPanelFailed = true;
                Log.Warning("Chat history window could not be created. " + error);
            }
        }

        private void ToggleMemoryPreview() {
            if (memoryPreviewPanel != null) {
                Destroy(memoryPreviewPanel.gameObject);
                memoryPreviewPanel = null;
                return;
            }

            if (memoryLibraryPanel != null) {
                Destroy(memoryLibraryPanel.gameObject);
                memoryLibraryPanel = null;
            }

            GameScreenManager? manager = GameScreenManager.Instance;
            if (manager == null || manager.ssOverlayCanvas == null)
                return;
            string preview = ONITalkService.Instance?.GetInjectionPreviewText() ??
                ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.RUNTIME.SERVICE_NOT_INITIALIZED);
            try {
                memoryPreviewPanel = ONITalkMemoryPreviewPanel.Create(
                    manager.ssOverlayCanvas, preview, () => memoryPreviewPanel = null);
            } catch (Exception error) {
                memoryPreviewPanel = null;
                Log.Warning("Memory injection preview could not be created. " + error);
            }
        }

        private void ToggleMemoryLibrary() {
            if (memoryLibraryPanel != null) {
                Destroy(memoryLibraryPanel.gameObject);
                memoryLibraryPanel = null;
                return;
            }
            if (memoryPreviewPanel != null) {
                Destroy(memoryPreviewPanel.gameObject);
                memoryPreviewPanel = null;
            }

            GameScreenManager? manager = GameScreenManager.Instance;
            ONITalkService? service = ONITalkService.Instance;
            if (manager == null || manager.ssOverlayCanvas == null || service == null)
                return;
            try {
                memoryLibraryPanel = ONITalkMemoryLibraryPanel.Create(
                    manager.ssOverlayCanvas, service.GetMemoryLibraryText,
                    service.ExportCurrentMemory, service.ClearCurrentMemory,
                    () => memoryLibraryPanel = null);
            } catch (Exception error) {
                memoryLibraryPanel = null;
                Log.Warning("Memory library panel could not be created. " + error);
            }
        }

        private void TryScanTriggers(float dt) {
            if (triggerScanFailed)
                return;

            ONITalkService? service = ONITalkService.Instance;
            if (service == null)
                return;

            stateScanElapsed += dt;
            chatterElapsed += dt;
            if (stateScanElapsed < service.StateScanIntervalSeconds)
                return;

            stateScanElapsed = 0f;
            bool allowAmbient = chatterElapsed >= service.ChatterIntervalSeconds;
            try {
                bool accepted = service.TryTriggerWorldEvent(ref nextSpeakerIndex,
                    allowAmbient);
                if (accepted && allowAmbient)
                    chatterElapsed = 0f;
            } catch (Exception error) {
                triggerScanFailed = true;
                Log.Warning("Trigger scan disabled after an error: " + error.Message);
            }
        }

        private void TryQueueDiagnostic(float dt) {
            if (diagnosticQueued ||
                    ONITalkService.Instance?.DiagnosticBubbleOnGameStart != true)
                return;

            diagnosticElapsed += dt;
            if (diagnosticElapsed < 3f)
                return;

            foreach (MinionIdentity identity in Components.LiveMinionIdentities.Items) {
                if (identity == null)
                    continue;

                diagnosticQueued = true;
                Enqueue(identity, identity.GetProperName(), string.Empty,
                    ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.RUNTIME.DIAGNOSTIC_BUBBLE));
                Log.Info("Diagnostic bubble queued for " + identity.GetProperName() + ".");
                return;
            }
        }

        private void Show(DialogueResult result) {
            string text = result.SpeakerName + "：" + result.Line;
            MinionIdentity speaker = result.Speaker;
            PopFXManager manager = PopFXManager.Instance;
            if (speaker != null && manager != null) {
                Vector3 position = speaker.transform.position + new Vector3(0f, 1.3f, 0f);
                manager.SpawnFX(manager.sprite_Plus, text, null, position);
            }
            Log.Info(text);

            var message = new DialogueMessage(DateTimeOffset.Now, result.SpeakerName,
                result.ListenerName, result.Line);
            if (history.Add(message))
                chatPanel?.AddMessage(message);
            if (result.SessionId != 0)
                ONITalkService.Instance?.OnLineDisplayed(result.SessionId,
                    result.TurnIndex);
        }

        private sealed class DialogueResult {
            internal DialogueResult(MinionIdentity speaker, string speakerName,
                    string listenerName, string line, long sessionId, int turnIndex) {
                Speaker = speaker;
                SpeakerName = speakerName;
                ListenerName = listenerName ?? string.Empty;
                Line = line;
                SessionId = sessionId;
                TurnIndex = turnIndex;
            }

            internal MinionIdentity Speaker { get; }

            internal string SpeakerName { get; }

            internal string ListenerName { get; }

            internal string Line { get; }

            internal long SessionId { get; }

            internal int TurnIndex { get; }
        }
    }
}
