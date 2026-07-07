using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ONITalk.Core;

namespace ONITalk.Infrastructure {
    internal sealed class ColonyIdentity {
        internal ColonyIdentity(string id, string name) {
            Id = id;
            Name = name;
        }

        internal string Id { get; }

        internal string Name { get; }

        internal static ColonyIdentity? TryCapture() {
            try {
                SaveLoader? loader = SaveLoader.Instance;
                if (loader == null)
                    return null;

                string id = loader.GameInfo.colonyGuid.ToString();
                string name = SaveGame.Instance == null ? string.Empty :
                    SaveGame.Instance.BaseName;
                if (string.IsNullOrWhiteSpace(id) ||
                        string.Equals(id, Guid.Empty.ToString(),
                            StringComparison.OrdinalIgnoreCase)) {
                    string activeSave = SaveLoader.GetActiveSaveFilePath() ?? string.Empty;
                    string seed = string.IsNullOrWhiteSpace(activeSave) ? name : activeSave;
                    if (string.IsNullOrWhiteSpace(seed))
                        return null;
                    id = "legacy-" + StableHash(seed);
                }

                return new ColonyIdentity(id.Trim(), string.IsNullOrWhiteSpace(name)
                    ? "未命名殖民地"
                    : name.Trim());
            } catch (Exception error) {
                Log.Warning("Colony identity is not ready. " + error.Message);
                return null;
            }
        }

        private static string StableHash(string value) {
            unchecked {
                uint hash = 2166136261;
                foreach (char character in value) {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash.ToString("x8");
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class ColonyMemoryDocument {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 3;

        [JsonProperty("colonyId")]
        public string ColonyId { get; set; } = string.Empty;

        [JsonProperty("colonyName")]
        public string ColonyName { get; set; } = string.Empty;

        [JsonProperty("updatedUtc")]
        public DateTimeOffset UpdatedUtc { get; set; }

        [JsonProperty("pairs")]
        public List<PairMemorySnapshot> Pairs { get; set; } =
            new List<PairMemorySnapshot>();

        [JsonProperty("actions")]
        public List<ActionMemorySnapshot> Actions { get; set; } =
            new List<ActionMemorySnapshot>();

        [JsonProperty("events")]
        public List<ColonyEventMemorySnapshot> Events { get; set; } =
            new List<ColonyEventMemorySnapshot>();
    }

    internal sealed class ColonyMemoryLoadResult {
        internal ColonyMemoryLoadResult(IReadOnlyList<PairMemorySnapshot> pairs,
                IReadOnlyList<ActionMemorySnapshot> actions,
                IReadOnlyList<ColonyEventMemorySnapshot> events) {
            Pairs = pairs;
            Actions = actions;
            Events = events;
        }

        internal IReadOnlyList<PairMemorySnapshot> Pairs { get; }

        internal IReadOnlyList<ActionMemorySnapshot> Actions { get; }

        internal IReadOnlyList<ColonyEventMemorySnapshot> Events { get; }
    }

    internal sealed class ColonyMemoryRepository {
        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

        private readonly ColonyIdentity colony;
        private readonly string path;

        internal ColonyMemoryRepository(ColonyIdentity colony) {
            this.colony = colony ?? throw new ArgumentNullException(nameof(colony));
            string configDirectory = Path.GetDirectoryName(ONITalkConfig.ConfigPath) ??
                KMod.Manager.GetDirectory();
            path = Path.Combine(configDirectory, "memory", SafeFileName(colony.Id) +
                ".json");
        }

        internal string ColonyId => colony.Id;

        internal string ColonyName => colony.Name;

        internal ColonyMemoryLoadResult Load() {
            ColonyMemoryDocument? document = TryRead(path);
            if (document == null)
                document = TryRead(path + ".bak");
            if (document == null)
                return new ColonyMemoryLoadResult(Array.Empty<PairMemorySnapshot>(),
                    Array.Empty<ActionMemorySnapshot>(),
                    Array.Empty<ColonyEventMemorySnapshot>());
            if (document.SchemaVersion != 1 && document.SchemaVersion != 2 &&
                    document.SchemaVersion != 3) {
                Log.Warning("Memory file schema is unsupported for colony " + colony.Id +
                    ".");
                return new ColonyMemoryLoadResult(Array.Empty<PairMemorySnapshot>(),
                    Array.Empty<ActionMemorySnapshot>(),
                    Array.Empty<ColonyEventMemorySnapshot>());
            }
            if (!string.IsNullOrWhiteSpace(document.ColonyId) &&
                    !string.Equals(document.ColonyId.Trim(), colony.Id,
                        StringComparison.OrdinalIgnoreCase)) {
                Log.Warning("Rejected memory file for another colony. Expected=" +
                    colony.Id + ", found=" + document.ColonyId + ".");
                return new ColonyMemoryLoadResult(Array.Empty<PairMemorySnapshot>(),
                    Array.Empty<ActionMemorySnapshot>(),
                    Array.Empty<ColonyEventMemorySnapshot>());
            }
            return new ColonyMemoryLoadResult(
                document.Pairs ?? new List<PairMemorySnapshot>(),
                document.Actions ?? new List<ActionMemorySnapshot>(),
                document.Events ?? new List<ColonyEventMemorySnapshot>());
        }

        internal bool Save(IReadOnlyList<PairMemorySnapshot> pairs,
                IReadOnlyList<ActionMemorySnapshot> actions,
                IReadOnlyList<ColonyEventMemorySnapshot> events) {
            string temporaryPath = path + ".tmp";
            string backupPath = path + ".bak";
            try {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var document = new ColonyMemoryDocument {
                    ColonyId = colony.Id,
                    ColonyName = colony.Name,
                    UpdatedUtc = DateTimeOffset.UtcNow,
                    Pairs = pairs == null
                        ? new List<PairMemorySnapshot>()
                        : new List<PairMemorySnapshot>(pairs),
                    Actions = actions == null
                        ? new List<ActionMemorySnapshot>()
                        : new List<ActionMemorySnapshot>(actions),
                    Events = events == null
                        ? new List<ColonyEventMemorySnapshot>()
                        : new List<ColonyEventMemorySnapshot>(events)
                };
                string json = JsonConvert.SerializeObject(document, Formatting.Indented,
                    JsonSettings);
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(path)) {
                    try {
                        File.Replace(temporaryPath, path, backupPath);
                    } catch (PlatformNotSupportedException) {
                        File.Copy(path, backupPath, true);
                        File.Copy(temporaryPath, path, true);
                        File.Delete(temporaryPath);
                    }
                } else {
                    File.Move(temporaryPath, path);
                }
                return true;
            } catch (Exception error) {
                Log.Warning("Could not save colony memory. " + error.Message);
                TryDeleteTemporary(temporaryPath);
                return false;
            }
        }

        internal string ExportSnapshot(IReadOnlyList<PairMemorySnapshot> pairs,
                IReadOnlyList<ActionMemorySnapshot> actions,
                IReadOnlyList<ColonyEventMemorySnapshot> events) {
            string configDirectory = Path.GetDirectoryName(ONITalkConfig.ConfigPath) ??
                KMod.Manager.GetDirectory();
            string directory = Path.Combine(configDirectory, "exports");
            Directory.CreateDirectory(directory);
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            string fileName = SafeFileName(colony.Name) + "-" + timestamp + ".json";
            string exportPath = Path.Combine(directory, fileName);
            var document = new ColonyMemoryDocument {
                ColonyId = colony.Id,
                ColonyName = colony.Name,
                UpdatedUtc = DateTimeOffset.UtcNow,
                Pairs = pairs == null
                    ? new List<PairMemorySnapshot>()
                    : new List<PairMemorySnapshot>(pairs),
                Actions = actions == null
                    ? new List<ActionMemorySnapshot>()
                    : new List<ActionMemorySnapshot>(actions),
                Events = events == null
                    ? new List<ColonyEventMemorySnapshot>()
                    : new List<ColonyEventMemorySnapshot>(events)
            };
            File.WriteAllText(exportPath, JsonConvert.SerializeObject(document,
                Formatting.Indented, JsonSettings));
            return exportPath;
        }

        private static ColonyMemoryDocument? TryRead(string sourcePath) {
            if (!File.Exists(sourcePath))
                return null;
            try {
                return JsonConvert.DeserializeObject<ColonyMemoryDocument>(
                    File.ReadAllText(sourcePath), JsonSettings);
            } catch (Exception error) {
                Log.Warning("Could not read colony memory file " +
                    Path.GetFileName(sourcePath) + ". " + error.Message);
                return null;
            }
        }

        private static string SafeFileName(string value) {
            string safe = value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(safe) ? "unknown-colony" : safe;
        }

        private static void TryDeleteTemporary(string temporaryPath) {
            try {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            } catch {
            }
        }
    }
}
