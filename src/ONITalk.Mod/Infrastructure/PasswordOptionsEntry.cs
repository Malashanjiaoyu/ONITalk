using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

namespace ONITalk.Infrastructure {
    /// <summary>
    /// PLib string option rendered as a masked single-line field.
    /// </summary>
    public sealed class PasswordOptionsEntry : OptionsEntry {
        private const int MaximumLength = 256;
        private GameObject? textField;
        private string value = string.Empty;

        public PasswordOptionsEntry(string field, IOptionSpec spec) : base(field, spec) {
        }

        public override object Value {
            get => value;
            set {
                this.value = value?.ToString() ?? string.Empty;
                Update();
            }
        }

        public override GameObject GetUIComponent() {
            textField = new PTextField {
                OnTextChanged = OnTextChanged,
                ToolTip = LookInStrings(Tooltip),
                Text = value,
                MinWidth = 300,
                Type = PTextField.FieldType.Password,
                MaxLength = MaximumLength
            }.Build();
            Update();
            return textField;
        }

        private void OnTextChanged(GameObject _, string text) {
            value = text;
            Update();
        }

        private void Update() {
            TMP_InputField? field = textField?.GetComponentInChildren<TMP_InputField>();
            if (field != null && field.text != value)
                field.text = value;
        }
    }
}
