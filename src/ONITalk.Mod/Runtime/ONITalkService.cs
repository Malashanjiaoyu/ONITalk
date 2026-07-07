using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ONITalk.Bridge;
using ONITalk.Core;
using ONITalk.Infrastructure;
using ONITalk.LocalizationSupport;

namespace ONITalk.Runtime {
    internal sealed class ONITalkService : IDisposable {
        internal static ONITalkService? Instance { get; private set; }
        private static long nextSessionId;

        private readonly ONITalkConfig config;
        private readonly ConversationGate gate;
        private readonly PersistentConversationMemory memory;
        private readonly PersistentActionMemory actionMemory;
        private readonly PersistentColonyEventMemory eventMemory;
        private readonly OpenAICompatibleClient? client;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly object sessionSync = new object();
        private readonly object previewSync = new object();
        private readonly Random replyRandom = new Random();
        private readonly RemoteFailureBackoff remoteBackoff =
            new RemoteFailureBackoff(30f, 300f);
        private ColonyMemoryRepository? memoryRepository;
        private ConversationSession? activeSession;
        private MemoryInjectionSelection? lastInjectionSelection;
        private float memoryAttachElapsed;
        private float memoryFlushElapsed;
        private int memoryRevision;
        private int savedMemoryRevision;
        private long memoryEpoch;
        private bool warnedAboutProvider;
        private bool warnedAboutRemoteFailure;
        private bool warnedAboutChatContext;

        private ONITalkService(ONITalkConfig config) {
            this.config = config;
            gate = new ConversationGate(
                TimeSpan.FromSeconds(config.PairCooldownSeconds),
                TimeSpan.FromSeconds(config.GlobalCooldownSeconds));
            memory = new PersistentConversationMemory(config.MemoryLinesPerPair,
                config.MemoryMaxPairs);
            actionMemory = new PersistentActionMemory(config.ActionMemoryCapacityPerDupe,
                config.ActionAggregationWindowCycles);
            eventMemory = new PersistentColonyEventMemory(50);
            if (UsesRemoteProvider(config))
                client = new OpenAICompatibleClient(config);
        }

        internal static void Initialize(ONITalkConfig config) {
            Instance?.Dispose();
            Instance = new ONITalkService(config);
        }

        internal bool DiagnosticBubbleOnGameStart => config.DiagnosticBubbleOnGameStart;

        internal ONITalkConfig Config => config;

        internal float StateScanIntervalSeconds => config.StateScanIntervalSeconds;

        internal float ChatterIntervalSeconds => config.TestingMode
            ? config.TestingChatterIntervalSeconds
            : config.AmbientChatterIntervalSeconds;

        internal string GetInjectionPreviewText() {
            lock (previewSync) {
                return lastInjectionSelection == null
                    ? ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.RUNTIME.NO_INJECTION_PREVIEW)
                    : lastInjectionSelection.ToPreviewText();
            }
        }

        internal string GetMemoryLibraryText() {
            ColonyMemoryRepository? repository = memoryRepository;
            if (repository == null)
                return ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.RUNTIME.MEMORY_NOT_CONNECTED);
            return MemoryLibraryFormatter.Format(repository.ColonyName,
                repository.ColonyId, memory.Export(), actionMemory.Export(),
                eventMemory.Export());
        }

        internal string ExportCurrentMemory() {
            ColonyMemoryRepository? repository = memoryRepository;
            if (repository == null)
                return ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.RUNTIME.EXPORT_NOT_CONNECTED);
            try {
                string path = repository.ExportSnapshot(memory.Export(),
                    actionMemory.Export(), eventMemory.Export());
                Log.Info("Colony memory exported. Colony=" +
                    repository.ColonyName + ".");
                return ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.RUNTIME.EXPORT_SUCCESS, path);
            } catch (Exception error) {
                Log.Warning("Could not export colony memory. " + error.Message);
                return ONITalkLocalization.Format(STRINGS.ONITALK.UI.RUNTIME.EXPORT_FAILED,
                    error.Message);
            }
        }

        internal string ClearCurrentMemory() {
            ColonyMemoryRepository? repository = memoryRepository;
            if (repository == null)
                return ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.RUNTIME.CLEAR_NOT_CONNECTED);

            CancelActiveConversation("玩家清空了长期记忆");
            Interlocked.Increment(ref memoryEpoch);
            int relationships = memory.Export().Count;
            int actions = actionMemory.Count;
            int events = eventMemory.Count;
            memory.Import(null);
            actionMemory.Import(null);
            eventMemory.Import(null);
            lock (previewSync)
                lastInjectionSelection = null;
            int revision = Interlocked.Increment(ref memoryRevision);
            FlushMemory();
            bool saved = savedMemoryRevision == revision;
            Log.Info("Colony memory cleared. Colony=" + repository.ColonyName +
                ", relationships=" + relationships + ", actions=" + actions +
                ", events=" + events + ", saved=" + saved + ".");
            return saved
                ? ONITalkLocalization.Get(STRINGS.ONITALK.UI.RUNTIME.CLEAR_SUCCESS)
                : ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.RUNTIME.CLEAR_SAVE_FAILED);
        }

        public void Dispose() {
            CancelActiveConversation(null);
            Interlocked.Increment(ref memoryEpoch);
            shutdown.Cancel();
            FlushMemory();
            client?.Dispose();
        }

        internal void AttachColonyMemory() {
            if (!config.PersistentMemoryEnabled)
                return;

            ColonyIdentity? colony = ColonyIdentity.TryCapture();
            if (colony == null)
                return;
            if (memoryRepository != null && string.Equals(memoryRepository.ColonyId,
                    colony.Id, StringComparison.OrdinalIgnoreCase))
                return;

            CancelActiveConversation("殖民地已切换");
            FlushMemory();
            var repository = new ColonyMemoryRepository(colony);
            ColonyMemoryLoadResult loaded = repository.Load();
            Interlocked.Increment(ref memoryEpoch);
            memory.Import(loaded.Pairs);
            actionMemory.Import(loaded.Actions);
            eventMemory.Import(loaded.Events);
            memoryRepository = repository;
            memoryRevision = 0;
            savedMemoryRevision = 0;
            memoryAttachElapsed = 0f;
            memoryFlushElapsed = 0f;
            Log.Info("Colony memory attached. Colony=" + colony.Name + ", id=" +
                colony.Id + ", relationships=" + loaded.Pairs.Count + ", actions=" +
                loaded.Actions.Count + ", events=" + loaded.Events.Count + ".");
        }

        internal void DetachColonyMemory() {
            CancelActiveConversation(null);
            FlushMemory();
            Interlocked.Increment(ref memoryEpoch);
            memoryRepository = null;
            memory.Import(null);
            actionMemory.Import(null);
            eventMemory.Import(null);
            memoryRevision = 0;
            savedMemoryRevision = 0;
            memoryAttachElapsed = 0f;
            memoryFlushElapsed = 0f;
        }

        internal void TickPersistence(float dt) {
            if (!config.PersistentMemoryEnabled)
                return;

            if (memoryRepository == null) {
                memoryAttachElapsed += dt;
                if (memoryAttachElapsed >= 2f) {
                    memoryAttachElapsed = 0f;
                    AttachColonyMemory();
                }
                return;
            }

            if (Volatile.Read(ref memoryRevision) == savedMemoryRevision)
                return;
            memoryFlushElapsed += dt;
            if (memoryFlushElapsed >= 15f)
                FlushMemory();
        }

        internal void FlushMemory() {
            ColonyMemoryRepository? repository = memoryRepository;
            if (!config.PersistentMemoryEnabled || repository == null)
                return;

            int revision = Volatile.Read(ref memoryRevision);
            if (revision == savedMemoryRevision)
                return;
            if (repository.Save(memory.Export(), actionMemory.Export(),
                    eventMemory.Export())) {
                savedMemoryRevision = revision;
                memoryFlushElapsed = 0f;
            }
        }

        internal void RecordCompletedAction(CompletedActionSnapshot action) {
            if (!config.Enabled || !config.ActionMemoryEnabled || action == null)
                return;

            int cycle = GameClock.Instance == null ? -1 : GameClock.Instance.GetCycle();
            ActionMemoryUpdate? update = actionMemory.Record(action.Actor, action.Category,
                action.Target, cycle, action.Importance, action.IsRoutine);
            if (update == null)
                return;
            if (memoryRepository != null)
                Interlocked.Increment(ref memoryRevision);
            if ((!action.IsRoutine && (update.IsNew || update.Count <= 2 ||
                    update.Count % 5 == 0)) || (action.IsRoutine &&
                    (update.Count == ActionMemoryPolicy.RoutinePromotionCount ||
                    update.Count % 5 == 0)))
                Log.Info("Action memory recorded. Actor=" + action.Actor + ", " +
                    update.Summary + ".");
        }

        internal void RecordMajorEvent(string category, string content, float importance) {
            if (!config.Enabled || !config.MajorEventMemoryEnabled)
                return;
            int cycle = GameClock.Instance == null ? -1 : GameClock.Instance.GetCycle();
            if (!eventMemory.Record(category, content, cycle, importance))
                return;
            if (memoryRepository != null)
                Interlocked.Increment(ref memoryRevision);
            Log.Info("Major event memory recorded. " + content + ".");
        }

        internal void TickConversation(float dt) {
            remoteBackoff.Tick(dt);
            ConversationSession? session = null;
            int turnIndex = 0;
            lock (sessionSync) {
                ConversationSession? current = activeSession;
                if (current == null || current.State != SessionState.Waiting)
                    return;

                current.WaitElapsed += dt;
                if (current.WaitElapsed < config.ReplyDelaySeconds)
                    return;

                current.State = SessionState.Generating;
                session = current;
                turnIndex = current.NextTurnIndex;
            }

            if (session == null)
                return;
            if (session.Epoch != Interlocked.Read(ref memoryEpoch)) {
                FinishConversation(session, "存档上下文已变化");
                return;
            }
            if (!CanContinue(session.First, session.Second)) {
                FinishConversation(session, "复制人已离开交谈范围");
                return;
            }

            MinionIdentity speaker = turnIndex % 2 == 0
                ? session.First
                : session.Second;
            MinionIdentity listener = turnIndex % 2 == 0
                ? session.Second
                : session.First;
            try {
                var context = new ConversationContext(SnapshotFactory.Capture(speaker),
                    SnapshotFactory.Capture(listener), "回应上一句对话");
                _ = GenerateTurnAsync(session, speaker, context, turnIndex,
                    session.LastLine);
            } catch (Exception error) {
                FinishConversation(session, "无法读取下一轮状态");
                Log.Warning("Dialogue reply snapshot failed: " + error.Message);
            }
        }

        internal void OnLineDisplayed(long sessionId, int turnIndex) {
            ConversationSession? completed = null;
            lock (sessionSync) {
                ConversationSession? session = activeSession;
                if (session == null || session.Id != sessionId ||
                        session.State != SessionState.Queued ||
                        session.NextTurnIndex != turnIndex)
                    return;

                if (turnIndex + 1 >= session.TargetLines) {
                    activeSession = null;
                    completed = session;
                } else {
                    session.NextTurnIndex = turnIndex + 1;
                    session.WaitElapsed = 0f;
                    session.State = SessionState.Waiting;
                }
            }

            if (completed != null)
                completed.Cancellation.Cancel();
            if (completed != null)
                Log.Info("Dialogue session completed. Pair=" + completed.PairKey +
                    ", lines=" + completed.CompletedLines + ".");
        }

        internal void OnConversation(MinionIdentity? speaker, MinionIdentity? listener) {
            if (!config.Enabled)
                return;
            if (speaker == null || listener == null) {
                if (!warnedAboutChatContext) {
                    warnedAboutChatContext = true;
                    Log.Warning("Chat event did not resolve both Duplicants.");
                }
                return;
            }
            if (speaker == listener)
                return;
            if (!ConversationEligibility.CanTalk(speaker) ||
                    !ConversationEligibility.CanTalk(listener))
                return;

            TryStartConversation(speaker, listener, "复制人开始闲聊", null);
        }

        internal bool TryTriggerWorldEvent(ref int nextSpeakerIndex, bool allowAmbient) {
            var identities = Components.LiveMinionIdentities.Items;
            int count = identities.Count;
            if (!config.Enabled || count < 2)
                return false;

            int start = nextSpeakerIndex % count;
            float maxDistanceSquared = config.MaxConversationDistance *
                config.MaxConversationDistance;

            for (int offset = 0; offset < count; offset++) {
                int index = (start + offset) % count;
                MinionIdentity speaker = identities[index];
                if (!ConversationEligibility.CanTalk(speaker))
                    continue;

                DupeSnapshot snapshot = SnapshotFactory.Capture(speaker);
                string? trigger = StateTriggerEvaluator.Evaluate(snapshot,
                    config.LowBreathThresholdPercent,
                    config.HighStressThresholdPercent,
                    config.HighTemperatureC);
                if (trigger == null && !allowAmbient)
                    continue;

                MinionIdentity? listener = null;
                float nearestDistanceSquared = maxDistanceSquared;
                for (int listenerIndex = 0; listenerIndex < count; listenerIndex++) {
                    MinionIdentity candidate = identities[listenerIndex];
                    if (candidate == speaker ||
                            !ConversationEligibility.CanTalk(candidate))
                        continue;

                    float distanceSquared = (candidate.transform.position -
                        speaker.transform.position).sqrMagnitude;
                    if (distanceSquared <= nearestDistanceSquared) {
                        nearestDistanceSquared = distanceSquared;
                        listener = candidate;
                    }
                }

                if (listener == null)
                    continue;

                string selectedTrigger = trigger ?? (config.TestingMode
                    ? "测试模式：附近复制人闲聊"
                    : "附近复制人开始闲聊");
                if (!TryStartConversation(speaker, listener, selectedTrigger, snapshot))
                    continue;

                nextSpeakerIndex = (index + 1) % count;
                return true;
            }
            return false;
        }

        private bool TryStartConversation(MinionIdentity speaker, MinionIdentity listener,
                string trigger, DupeSnapshot? speakerSnapshot) {
            string speakerName = speaker.GetProperName();
            string listenerName = listener.GetProperName();
            string pairKey = ConversationGate.CreatePairKey(speakerName, listenerName);
            ConversationSession session;
            lock (sessionSync) {
                if (activeSession != null ||
                        !gate.TryAcquire(pairKey, DateTimeOffset.UtcNow))
                    return false;

                int targetLines = ShouldGenerateReplies()
                    ? config.MaxConversationLines
                    : 1;
                session = new ConversationSession(
                    Interlocked.Increment(ref nextSessionId),
                    Interlocked.Read(ref memoryEpoch), pairKey, trigger, speaker, listener,
                    GameClock.Instance == null ? -1 : GameClock.Instance.GetCycle(),
                    targetLines);
                activeSession = session;
            }

            Log.Info("Dialogue session started [" + trigger + "]: " + speakerName +
                " -> " + listenerName + ", targetLines=" + session.TargetLines + ".");

            try {
                var context = new ConversationContext(
                    speakerSnapshot ?? SnapshotFactory.Capture(speaker),
                    SnapshotFactory.Capture(listener), trigger);
                _ = GenerateTurnAsync(session, speaker, context, 0, null);
                return true;
            } catch (Exception error) {
                FinishConversation(session, "无法读取起始状态");
                Log.Warning("Dialogue start snapshot failed: " + error.Message);
                return false;
            }
        }

        private async Task GenerateTurnAsync(ConversationSession session,
                MinionIdentity speaker, ConversationContext context, int turnIndex,
                string? immediatePreviousLine) {
            try {
                if (!IsCurrent(session))
                    return;

                PairMemoryContext relationshipMemory;
                lock (sessionSync) {
                    if (!ReferenceEquals(activeSession, session) ||
                            session.Epoch != Interlocked.Read(ref memoryEpoch))
                        return;
                    relationshipMemory = memory.GetContext(session.PairKey);
                }
                IReadOnlyList<string> recentHistory = relationshipMemory.RecentLines;
                MemoryInjectionSelection injection = BuildMemorySelection(session.PairKey,
                    context, immediatePreviousLine, session.Cycle);
                lock (previewSync)
                    lastInjectionSelection = injection;
                string raw;
                bool providerConfigured = ProviderIsConfigured();
                if (client == null || !providerConfigured || !remoteBackoff.CanAttempt) {
                    if (client != null && !providerConfigured && !warnedAboutProvider) {
                        warnedAboutProvider = true;
                        Log.Warning("Remote provider is incomplete; falling back to echo mode.");
                    }
                    raw = EchoDialogueGenerator.Generate(context, recentHistory,
                        immediatePreviousLine, ONITalkLocalization.DialogueLanguageName);
                } else {
                    string systemPrompt = PromptBuilder.BuildSystemPrompt(
                        ONITalkLocalization.DialogueLanguageName, config.MaxCharacters,
                        config.CustomPromptEnabled ? config.CustomPrompt : null, context);
                    string userPrompt = PromptBuilder.BuildUserPrompt(context,
                        relationshipMemory, immediatePreviousLine, null, null, null,
                        injection);
                    try {
                        string? remote;
                        using (var turnCancellation =
                                CancellationTokenSource.CreateLinkedTokenSource(
                                    shutdown.Token, session.Cancellation.Token)) {
                            remote = await client.TryGenerateAsync(systemPrompt, userPrompt,
                                turnCancellation.Token).ConfigureAwait(false);
                        }
                        if (!string.IsNullOrWhiteSpace(remote)) {
                            remoteBackoff.RegisterSuccess();
                            warnedAboutRemoteFailure = false;
                        }
                        raw = string.IsNullOrWhiteSpace(remote)
                            ? EchoDialogueGenerator.Generate(context, recentHistory,
                                immediatePreviousLine,
                                ONITalkLocalization.DialogueLanguageName)
                            : remote;
                    } catch (OperationCanceledException) when (
                            shutdown.IsCancellationRequested ||
                            session.Cancellation.IsCancellationRequested) {
                        return;
                    } catch (Exception error) {
                        float retrySeconds = remoteBackoff.RegisterFailure();
                        if (!warnedAboutRemoteFailure) {
                            warnedAboutRemoteFailure = true;
                            Log.Warning("Remote dialogue request failed; using offline fallback. " +
                                "Retry in " + retrySeconds.ToString("0") + "s. " +
                                error.Message);
                        }
                        raw = EchoDialogueGenerator.Generate(context, recentHistory,
                            immediatePreviousLine,
                            ONITalkLocalization.DialogueLanguageName);
                    }
                }

                string line = LineSanitizer.Clean(raw, context.Speaker.Name,
                    config.MaxCharacters);
                if (string.IsNullOrWhiteSpace(line)) {
                    FinishConversation(session, "生成结果为空");
                    return;
                }
                if (shutdown.IsCancellationRequested ||
                        session.Cancellation.IsCancellationRequested || !IsCurrent(session))
                    return;

                lock (sessionSync) {
                    if (!ReferenceEquals(activeSession, session) ||
                            session.Epoch != Interlocked.Read(ref memoryEpoch))
                        return;
                    memory.Record(session.PairKey, context.Speaker.Name,
                        context.Listener.Name, line, session.Cycle);
                    if (memoryRepository != null)
                        Interlocked.Increment(ref memoryRevision);
                    session.LastLine = context.Speaker.Name + "：" + line;
                    session.CompletedLines = turnIndex + 1;
                    session.State = SessionState.Queued;
                }
                if (!ONITalkController.Enqueue(speaker, context.Speaker.Name,
                        context.Listener.Name, line, session.Id, turnIndex))
                    FinishConversation(session, "台词无法进入显示队列");
            } catch (Exception error) {
                FinishConversation(session, "生成失败");
                Log.Warning("Dialogue generation failed: " + error.Message);
            }
        }

        private MemoryInjectionSelection BuildMemorySelection(string pairKey,
                ConversationContext context, string? immediatePreviousLine,
                int currentCycle) {
            var candidates = new List<MemoryInjectionCandidate>();

            PairMemorySnapshot? pair = memory.Export().FirstOrDefault(item =>
                string.Equals(item.PairKey, pairKey, StringComparison.Ordinal));
            if (pair != null) {
                for (int index = 0; index < pair.RecentLines.Count; index++) {
                    ConversationMemoryLine line = pair.RecentLines[index];
                    if (line == null || string.IsNullOrWhiteSpace(line.Text))
                        continue;
                    candidates.Add(new MemoryInjectionCandidate {
                        Id = "relationship:" + pair.PairKey + ":" + index.ToString(
                            CultureInfo.InvariantCulture),
                        Kind = MemoryCandidateKind.Relationship,
                        Text = line.Speaker + "：" + line.Text.Trim(),
                        MatchText = line.Speaker + " " + line.Listener,
                        Cycle = line.Cycle,
                        Importance = 0.65f,
                        Sequence = pair.LastUpdatedSequence * 100L + index
                    });
                }
            }

            if (config.ActionMemoryEnabled) {
                foreach (ActionMemorySnapshot action in actionMemory.Export().Where(
                        ActionMemoryPolicy.IsEligibleForInjection)) {
                    MemoryCandidateKind? kind = null;
                    if (string.Equals(action.Actor, context.Speaker.Name,
                            StringComparison.Ordinal))
                        kind = MemoryCandidateKind.SpeakerAction;
                    else if (string.Equals(action.Actor, context.Listener.Name,
                            StringComparison.Ordinal))
                        kind = MemoryCandidateKind.ListenerAction;
                    if (!kind.HasValue)
                        continue;
                    candidates.Add(new MemoryInjectionCandidate {
                        Id = "action:" + action.LastUpdatedSequence.ToString(
                            CultureInfo.InvariantCulture),
                        Kind = kind.Value,
                        Text = PersistentActionMemory.FormatSummary(action),
                        MatchText = action.Actor + " " + action.Category + " " +
                            action.Target,
                        Cycle = action.LastCycle,
                        Importance = action.Importance,
                        Sequence = action.LastUpdatedSequence
                    });
                }
            }

            if (config.MajorEventMemoryEnabled) {
                foreach (ColonyEventMemorySnapshot colonyEvent in eventMemory.Export()) {
                    candidates.Add(new MemoryInjectionCandidate {
                        Id = "event:" + colonyEvent.LastUpdatedSequence.ToString(
                            CultureInfo.InvariantCulture),
                        Kind = MemoryCandidateKind.ColonyEvent,
                        Text = "周期 " + colonyEvent.Cycle.ToString(
                            CultureInfo.InvariantCulture) + "：" + colonyEvent.Content,
                        MatchText = colonyEvent.Category + " " + colonyEvent.Content,
                        Cycle = colonyEvent.Cycle,
                        Importance = colonyEvent.Importance,
                        Sequence = colonyEvent.LastUpdatedSequence
                    });
                }
            }

            string contextLabel = context.Speaker.Name + " → " +
                context.Listener.Name + " · " + context.Trigger;
            string searchText = string.Join(" ", new[] {
                context.Trigger,
                context.Speaker.Name,
                context.Listener.Name,
                context.Speaker.CurrentTask,
                context.Listener.CurrentTask,
                context.Speaker.Element,
                context.Listener.Element,
                immediatePreviousLine ?? string.Empty
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return SmartMemoryInjectionEngine.Select(contextLabel, searchText,
                currentCycle, candidates, config.MemoryPreset, config.MemoryTokenBudget);
        }

        private bool ShouldGenerateReplies() {
            if (!config.ConversationRepliesEnabled || config.MaxConversationLines <= 1 ||
                    config.ReplyChancePercent <= 0)
                return false;
            return config.ReplyChancePercent >= 100 ||
                replyRandom.Next(100) < config.ReplyChancePercent;
        }

        private bool CanContinue(MinionIdentity first, MinionIdentity second) {
            if (first == second || !ConversationEligibility.CanTalk(first) ||
                    !ConversationEligibility.CanTalk(second))
                return false;

            float maxDistance = config.MaxConversationDistance;
            return (first.transform.position - second.transform.position).sqrMagnitude <=
                maxDistance * maxDistance;
        }

        private bool IsCurrent(ConversationSession session) {
            lock (sessionSync) {
                return ReferenceEquals(activeSession, session) &&
                    session.Epoch == Interlocked.Read(ref memoryEpoch);
            }
        }

        private void CancelActiveConversation(string? reason) {
            ConversationSession? session;
            lock (sessionSync) {
                session = activeSession;
                activeSession = null;
            }
            session?.Cancellation.Cancel();
            if (session != null && !string.IsNullOrWhiteSpace(reason))
                Log.Info("Dialogue session stopped. Pair=" + session.PairKey +
                    ", reason=" + reason + ".");
        }

        private void FinishConversation(ConversationSession session, string reason) {
            bool removed = false;
            lock (sessionSync) {
                if (ReferenceEquals(activeSession, session)) {
                    activeSession = null;
                    removed = true;
                }
            }
            if (removed)
                session.Cancellation.Cancel();
            if (removed)
                Log.Info("Dialogue session stopped. Pair=" + session.PairKey +
                    ", reason=" + reason + ", lines=" + session.CompletedLines + ".");
        }

        private bool ProviderIsConfigured() {
            ProviderProfile profile = ProviderProfileCatalog.Get(config.Provider);
            if (profile.IsOffline)
                return false;
            bool hasConnection = !string.IsNullOrWhiteSpace(config.Endpoint) &&
                !string.IsNullOrWhiteSpace(config.Model);
            if (!hasConnection)
                return false;
            return !profile.ApiKeyRequired || !string.IsNullOrWhiteSpace(config.ApiKey);
        }

        private static bool UsesRemoteProvider(ONITalkConfig config) {
            return !ProviderProfileCatalog.Get(config.Provider).IsOffline;
        }

        private enum SessionState {
            Generating,
            Queued,
            Waiting
        }

        private sealed class ConversationSession {
            internal ConversationSession(long id, long epoch, string pairKey,
                    string trigger, MinionIdentity first, MinionIdentity second, int cycle,
                    int targetLines) {
                Id = id;
                Epoch = epoch;
                PairKey = pairKey;
                Trigger = trigger;
                First = first;
                Second = second;
                Cycle = cycle;
                TargetLines = Math.Max(1, targetLines);
            }

            internal long Id { get; }

            internal long Epoch { get; }

            internal string PairKey { get; }

            internal string Trigger { get; }

            internal MinionIdentity First { get; }

            internal MinionIdentity Second { get; }

            internal int Cycle { get; }

            internal int TargetLines { get; }

            internal CancellationTokenSource Cancellation { get; } =
                new CancellationTokenSource();

            internal int NextTurnIndex { get; set; }

            internal int CompletedLines { get; set; }

            internal float WaitElapsed { get; set; }

            internal string? LastLine { get; set; }

            internal SessionState State { get; set; } = SessionState.Generating;
        }
    }
}
