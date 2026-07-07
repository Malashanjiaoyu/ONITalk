namespace ONITalk {
    public static class STRINGS {
        public static class ONITALK {
            public static class CATEGORIES {
                public static LocString GENERAL = "General";
                public static LocString PROVIDER = "AI provider";
                public static LocString FREQUENCY = "Conversation frequency";
                public static LocString ADVANCED = "Advanced";
                public static LocString CHAT_WINDOW = "Chat window";
                public static LocString CHARACTER_MEMORY = "Character memory";
                public static LocString ACTION_MEMORY = "Action memory";
                public static LocString MEMORY_INJECTION = "Memory injection";
                public static LocString CONTINUOUS_DIALOGUE = "Continuous dialogue";
                public static LocString PROMPT = "Prompt customization";
            }

            public static class LANGUAGES {
                public static LocString FOLLOW_GAME = "Follow game language";
                public static LocString ENGLISH = "English";
                public static LocString SIMPLIFIED_CHINESE = "Simplified Chinese";
                public static LocString SPANISH = "Spanish";
            }

            public static class MEMORY_PRESETS {
                public static LocString LIGHT = "Light";
                public static LocString BALANCED = "Balanced";
                public static LocString RICH = "Rich";
                public static LocString CUSTOM = "Custom";
            }

            public static class UI {
                public static class PROVIDER {
                    public static LocString SELECT_TOOLTIP =
                        "Choose a provider preset. Endpoint and model remain editable.";
                    public static LocString ENDPOINT_LABEL = "Endpoint";
                    public static LocString ENDPOINT_TOOLTIP =
                        "Complete Chat Completions endpoint URL";
                    public static LocString MODEL_LABEL = "Model";
                    public static LocString MODEL_TOOLTIP =
                        "Model ID shown in the provider console";
                    public static LocString API_KEY_LABEL = "API Key";
                    public static LocString API_KEY_TOOLTIP =
                        "Stored locally as plain text and never written to logs";
                    public static LocString STATUS_NOT_VALIDATED =
                        "The current configuration has not been validated.";
                    public static LocString STATUS_VALIDATING = "Validating, please wait...";
                    public static LocString STATUS_UI_ERROR =
                        "Validation interface error: {0}";
                    public static LocString KEY_REQUIRED = " API Key required.";
                    public static LocString KEY_OPTIONAL = " API Key may be left empty.";
                    public static LocString VALIDATE_BUTTON = "Validate configuration";
                    public static LocString VALIDATE_TOOLTIP =
                        "Sends one minimal request without creating dialogue or changing memory.";

                    public static class PROFILES {
                        public static LocString DEEPSEEK_NAME = "DeepSeek";
                        public static LocString DEEPSEEK_DESCRIPTION =
                            "Strong Chinese output and good value.";
                        public static LocString OPENAI_NAME = "OpenAI";
                        public static LocString OPENAI_DESCRIPTION =
                            "Official OpenAI Chat Completions endpoint.";
                        public static LocString OPENROUTER_NAME = "OpenRouter";
                        public static LocString OPENROUTER_DESCRIPTION =
                            "Access models from multiple providers through one endpoint.";
                        public static LocString GEMINI_NAME = "Google Gemini";
                        public static LocString GEMINI_DESCRIPTION =
                            "Google Gemini's OpenAI-compatible endpoint.";
                        public static LocString SILICONFLOW_NAME = "SiliconFlow";
                        public static LocString SILICONFLOW_DESCRIPTION =
                            "Multi-model OpenAI-compatible platform available in China.";
                        public static LocString OLLAMA_NAME = "Ollama (local)";
                        public static LocString OLLAMA_DESCRIPTION =
                            "Runs models locally and does not require a real API Key.";
                        public static LocString BAILIAN_NAME =
                            "Alibaba Cloud Model Studio / Qwen";
                        public static LocString BAILIAN_DESCRIPTION =
                            "The endpoint can be changed for a regional or Workspace-specific address.";
                        public static LocString GROQ_NAME = "Groq";
                        public static LocString GROQ_DESCRIPTION =
                            "Low-latency OpenAI-compatible inference service.";
                        public static LocString CLAUDE_NAME =
                            "Anthropic Claude (compatibility layer)";
                        public static LocString CLAUDE_DESCRIPTION =
                            "Uses Anthropic's official OpenAI compatibility layer. Native advanced features may be added later.";
                        public static LocString CUSTOM_NAME =
                            "Custom compatible endpoint";
                        public static LocString CUSTOM_DESCRIPTION =
                            "For compatible services such as Kimi, GLM, MiniMax, and LM Studio.";
                        public static LocString ECHO_NAME = "Offline dialogue";
                        public static LocString ECHO_DESCRIPTION =
                            "Uses built-in offline lines without calling a remote endpoint.";
                    }
                }

                public static class CHAT {
                    public static LocString TITLE = "ONITalk conversations";
                    public static LocString LIBRARY_BUTTON = "L";
                    public static LocString LIBRARY_TOOLTIP =
                        "View, export, or clear long-term memory";
                    public static LocString MEMORY_BUTTON = "M";
                    public static LocString MEMORY_TOOLTIP =
                        "View memory injection and scoring from the previous request";
                    public static LocString CLEAR_BUTTON = "C";
                    public static LocString CLEAR_TOOLTIP =
                        "Clear chat history for this game session";
                    public static LocString MINIMIZE_TOOLTIP =
                        "Minimize or restore the chat window";
                }

                public static class MEMORY_PREVIEW {
                    public static LocString TITLE = "ONITalk memory injection preview";
                    public static LocString CLOSE_TOOLTIP = "Close preview";
                }

                public static class MEMORY_LIBRARY {
                    public static LocString TITLE = "ONITalk long-term memory";
                    public static LocString CLOSE_TOOLTIP = "Close memory manager";
                    public static LocString REFRESH_BUTTON = "Refresh";
                    public static LocString REFRESH_TOOLTIP =
                        "Reload the summary currently held in memory";
                    public static LocString EXPORT_BUTTON = "Export JSON";
                    public static LocString EXPORT_TOOLTIP =
                        "Export complete memory without the API Key";
                    public static LocString CLEAR_BUTTON = "Clear long-term memory";
                    public static LocString CLEAR_TOOLTIP =
                        "Requires two confirmations and does not clear chat history";
                    public static LocString NO_INFORMATION =
                        "No memory information is available.";
                    public static LocString READ_FAILED = "Failed to read memory: {0}";
                    public static LocString EXPORT_UNAVAILABLE =
                        "The export function is not connected.";
                    public static LocString EXPORT_FAILED = "Export failed: {0}";
                    public static LocString CLEAR_CONFIRM =
                        "Clear all long-term relationship, action, and event memory for the current colony?\n\nChat history will not be deleted. Exporting a backup first is recommended.";
                    public static LocString CLEAR_FINAL_CONFIRM =
                        "Final confirmation: this immediately overwrites the memory file on disk and cannot be undone.";
                    public static LocString CONFIRM_CLEAR_BUTTON = "Confirm clear";
                    public static LocString CONTINUE_BUTTON = "Continue";
                    public static LocString CANCEL_BUTTON = "Cancel";
                    public static LocString CLEAR_UNAVAILABLE =
                        "The clear function is not connected.";
                    public static LocString CLEAR_FAILED = "Clear failed: {0}";
                }

                public static class VALIDATION {
                    public static LocString OFFLINE_VALID =
                        "Offline dialogue does not require an API connection. Configuration is valid.";
                    public static LocString ENDPOINT_REQUIRED = "Enter an API endpoint.";
                    public static LocString ENDPOINT_INVALID =
                        "Endpoint must be a valid HTTP or HTTPS URL.";
                    public static LocString MODEL_REQUIRED = "Enter a model name.";
                    public static LocString API_KEY_REQUIRED = "{0} requires an API Key.";
                    public static LocString SUCCESS =
                        "Connection successful · Model {0} · {1} ms";
                    public static LocString CANCELLED =
                        "Validation was cancelled or timed out.";
                    public static LocString INVALID_JSON =
                        "The response is not valid JSON.";
                    public static LocString EMPTY_RESPONSE =
                        "The response contains no dialogue text.";
                    public static LocString CONNECTION_FAILED = "Connection failed: {0}";
                    public static LocString PROVIDER_REJECTED =
                        "The provider rejected the request";
                }

                public static class RUNTIME {
                    public static LocString NO_INJECTION_PREVIEW =
                        "No memory injection has been recorded. Wait for the next ONITalk line and try again.";
                    public static LocString MEMORY_NOT_CONNECTED =
                        "No colony memory is connected. Enter a saved game and try again.";
                    public static LocString EXPORT_NOT_CONNECTED =
                        "Export failed: no colony memory is connected.";
                    public static LocString EXPORT_SUCCESS =
                        "Export successful:\n{0}\n\nThe file contains colony memory only and never includes the API Key.";
                    public static LocString EXPORT_FAILED = "Export failed: {0}";
                    public static LocString CLEAR_NOT_CONNECTED =
                        "Clear failed: no colony memory is connected.";
                    public static LocString CLEAR_SUCCESS =
                        "Long-term memory was cleared and saved. Chat history was not affected.";
                    public static LocString CLEAR_SAVE_FAILED =
                        "Long-term memory was cleared in memory, but saving to disk failed. Check Player.log.";
                    public static LocString SERVICE_NOT_INITIALIZED =
                        "The ONITalk service has not been initialized.";
                    public static LocString DIAGNOSTIC_BUBBLE =
                        "ONITalk test: the display pipeline is working.";
                }

                public static class PROMPT {
                    public static LocString ENABLE = "Use custom style prompt";
                    public static LocString ENABLE_TOOLTIP = "Adds your style instructions after ONITalk's protected core prompt.";
                    public static LocString VARIABLES = "Variables: {language}, {maxCharacters}, {speaker}, {listener}, {trigger}";
                    public static LocString EDITOR_TOOLTIP = "Custom style instructions. Core safety and factual rules cannot be overridden.";
                    public static LocString STATUS_OK = "Template variables are valid.";
                    public static LocString STATUS_UNKNOWN = "Unknown variables: {0}";
                    public static LocString RESET = "Restore starter template";
                    public static LocString COPY_DEFAULT = "Copy default prompt";
                    public static LocString COPIED = "Default prompt copied to clipboard.";
                    public static LocString PREVIEW = "Preview final prompt";
                }
            }

            public static class CORE {
                public static class MEMORY_LIBRARY {
                    public static LocString COLONY = "Colony: {0}";
                    public static LocString ID = "ID: {0}";
                    public static LocString COUNTS =
                        "Relationships: {0} pairs · Recorded lines: {1} · Actions: {2} · Events: {3}";
                    public static LocString SUMMARY_NOTE =
                        "This is the latest summary. Export JSON contains the complete memory.";
                    public static LocString RELATIONSHIPS_HEADING =
                        "[Relationship conversations]";
                    public static LocString ACTIONS_HEADING = "[Action memory]";
                    public static LocString EVENTS_HEADING = "[Colony events]";
                    public static LocString NO_RECORDS = "No records";
                    public static LocString LINE_COUNT = "{0} lines";
                    public static LocString CYCLE = "Cycle {0}";
                    public static LocString LATEST = "Latest: {0}: {1}";
                    public static LocString IMPORTANCE = "Importance {0}";
                    public static LocString ROUTINE_CONSTRUCTION =
                        "Routine construction";
                    public static LocString OMITTED =
                        "... {0} more entries are not shown in this summary";
                    public static LocString UNKNOWN = "Unknown";
                    public static LocString UNKNOWN_RELATION = "Unknown relationship";
                    public static LocString UNNAMED_COLONY = "Unnamed colony";
                }

                public static class MEMORY_PREVIEW {
                    public static LocString CONTEXT = "Context: {0}";
                    public static LocString BUDGET =
                        "Budget: about {0} / {1} tokens";
                    public static LocString COUNTS = "Candidates: {0}, selected: {1}";
                    public static LocString NONE =
                        "No memory met the current budget and quota requirements.";
                    public static LocString ITEM_HEADER =
                        "[{0}] Score {1} · about {2} tokens";
                    public static LocString BREAKDOWN =
                        "Relevance {0}  Recency {1}  Importance {2}  Participants {3}  Type {4}";
                    public static LocString KIND_RELATIONSHIP =
                        "Relationship conversation";
                    public static LocString KIND_SPEAKER_ACTION = "Speaker action";
                    public static LocString KIND_LISTENER_ACTION = "Listener action";
                    public static LocString KIND_COLONY_EVENT = "Colony event";
                    public static LocString KIND_MEMORY = "Memory";
                }
            }

            public static class OPTIONS {
                public static class PROMPT_CONFIGURATION {
                    public static LocString NAME = "Prompt configuration";
                    public static LocString TOOLTIP = "Choose the default prompt or add custom style instructions and preview the result.";
                }
                public static class INTERFACE_LANGUAGE {
                    public static LocString NAME = "Interface language";
                    public static LocString TOOLTIP =
                        "Choose ONITalk's interface language. Close and reopen this window after saving.";
                }

                public static class ENABLED {
                    public static LocString NAME = "Enable ONITalk";
                    public static LocString TOOLTIP =
                        "When disabled, ONITalk will not generate any new dialogue.";
                }

                public static class PROVIDER_CONFIGURATION {
                    public static LocString NAME = "Provider configuration";
                    public static LocString TOOLTIP =
                        "Choose a common provider or enter a custom OpenAI-compatible endpoint.";
                }

                public static class AMBIENT_CHATTER_INTERVAL_SECONDS {
                    public static LocString NAME = "Ambient chatter interval (seconds)";
                    public static LocString TOOLTIP =
                        "How often nearby Duplicants may chat when there is no urgent condition.";
                }

                public static class PAIR_COOLDOWN_SECONDS {
                    public static LocString NAME = "Duplicant pair cooldown (seconds)";
                    public static LocString TOOLTIP =
                        "Minimum wait before the same pair of Duplicants can talk again.";
                }

                public static class GLOBAL_COOLDOWN_SECONDS {
                    public static LocString NAME = "Global cooldown (seconds)";
                    public static LocString TOOLTIP =
                        "Minimum interval between any two ONITalk lines in the colony.";
                }

                public static class CONVERSATION_REPLIES_ENABLED {
                    public static LocString NAME = "Allow listener replies";
                    public static LocString TOOLTIP =
                        "Allows the listener to reply after an opening line. This can increase API usage.";
                }

                public static class REPLY_CHANCE_PERCENT {
                    public static LocString NAME = "Listener reply chance (%)";
                    public static LocString TOOLTIP =
                        "Chance that an opening line develops into a continued conversation.";
                }

                public static class MAX_CONVERSATION_LINES {
                    public static LocString NAME = "Maximum lines per conversation";
                    public static LocString TOOLTIP =
                        "Includes the opening line. A value of 2 means one opening line and one reply.";
                }

                public static class REPLY_DELAY_SECONDS {
                    public static LocString NAME = "Reply delay (seconds)";
                    public static LocString TOOLTIP =
                        "How long the listener waits before replying to the previous line.";
                }

                public static class TESTING_MODE {
                    public static LocString NAME = "High-frequency testing mode";
                    public static LocString TOOLTIP =
                        "For testing only. This greatly increases dialogue and API request frequency.";
                }

                public static class TESTING_CHATTER_INTERVAL_SECONDS {
                    public static LocString NAME = "Testing chatter interval (seconds)";
                    public static LocString TOOLTIP =
                        "Used only while high-frequency testing mode is enabled.";
                }

                public static class MAX_CONVERSATION_DISTANCE {
                    public static LocString NAME = "Maximum conversation distance";
                    public static LocString TOOLTIP =
                        "Only Duplicants within this distance can talk to each other.";
                }

                public static class REQUEST_TIMEOUT_SECONDS {
                    public static LocString NAME = "API timeout (seconds)";
                    public static LocString TOOLTIP =
                        "Cancels a request after this duration and uses offline dialogue instead.";
                }

                public static class TEMPERATURE {
                    public static LocString NAME = "Dialogue randomness";
                    public static LocString TOOLTIP =
                        "Higher values produce more varied dialogue. The valid range depends on the provider.";
                }

                public static class MAX_CHARACTERS {
                    public static LocString NAME = "Maximum characters per line";
                    public static LocString TOOLTIP =
                        "Generated speech-bubble text is limited to this length.";
                }

                public static class MAX_TOKENS {
                    public static LocString NAME = "Maximum API output tokens";
                    public static LocString TOOLTIP =
                        "Short dialogue usually does not need a large output limit.";
                }

                public static class CHAT_WINDOW_ENABLED {
                    public static LocString NAME = "Show chat history window";
                    public static LocString TOOLTIP =
                        "Continuously displays ONITalk conversation history in the game interface.";
                }

                public static class CHAT_HISTORY_LIMIT {
                    public static LocString NAME = "Chat history limit";
                    public static LocString TOOLTIP =
                        "Keeps only the newest messages to limit memory usage during long games.";
                }

                public static class CHAT_FONT_SIZE {
                    public static LocString NAME = "Chat font size";
                    public static LocString TOOLTIP =
                        "Adjusts dialogue text size inside the history window.";
                }

                public static class CHAT_WINDOW_OPACITY {
                    public static LocString NAME = "Window background opacity";
                    public static LocString TOOLTIP =
                        "Lower values obstruct less of the game view. Text and buttons remain visible.";
                }

                public static class CHAT_WINDOW_AUTO_FADE {
                    public static LocString NAME = "Darken background on hover";
                    public static LocString TOOLTIP =
                        "Increases background opacity while the pointer is over the window.";
                }

                public static class PERSISTENT_MEMORY_ENABLED {
                    public static LocString NAME = "Enable long-term colony memory";
                    public static LocString TOOLTIP =
                        "Stores recent conversations and familiarity per colony. Different saves never share memory.";
                }

                public static class MEMORY_LINES_PER_PAIR {
                    public static LocString NAME = "Memory lines per Duplicant pair";
                    public static LocString TOOLTIP =
                        "Number of recent shared lines available to dialogue generation.";
                }

                public static class MEMORY_MAX_PAIRS {
                    public static LocString NAME = "Maximum stored relationships";
                    public static LocString TOOLTIP =
                        "Limits the colony memory file size by removing the least recently updated relationships.";
                }

                public static class ACTION_MEMORY_ENABLED {
                    public static LocString NAME = "Enable action memory";
                    public static LocString TOOLTIP =
                        "Records important completed actions such as building, repair, digging, and fabrication.";
                }

                public static class ACTION_MEMORY_CAPACITY_PER_DUPE {
                    public static LocString NAME = "Action memories per Duplicant";
                    public static LocString TOOLTIP =
                        "Limits the number of local action records stored for each Duplicant.";
                }

                public static class ACTION_AGGREGATION_WINDOW_CYCLES {
                    public static LocString NAME = "Repeated-action aggregation window";
                    public static LocString TOOLTIP =
                        "Repeated matching work within this many cycles is merged into one counted memory.";
                }

                public static class MAJOR_EVENT_MEMORY_ENABLED {
                    public static LocString NAME = "Enable major colony events";
                    public static LocString TOOLTIP =
                        "Records confirmed colony events such as deaths and completed research.";
                }

                public static class MEMORY_PRESET {
                    public static LocString NAME = "Memory injection preset";
                    public static LocString TOOLTIP =
                        "Light uses about 250, Balanced 520, and Rich 850 tokens. Custom uses the budget below.";
                }

                public static class MEMORY_TOKEN_BUDGET {
                    public static LocString NAME = "Custom token budget";
                    public static LocString TOOLTIP =
                        "Used only with the Custom preset. Limits memory text injected into each request.";
                }
            }
        }
    }
}
