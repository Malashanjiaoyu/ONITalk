using System;
using System.Collections.Generic;
using System.Linq;
using ONITalk.Core;

namespace ONITalk.Core.Tests {
    internal static class Program {
        private static int failures;

        private static int Main() {
            TestConversationGate();
            TestPromptBuilder();
            TestLineSanitizer();
            TestEchoGenerator();
            TestStateTriggerEvaluator();
            TestDialogueHistory();
            TestPersistentConversationMemory();
            TestPersistentActionMemory();
            TestActionMemoryPolicy();
            TestPersistentColonyEventMemory();
            TestActionCategoryClassifier();
            TestSmartMemoryInjection();
            TestProviderProfiles();
            TestRemoteFailureBackoff();
            TestMemoryLibraryFormatter();

            if (failures == 0) {
                Console.WriteLine("All ONITalk core checks passed.");
                return 0;
            }

            Console.Error.WriteLine(failures + " ONITalk core check(s) failed.");
            return 1;
        }

        private static void TestConversationGate() {
            var gate = new ConversationGate(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            DateTimeOffset now = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
            string pair = ConversationGate.CreatePairKey("Mi-Ma", "Hassan");

            Check(gate.TryAcquire(pair, now), "first conversation is accepted");
            Check(!gate.TryAcquire(pair, now.AddSeconds(6)), "pair cooldown blocks a repeat");
            Check(gate.TryAcquire(pair, now.AddSeconds(31)), "pair cooldown expires");
            Check(pair == ConversationGate.CreatePairKey("Hassan", "Mi-Ma"),
                "pair key is order independent");
        }

        private static void TestPromptBuilder() {
            ConversationContext context = CreateContext();
            string system = PromptBuilder.BuildSystemPrompt("简体中文", 80);
            string pair = ConversationGate.CreatePairKey("米玛", "哈桑");
            var memory = new PersistentConversationMemory();
            memory.Record(pair, "哈桑", "米玛", "上次也是你修的厕所。", 42);
            string user = PromptBuilder.BuildUserPrompt(context, memory.GetContext(pair));

            Check(system.Contains("不要提到AI"), "system prompt protects immersion");
            Check(user.Contains("米玛") && user.Contains("85%") && user.Contains("污氧"),
                "user prompt includes live dupe context");
            Check(user.Contains("最近共同经历"), "user prompt includes memory");
            Check(user.Contains("周期 42") && user.Contains("刚开始交谈"),
                "user prompt includes persistent relationship context");

            string reply = PromptBuilder.BuildUserPrompt(context, memory.GetContext(pair),
                "哈桑：这根管子肯定接对了。");
            Check(reply.Contains("对方刚刚说") && reply.Contains("必须自然回应"),
                "reply prompt requires immediate conversational continuity");

            var actions = new PersistentActionMemory();
            actions.Record("米玛", "完成修理", "电解器", 41, 0.8f);
            string actionPrompt = PromptBuilder.BuildUserPrompt(context,
                memory.GetContext(pair), null, actions.GetContext("米玛", 3), null);
            Check(actionPrompt.Contains("游戏确认发生的事实") &&
                actionPrompt.Contains("完成修理") && actionPrompt.Contains("电解器"),
                "user prompt includes verified action memory");

            var events = new PersistentColonyEventMemory();
            events.Record("科技", "殖民地完成科技研究：基础农业", 42, 0.9f);
            string eventPrompt = PromptBuilder.BuildUserPrompt(context,
                memory.GetContext(pair), null, null, null, events.GetContext(3));
            Check(eventPrompt.Contains("殖民地近期重大事件") &&
                eventPrompt.Contains("基础农业"),
                "user prompt includes verified colony events");

            string customized = PromptBuilder.BuildSystemPrompt("Español", 80,
                "Make {speaker} answer {listener} about {trigger} in {language}.", context);
            Check(customized.Contains("米玛") && customized.Contains("哈桑") &&
                    customized.Contains("Español") && customized.Contains("不得覆盖"),
                "custom prompt expands variables behind protected core rules");
            Check(PromptCustomization.FindUnknownVariables("Use {speaker} and {mood}")
                    .Single() == "mood",
                "custom prompt reports unknown variables");
        }

        private static void TestLineSanitizer() {
            string clean = LineSanitizer.Clean("\n“米玛：  厕所又堵了。 ”\n", "米玛", 80);
            Check(clean == "厕所又堵了。", "sanitizer removes wrappers and speaker prefix");

            string longLine = LineSanitizer.Clean(new string('氧', 30), "米玛", 20);
            Check(longLine.Length == 20 && longLine.EndsWith("…"),
                "sanitizer limits bubble length");
        }

        private static void TestEchoGenerator() {
            ConversationContext context = CreateContext();
            string line = EchoDialogueGenerator.Generate(context);
            Check(line.Contains("压力"), "echo mode reacts to context without an API");

            var history = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < 4; index++) {
                string next = EchoDialogueGenerator.Generate(context, history);
                unique.Add(next);
                history.Add(context.Speaker.Name + "：" + next);
            }
            Check(unique.Count == 4, "echo mode avoids recent repeated lines");

            string reply = EchoDialogueGenerator.Generate(context, history,
                "哈桑：这根管子肯定接对了。");
            Check(!string.IsNullOrWhiteSpace(reply) &&
                (reply.Contains("哈桑") || reply.Contains("你")),
                "echo mode can produce a direct reply");

            string english = EchoDialogueGenerator.Generate(context, history, null,
                "English");
            string spanish = EchoDialogueGenerator.Generate(context, history, null,
                "Español");
            Check(english.IndexOfAny(new[] { '氧', '压', '温' }) < 0,
                "offline fallback follows English interface language");
            Check(spanish.Contains("estrés") || spanish.Contains("colonia") ||
                    spanish.Contains("alerta"),
                "offline fallback follows Spanish interface language");
        }

        private static void TestStateTriggerEvaluator() {
            var snapshot = new DupeSnapshot {
                BreathPercent = 20f,
                StressPercent = 85f,
                TemperatureC = 60f
            };
            Check(StateTriggerEvaluator.Evaluate(snapshot, 35f, 70f, 45f) ==
                "复制人呼吸不足", "low breath has highest trigger priority");

            snapshot.BreathPercent = 90f;
            Check(StateTriggerEvaluator.Evaluate(snapshot, 35f, 70f, 45f) ==
                "复制人压力过高", "high stress creates a trigger");

            snapshot.StressPercent = 10f;
            snapshot.TemperatureC = 20f;
            Check(StateTriggerEvaluator.Evaluate(snapshot, 35f, 70f, 45f) == null,
                "normal state does not create an urgent trigger");
        }

        private static void TestDialogueHistory() {
            var history = new DialogueHistory(2);
            DateTimeOffset now = new DateTimeOffset(2026, 6, 28, 14, 0, 0,
                TimeSpan.Zero);
            Check(history.Add(new DialogueMessage(now, "Mi-Ma", "Hassan", "First")),
                "chat history accepts a valid line");
            history.Add(new DialogueMessage(now.AddSeconds(1), "Hassan", "Mi-Ma", "Second"));
            history.Add(new DialogueMessage(now.AddSeconds(2), "Ada", "Mi-Ma", "Third"));

            IReadOnlyList<DialogueMessage> snapshot = history.Snapshot();
            Check(snapshot.Count == 2 && snapshot[0].Text == "Second" &&
                snapshot[1].Text == "Third", "chat history keeps newest lines in order");

            history.SetCapacity(1);
            Check(history.Count == 1 && history.Snapshot()[0].Text == "Third",
                "chat history trims immediately when capacity changes");
            Check(!history.Add(new DialogueMessage(now, "", "Mi-Ma", "Ignored")),
                "chat history rejects invalid lines");
            history.Clear();
            Check(history.Count == 0, "chat history can be cleared");
        }

        private static void TestPersistentConversationMemory() {
            string pair = ConversationGate.CreatePairKey("Mi-Ma", "Hassan");
            var memory = new PersistentConversationMemory(2, 2);
            memory.Record(pair, "Mi-Ma", "Hassan", "First", 10);
            memory.Record(pair, "Hassan", "Mi-Ma", "Second", 11);
            memory.Record(pair, "Mi-Ma", "Hassan", "Third", 12);

            PairMemoryContext context = memory.GetContext(pair);
            Check(context.TotalLines == 3 && context.RecentLines.Count == 2 &&
                context.RecentLines[0].Contains("Second") && context.LastCycle == 12,
                "persistent memory keeps totals while trimming recent lines");
            Check(context.Familiarity == "逐渐熟悉",
                "persistent memory derives a conservative familiarity tier");

            var restored = new PersistentConversationMemory(2, 2);
            restored.Import(memory.Export());
            PairMemoryContext restoredContext = restored.GetContext(pair);
            Check(restoredContext.TotalLines == 3 &&
                restoredContext.RecentLines[1].Contains("Third"),
                "persistent memory survives export and import");

            restored.Record("Ada|Hassan", "Ada", "Hassan", "A", 13);
            restored.Record("Ada|Mi-Ma", "Ada", "Mi-Ma", "B", 14);
            Check(restored.Export().Count == 2 &&
                restored.GetContext(pair).TotalLines == 0,
                "persistent memory evicts the least recently used pair");
        }

        private static void TestPersistentActionMemory() {
            var memory = new PersistentActionMemory(2, 2);
            ActionMemoryUpdate? first = memory.Record("Mi-Ma", "完成建造",
                "电解器", 10, 0.7f);
            ActionMemoryUpdate? merged = memory.Record("Mi-Ma", "完成建造",
                "电解器", 11, 0.7f);

            Check(first?.IsNew == true && merged?.IsNew == false &&
                merged.Count == 2 && merged.Summary.Contains("共 2 次"),
                "action memory aggregates repeated completed work");

            memory.Record("Mi-Ma", "完成建造", "电解器", 14, 0.7f);
            Check(memory.Export().Count == 2,
                "action memory starts a new episode outside the aggregation window");

            memory.Record("Mi-Ma", "完成修理", "水泵", 15, 0.9f);
            ActionMemoryContext context = memory.GetContext("Mi-Ma", 5);
            Check(context.RecentActions.Count == 2 &&
                context.RecentActions[0].Contains("水泵"),
                "action memory caps each Duplicant and keeps newest episodes");

            memory.Record("Hassan", "完成研究", "基础农业", 16, 0.8f);
            Check(memory.GetContext("Hassan", 3).RecentActions.Count == 1,
                "action memory capacity is isolated per Duplicant");

            var restored = new PersistentActionMemory(2, 2);
            restored.Import(memory.Export());
            Check(restored.GetContext("Hassan", 3).RecentActions[0]
                    .Contains("基础农业"),
                "action memory survives export and import");
        }

        private static void TestPersistentColonyEventMemory() {
            var memory = new PersistentColonyEventMemory(2);
            Check(memory.Record("死亡", "复制人米玛死亡", 20, 1f),
                "colony event memory records a verified event");
            Check(!memory.Record("死亡", "复制人米玛死亡", 20, 1f),
                "colony event memory rejects same-cycle duplicates");
            memory.Record("科技", "完成科技研究：基础农业", 21, 0.9f);
            memory.Record("科技", "完成科技研究：高级电力", 22, 0.8f);

            ColonyEventMemoryContext context = memory.GetContext(5);
            Check(context.RecentEvents.Count == 2 &&
                context.RecentEvents.Any(item => item.Contains("米玛死亡")),
                "colony event memory respects capacity and preserves critical events");

            var restored = new PersistentColonyEventMemory(2);
            restored.Import(memory.Export());
            Check(restored.GetContext(2).RecentEvents.Count == 2,
                "colony event memory survives export and import");
        }

        private static void TestActionMemoryPolicy() {
            var memory = new PersistentActionMemory(10, 2);
            memory.Record("米玛", "完成建造", "砖块", 10, 0.75f, true);
            Check(memory.GetContext("米玛", 5).RecentActions.Count == 0,
                "single routine construction is stored but not injected");

            memory.Record("米玛", "完成建造", "砖块", 10, 0.75f, true);
            memory.Record("米玛", "完成建造", "砖块", 11, 0.75f, true);
            memory.Record("米玛", "完成建造", "砖块", 12, 0.75f, true);
            ActionMemorySnapshot routine = memory.Export().Single();
            Check(routine.IsRoutine && routine.Count == 4 &&
                routine.Importance >= 0.29f && routine.Importance <= 0.31f,
                "routine construction is promoted after four completions at low weight");
            Check(memory.GetContext("米玛", 5).RecentActions.Single()
                    .Contains("完成连续铺设"),
                "promoted routine work is summarized as a construction batch");

            for (int index = 0; index < 20; index++)
                memory.Record("米玛", "完成建造", "砖块", 12, 0.75f, true);
            Check(memory.Export().Single().Importance <= 0.45f,
                "routine repetition never becomes a high-importance memory");

            memory.Record("米玛", "完成建造", "电解器", 12, 0.75f, false);
            Check(memory.GetContext("米玛", 5).RecentActions.Any(item =>
                    item.Contains("电解器")),
                "functional construction remains immediately eligible");

            var restored = new PersistentActionMemory(10, 2);
            restored.Import(new[] {
                new ActionMemorySnapshot {
                    Actor = "哈桑",
                    Category = "完成建造",
                    Target = "隔热砖",
                    Count = 1,
                    Importance = 0.75f,
                    LastUpdatedSequence = 1
                }
            });
            ActionMemorySnapshot migrated = restored.Export().Single();
            Check(migrated.IsRoutine && migrated.Importance < 0.3f &&
                    restored.GetContext("哈桑", 5).RecentActions.Count == 0,
                "legacy tile memories are reclassified and down-weighted on import");
        }

        private static void TestActionCategoryClassifier() {
            Check(ActionCategoryClassifier.TryClassify(
                    new[] { "ComplexFabricatorLayeredWorkable" }, out string fabrication,
                    out float fabricationImportance) && fabrication == "完成制作" &&
                    fabricationImportance > 0.5f,
                "action classifier recognizes meaningful completed work");
            Check(ActionCategoryClassifier.TryClassify(
                    new[] { "DoctorStationDoctorWorkable" }, out string treatment,
                    out _) && treatment == "完成治疗",
                "action classifier recognizes high-importance treatment");
            Check(!ActionCategoryClassifier.TryClassify(
                    new[] { "ToiletWorkableUse" }, out _, out _),
                "action classifier ignores routine noise");
        }

        private static void TestSmartMemoryInjection() {
            var candidates = new[] {
                new MemoryInjectionCandidate {
                    Id = "old-wire",
                    Kind = MemoryCandidateKind.SpeakerAction,
                    Text = "周期 3：完成建造（电线）",
                    MatchText = "米玛 建造 电线 供电",
                    Cycle = 3,
                    Importance = 0.8f,
                    Sequence = 1
                },
                new MemoryInjectionCandidate {
                    Id = "new-sand",
                    Kind = MemoryCandidateKind.SpeakerAction,
                    Text = "周期 9：完成挖掘（砂岩）",
                    MatchText = "米玛 挖掘 砂岩",
                    Cycle = 9,
                    Importance = 0.6f,
                    Sequence = 2
                },
                new MemoryInjectionCandidate {
                    Id = "relationship",
                    Kind = MemoryCandidateKind.Relationship,
                    Text = "哈桑：上次那条电线终于通电了。",
                    MatchText = "米玛 哈桑 电线 供电",
                    Cycle = 7,
                    Importance = 0.65f,
                    Sequence = 3
                },
                new MemoryInjectionCandidate {
                    Id = "listener",
                    Kind = MemoryCandidateKind.ListenerAction,
                    Text = "周期 8：完成修理（电池）",
                    MatchText = "哈桑 修理 电池 供电",
                    Cycle = 8,
                    Importance = 0.8f,
                    Sequence = 4
                },
                new MemoryInjectionCandidate {
                    Id = "event",
                    Kind = MemoryCandidateKind.ColonyEvent,
                    Text = "周期 6：殖民地完成科技研究：高级电力",
                    MatchText = "科技 电力 供电",
                    Cycle = 6,
                    Importance = 0.9f,
                    Sequence = 5
                }
            };
            MemoryInjectionSelection selection = SmartMemoryInjectionEngine.Select(
                "米玛 → 哈桑 · 修理供电", "米玛 哈桑 电线 供电", 10,
                candidates, MemoryInjectionPreset.平衡, 520);

            SelectedMemory wire = selection.Items.Single(item =>
                item.Candidate.Id == "old-wire");
            SelectedMemory sand = selection.Items.Single(item =>
                item.Candidate.Id == "new-sand");
            Check(wire.Score > sand.Score,
                "smart memory favors relevant history over merely recent history");
            Check(Enum.GetValues(typeof(MemoryCandidateKind)).Cast<MemoryCandidateKind>()
                    .All(kind => selection.Items.Any(item =>
                        item.Candidate.Kind == kind)),
                "smart memory reserves room for every available memory kind");
            Check(selection.EstimatedTokens <= selection.TokenBudget,
                "smart memory selection stays within its token budget");
            Check(MemoryTokenEstimator.Estimate("氧气 oxygen") > 2,
                "token estimator handles mixed Chinese and ASCII text");
            Check(selection.ToPreviewText().Contains("评分") &&
                selection.ToPreviewText().Contains("预算"),
                "smart memory exposes a readable scoring preview");

            ConversationContext context = CreateContext();
            string prompt = PromptBuilder.BuildUserPrompt(context, null, null,
                null, null, null, selection);
            Check(prompt.Contains("由本地评分选择") && prompt.Contains("高级电力"),
                "prompt builder injects the selected smart memories");
        }

        private static void TestProviderProfiles() {
            Check(ProviderProfileCatalog.All.Select(item => item.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                    ProviderProfileCatalog.All.Count,
                "provider profile identifiers are unique");
            Check(ProviderProfileCatalog.Get("openai-compatible").Id == "custom",
                "legacy generic provider migrates to custom compatibility mode");

            var deepSeek = new ProviderConfiguration {
                Provider = "deepseek",
                Endpoint = ProviderProfileCatalog.Get("deepseek").Endpoint,
                Model = ProviderProfileCatalog.Get("deepseek").DefaultModel,
                ApiKey = "secret"
            };
            ProviderConfiguration gemini = ProviderProfileCatalog.SwitchProfile(
                deepSeek, "gemini");
            Check(gemini.Endpoint.Contains("googleapis.com") &&
                    gemini.Model.StartsWith("gemini", StringComparison.Ordinal) &&
                    gemini.ApiKey == "secret",
                "switching provider replaces known defaults but preserves the key");

            ProviderConfiguration forcedPreset = ProviderProfileCatalog.SwitchProfile(
                new ProviderConfiguration {
                    Provider = "custom",
                    Endpoint = "http://old-provider/v1/chat/completions",
                    Model = "old-model"
                }, "openai");
            Check(forcedPreset.Endpoint.Contains("api.openai.com") &&
                    forcedPreset.Model == ProviderProfileCatalog.Get("openai").DefaultModel,
                "selecting a preset never carries another provider endpoint forward");

            var custom = new ProviderConfiguration {
                Provider = "custom",
                Endpoint = "http://localhost:1234/v1/chat/completions",
                Model = "my-model"
            };
            ProviderConfiguration normalized = ProviderProfileCatalog.Normalize(custom);
            Check(normalized.Endpoint == custom.Endpoint &&
                    normalized.Model == custom.Model,
                "custom provider preserves user endpoint and model");
            Check(!ProviderProfileCatalog.Get("ollama").ApiKeyRequired &&
                    ProviderProfileCatalog.Get("claude").MaximumTemperature == 1f,
                "provider profiles expose authentication and parameter differences");
        }

        private static void TestRemoteFailureBackoff() {
            var backoff = new RemoteFailureBackoff(30f, 300f);
            Check(backoff.CanAttempt && backoff.RegisterFailure() == 30f &&
                    !backoff.CanAttempt,
                "remote failure starts offline backoff");
            backoff.Tick(30f);
            Check(backoff.CanAttempt && backoff.RegisterFailure() == 60f,
                "repeated remote failures increase backoff");
            for (int index = 0; index < 8; index++) {
                backoff.Tick(300f);
                backoff.RegisterFailure();
            }
            Check(backoff.RemainingSeconds == 300f,
                "remote failure backoff is capped");
            backoff.RegisterSuccess();
            Check(backoff.CanAttempt && backoff.ConsecutiveFailures == 0,
                "remote success resets backoff");
        }

        private static void TestMemoryLibraryFormatter() {
            string pairKey = ConversationGate.CreatePairKey("米玛", "哈桑");
            var relationships = new PersistentConversationMemory();
            relationships.Record(pairKey, "米玛", "哈桑", "电线终于通了。", 12);
            var actions = new PersistentActionMemory();
            actions.Record("米玛", "完成建造", "电解器", 12, 0.75f);
            actions.Record("哈桑", "完成建造", "砖块", 12, 0.75f, true);
            var events = new PersistentColonyEventMemory();
            events.Record("科技", "完成科技研究：高级电力", 12, 0.9f);

            string text = MemoryLibraryFormatter.Format("测试殖民地", "colony-1",
                relationships.Export(), actions.Export(), events.Export());
            Check(text.Contains("测试殖民地") && text.Contains("米玛 ↔ 哈桑") &&
                    text.Contains("电线终于通了"),
                "memory library shows colony and relationship summaries");
            Check(text.Contains("电解器") && text.Contains("重要性 0.75") &&
                    text.Contains("高级电力"),
                "memory library shows action and event summaries");
            Check(text.Contains("行动：2 条") && text.Contains("事件：1 条"),
                "memory library reports complete category counts");

            string limited = MemoryLibraryFormatter.Format("测试", "id",
                relationships.Export(), actions.Export(), events.Export(), 1);
            Check(limited.Contains("另有 1 条未在摘要中显示"),
                "memory library limits display without hiding full counts");

            try {
                CoreText.Resolver = key => key == "MEMORY_LIBRARY.COLONY"
                    ? "Colony: {0}"
                    : key == "MEMORY_LIBRARY.COUNTS"
                        ? "Relationships: {0} · Lines: {1} · Actions: {2} · Events: {3}"
                        : null;
                string english = MemoryLibraryFormatter.Format("Test Colony", "id",
                    relationships.Export(), actions.Export(), events.Export());
                Check(english.Contains("Colony: Test Colony") &&
                        english.Contains("Actions: 2") && english.Contains("Events: 1"),
                    "core summaries accept localized structural text");
            } finally {
                CoreText.Resolver = null;
            }
        }

        private static ConversationContext CreateContext() {
            return new ConversationContext(
                new DupeSnapshot {
                    Name = "米玛",
                    Traits = new List<string> { "鼾声如雷", "早起鸟" },
                    StressPercent = 85f,
                    BreathPercent = 62f,
                    CurrentTask = "修理厕所",
                    Element = "污氧",
                    TemperatureC = 31.2f,
                    CellMassKg = 0.7f
                },
                new DupeSnapshot {
                    Name = "哈桑",
                    Traits = new List<string> { "无法研究" },
                    StressPercent = 12f,
                    BreathPercent = 98f,
                    CurrentTask = "搬运沙石",
                    Element = "氧气",
                    TemperatureC = 24f,
                    CellMassKg = 1.8f
                },
                "复制人开始闲聊");
        }

        private static void Check(bool condition, string name) {
            if (condition) {
                Console.WriteLine("PASS  " + name);
                return;
            }

            failures++;
            Console.Error.WriteLine("FAIL  " + name);
        }
    }
}
