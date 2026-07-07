using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ONITalk.Core;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

namespace ONITalk.Infrastructure {
    public sealed class ProviderConfigurationOptionsEntry : OptionsEntry {
        private readonly List<ProfileOption> choices;
        private ProviderConfiguration working = ProviderProfileCatalog.Normalize(null);
        private GameObject? endpointField;
        private GameObject? keyField;
        private GameObject? modelField;
        private GameObject? providerCombo;
        private LocText? descriptionText;
        private LocText? statusText;
        private bool validating;

        public ProviderConfigurationOptionsEntry(string field, IOptionSpec spec)
                : base(field, spec) {
            choices = ProviderProfileCatalog.All.Select(profile =>
                new ProfileOption(profile)).ToList();
        }

        public override object Value {
            get => working.Clone();
            set {
                if (value is ProviderConfiguration configuration)
                    working = ProviderProfileCatalog.Normalize(configuration);
                UpdateUI();
            }
        }

        public override GameObject GetUIComponent() {
            ProfileOption selected = FindChoice(working.Provider);
            var panel = new PPanel("ProviderConfiguration") {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 6,
                FlexSize = Vector2.right
            };
            panel.AddChild(new PComboBox<ProfileOption>("Provider") {
                Content = choices,
                InitialItem = selected,
                OnOptionSelected = OnProviderSelected,
                BackColor = PUITuning.Colors.ButtonPinkStyle,
                EntryColor = PUITuning.Colors.ButtonBlueStyle,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                MinWidth = 390,
                MaxRowsShown = 8,
                ToolTip = T(STRINGS.ONITALK.UI.PROVIDER.SELECT_TOOLTIP)
            }.AddOnRealize(gameObject => providerCombo = gameObject));
            panel.AddChild(new PLabel("Description") {
                Text = BuildProfileDescription(selected.Profile),
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                DynamicSize = true,
                FlexSize = Vector2.right,
                Margin = new RectOffset(2, 2, 1, 3)
            }.AddOnRealize(gameObject => descriptionText =
                gameObject.GetComponentInChildren<LocText>()));
            panel.AddChild(CreateTextRow(T(STRINGS.ONITALK.UI.PROVIDER.ENDPOINT_LABEL),
                T(STRINGS.ONITALK.UI.PROVIDER.ENDPOINT_TOOLTIP),
                working.Endpoint, PTextField.FieldType.Text, 1024,
                (gameObject, value) => {
                    endpointField = gameObject;
                    working.Endpoint = value;
                    ResetStatus();
                }, gameObject => endpointField = gameObject));
            panel.AddChild(CreateTextRow(T(STRINGS.ONITALK.UI.PROVIDER.MODEL_LABEL),
                T(STRINGS.ONITALK.UI.PROVIDER.MODEL_TOOLTIP),
                working.Model, PTextField.FieldType.Text, 256,
                (gameObject, value) => {
                    modelField = gameObject;
                    working.Model = value;
                    ResetStatus();
                }, gameObject => modelField = gameObject));
            panel.AddChild(CreateTextRow(T(STRINGS.ONITALK.UI.PROVIDER.API_KEY_LABEL),
                T(STRINGS.ONITALK.UI.PROVIDER.API_KEY_TOOLTIP),
                working.ApiKey, PTextField.FieldType.Password, 512,
                (gameObject, value) => {
                    keyField = gameObject;
                    working.ApiKey = value;
                    ResetStatus();
                }, gameObject => keyField = gameObject));
            panel.AddChild(new PLabel("ValidationStatus") {
                Text = T(STRINGS.ONITALK.UI.PROVIDER.STATUS_NOT_VALIDATED),
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                DynamicSize = true,
                FlexSize = Vector2.right,
                Margin = new RectOffset(2, 2, 3, 2)
            }.AddOnRealize(gameObject => statusText =
                gameObject.GetComponentInChildren<LocText>()));
            panel.AddChild(new PButton("ValidateProvider") {
                Text = T(STRINGS.ONITALK.UI.PROVIDER.VALIDATE_BUTTON),
                ToolTip = T(STRINGS.ONITALK.UI.PROVIDER.VALIDATE_TOOLTIP),
                OnClick = _ => ValidateCurrent(),
                Margin = new RectOffset(12, 12, 4, 4),
                DynamicSize = true
            }.SetKleiPinkStyle());
            return panel.Build();
        }

        private PPanel CreateTextRow(string label, string tooltip, string initial,
                PTextField.FieldType type, int maximumLength,
                PUIDelegates.OnTextChanged changed,
                PUIDelegates.OnRealize realized) {
            return new PPanel("Row_" + label) {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 2,
                FlexSize = Vector2.right
            }.AddChild(new PLabel("Label_" + label) {
                Text = label,
                ToolTip = tooltip,
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                DynamicSize = true,
                FlexSize = Vector2.right
            }).AddChild(new PTextField("Field_" + label) {
                Text = initial ?? string.Empty,
                ToolTip = tooltip,
                Type = type,
                MaxLength = maximumLength,
                MinWidth = 390,
                TextAlignment = TextAlignmentOptions.Left,
                OnTextChanged = changed
            }.SetKleiBlueStyle().AddOnRealize(realized));
        }

        private void OnProviderSelected(GameObject _, ProfileOption selected) {
            if (selected == null)
                return;
            working = ProviderProfileCatalog.SwitchProfile(working,
                selected.Profile.Id);
            UpdateUI();
            ResetStatus();
        }

        private async void ValidateCurrent() {
            if (validating)
                return;
            validating = true;
            SetStatus(T(STRINGS.ONITALK.UI.PROVIDER.STATUS_VALIDATING));
            try {
                ProviderValidationResult result = await ProviderConnectionValidator.
                    ValidateAsync(working.Clone(), CancellationToken.None);
                SetStatus((result.Success ? "✓ " : "✕ ") + result.Message);
            } catch (Exception error) {
                SetStatus("✕ " + ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.PROVIDER.STATUS_UI_ERROR,
                    error.GetType().Name));
            } finally {
                validating = false;
            }
        }

        private void UpdateUI() {
            ProviderProfile profile = ProviderProfileCatalog.Get(working.Provider);
            if (providerCombo != null)
                PComboBox<ProfileOption>.SetSelectedItem(providerCombo,
                    FindChoice(profile.Id));
            SetInput(endpointField, working.Endpoint);
            SetInput(modelField, working.Model);
            SetInput(keyField, working.ApiKey);
            if (descriptionText != null)
                descriptionText.text = BuildProfileDescription(profile);
        }

        private void ResetStatus() {
            if (!validating)
                SetStatus(T(STRINGS.ONITALK.UI.PROVIDER.STATUS_NOT_VALIDATED));
        }

        private void SetStatus(string value) {
            if (statusText != null)
                statusText.text = value;
        }

        private static void SetInput(GameObject? gameObject, string value) {
            TMP_InputField? input = gameObject?.GetComponent<TMP_InputField>();
            if (input != null && input.text != value)
                input.text = value ?? string.Empty;
        }

        private ProfileOption FindChoice(string provider) {
            string id = ProviderProfileCatalog.NormalizeId(provider);
            return choices.First(choice => string.Equals(choice.Profile.Id, id,
                StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildProfileDescription(ProviderProfile profile) {
            return GetProfileDescription(profile) + (profile.ApiKeyRequired
                ? T(STRINGS.ONITALK.UI.PROVIDER.KEY_REQUIRED)
                : T(STRINGS.ONITALK.UI.PROVIDER.KEY_OPTIONAL));
        }

        private static string T(LocString value) => ONITalkLocalization.Get(value);

        internal static string GetProfileName(ProviderProfile profile) {
            switch (profile.Id) {
                case "deepseek": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.DEEPSEEK_NAME);
                case "openai": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OPENAI_NAME);
                case "openrouter": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OPENROUTER_NAME);
                case "gemini": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.GEMINI_NAME);
                case "siliconflow": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.SILICONFLOW_NAME);
                case "ollama": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OLLAMA_NAME);
                case "bailian": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.BAILIAN_NAME);
                case "groq": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.GROQ_NAME);
                case "claude": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.CLAUDE_NAME);
                case "custom": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.CUSTOM_NAME);
                case "echo": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.ECHO_NAME);
                default: return profile.DisplayName;
            }
        }

        private static string GetProfileDescription(ProviderProfile profile) {
            switch (profile.Id) {
                case "deepseek": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.DEEPSEEK_DESCRIPTION);
                case "openai": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OPENAI_DESCRIPTION);
                case "openrouter": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OPENROUTER_DESCRIPTION);
                case "gemini": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.GEMINI_DESCRIPTION);
                case "siliconflow": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.SILICONFLOW_DESCRIPTION);
                case "ollama": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.OLLAMA_DESCRIPTION);
                case "bailian": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.BAILIAN_DESCRIPTION);
                case "groq": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.GROQ_DESCRIPTION);
                case "claude": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.CLAUDE_DESCRIPTION);
                case "custom": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.CUSTOM_DESCRIPTION);
                case "echo": return T(STRINGS.ONITALK.UI.PROVIDER.PROFILES.ECHO_DESCRIPTION);
                default: return profile.Description;
            }
        }

        private sealed class ProfileOption : ITooltipListableOption {
            internal ProfileOption(ProviderProfile profile) {
                Profile = profile;
            }

            internal ProviderProfile Profile { get; }

            public string GetProperName() {
                return GetProfileName(Profile);
            }

            public string GetToolTipText() {
                return GetProfileDescription(Profile);
            }
        }
    }
}
