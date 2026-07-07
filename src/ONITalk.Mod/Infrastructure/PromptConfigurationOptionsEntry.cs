using System;
using System.Collections.Generic;
using ONITalk.Core;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

namespace ONITalk.Infrastructure {
    public sealed class PromptConfiguration {
        public bool Enabled { get; set; }
        public string Template { get; set; } = PromptCustomization.StarterTemplate;

        public PromptConfiguration Clone() {
            return new PromptConfiguration { Enabled = Enabled, Template = Template ?? "" };
        }
    }

    public sealed class PromptConfigurationOptionsEntry : OptionsEntry {
        private PromptConfiguration working = new PromptConfiguration();
        private GameObject? textArea;
        private LocText? status;

        public PromptConfigurationOptionsEntry(string field, IOptionSpec spec) : base(field, spec) { }

        public override object Value {
            get => working.Clone();
            set { if (value is PromptConfiguration prompt) working = prompt.Clone(); }
        }

        public override GameObject GetUIComponent() {
            var panel = new PPanel("PromptConfiguration") {
                Direction = PanelDirection.Vertical, Spacing = 6,
                Alignment = TextAnchor.UpperLeft, FlexSize = Vector2.right
            };
            panel.AddChild(new PCheckBox("CustomPromptEnabled") {
                Text = T(STRINGS.ONITALK.UI.PROMPT.ENABLE),
                ToolTip = T(STRINGS.ONITALK.UI.PROMPT.ENABLE_TOOLTIP),
                InitialState = working.Enabled ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED,
                OnChecked = (source, state) => {
                    // PLib reports the state before the click; advance and redraw explicitly.
                    working.Enabled = state == PCheckBox.STATE_UNCHECKED;
                    PCheckBox.SetCheckState(source, working.Enabled ? PCheckBox.STATE_CHECKED :
                        PCheckBox.STATE_UNCHECKED);
                }
            });
            panel.AddChild(new PLabel("Variables") {
                Text = T(STRINGS.ONITALK.UI.PROMPT.VARIABLES),
                TextStyle = PUITuning.Fonts.TextLightStyle, DynamicSize = true
            });
            panel.AddChild(new PTextArea("CustomPrompt") {
                Text = working.Template, LineCount = 7,
                MaxLength = PromptCustomization.MaximumLength, MinWidth = 480,
                ToolTip = T(STRINGS.ONITALK.UI.PROMPT.EDITOR_TOOLTIP),
                OnTextChanged = (_, value) => { working.Template = value; Validate(); }
            }.SetKleiBlueStyle().AddOnRealize(go => textArea = go));
            panel.AddChild(new PLabel("Status") {
                Text = T(STRINGS.ONITALK.UI.PROMPT.STATUS_OK),
                TextStyle = PUITuning.Fonts.TextLightStyle, DynamicSize = true
            }.AddOnRealize(go => status = go.GetComponentInChildren<LocText>()));
            var buttons = new PPanel("Buttons") { Direction = PanelDirection.Horizontal,
                Spacing = 8, FlexSize = Vector2.right };
            buttons.AddChild(new PButton("Reset") { Text = T(STRINGS.ONITALK.UI.PROMPT.RESET),
                OnClick = _ => Reset(), DynamicSize = true }.SetKleiPinkStyle());
            buttons.AddChild(new PButton("CopyDefault") { Text = T(STRINGS.ONITALK.UI.PROMPT.COPY_DEFAULT),
                OnClick = _ => CopyDefault(), DynamicSize = true }.SetKleiPinkStyle());
            buttons.AddChild(new PButton("Preview") { Text = T(STRINGS.ONITALK.UI.PROMPT.PREVIEW),
                OnClick = _ => Preview(), DynamicSize = true }.SetKleiPinkStyle());
            panel.AddChild(buttons);
            return panel.Build();
        }

        private void Reset() {
            working.Template = PromptCustomization.StarterTemplate;
            TMP_InputField? input = textArea?.GetComponent<TMP_InputField>();
            if (input != null) input.text = working.Template;
            Validate();
        }

        private void Validate() {
            IReadOnlyList<string> unknown = PromptCustomization.FindUnknownVariables(working.Template);
            if (status != null)
                status.text = unknown.Count == 0 ? T(STRINGS.ONITALK.UI.PROMPT.STATUS_OK) :
                    ONITalkLocalization.Format(STRINGS.ONITALK.UI.PROMPT.STATUS_UNKNOWN,
                        string.Join(", ", unknown));
        }

        private void Preview() {
            var context = new ConversationContext(new DupeSnapshot { Name = "Ada",
                CurrentTask = "Building", Element = "Oxygen" },
                new DupeSnapshot { Name = "Burt" }, "Ambient chatter");
            string template = working.Enabled ? working.Template : string.Empty;
            string preview = PromptBuilder.BuildSystemPrompt(
                ONITalkLocalization.DialogueLanguageName, 80, template, context);
            PUIElements.ShowMessageDialog(null!, preview);
        }

        private void CopyDefault() {
            GUIUtility.systemCopyBuffer = PromptBuilder.BuildSystemPrompt(
                ONITalkLocalization.DialogueLanguageName, 80);
            if (status != null) status.text = T(STRINGS.ONITALK.UI.PROMPT.COPIED);
        }

        private static string T(LocString value) => ONITalkLocalization.Get(value);
    }
}
