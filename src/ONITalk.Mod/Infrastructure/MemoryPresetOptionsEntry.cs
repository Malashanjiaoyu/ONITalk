using System;
using System.Collections.Generic;
using System.Linq;
using ONITalk.Core;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using UnityEngine;

namespace ONITalk.Infrastructure {
    public sealed class MemoryPresetOptionsEntry : OptionsEntry {
        private readonly List<PresetOption> choices = Enum.GetValues(
            typeof(MemoryInjectionPreset)).Cast<MemoryInjectionPreset>()
            .Select(value => new PresetOption(value)).ToList();
        private MemoryInjectionPreset working = MemoryInjectionPreset.平衡;
        private GameObject? combo;

        public MemoryPresetOptionsEntry(string field, IOptionSpec spec) : base(field, spec) { }

        public override object Value {
            get => working;
            set {
                if (value is MemoryInjectionPreset preset)
                    working = preset;
                UpdateUI();
            }
        }

        public override GameObject GetUIComponent() {
            GameObject result = new PComboBox<PresetOption>("MemoryPreset") {
                Content = choices,
                InitialItem = Find(working),
                OnOptionSelected = (_, selected) => {
                    if (selected != null) working = selected.Value;
                },
                BackColor = PUITuning.Colors.ButtonPinkStyle,
                EntryColor = PUITuning.Colors.ButtonBlueStyle,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                MinWidth = 260,
                MaxRowsShown = 4
            }.Build();
            combo = result;
            return result;
        }

        private PresetOption Find(MemoryInjectionPreset value) {
            return choices.First(option => option.Value == value);
        }

        private void UpdateUI() {
            if (combo != null)
                PComboBox<PresetOption>.SetSelectedItem(combo, Find(working));
        }

        private sealed class PresetOption : ITooltipListableOption {
            internal PresetOption(MemoryInjectionPreset value) { Value = value; }

            internal MemoryInjectionPreset Value { get; }

            public string GetProperName() {
                switch (Value) {
                    case MemoryInjectionPreset.轻量:
                        return ONITalkLocalization.Get(STRINGS.ONITALK.MEMORY_PRESETS.LIGHT);
                    case MemoryInjectionPreset.丰富:
                        return ONITalkLocalization.Get(STRINGS.ONITALK.MEMORY_PRESETS.RICH);
                    case MemoryInjectionPreset.自定义:
                        return ONITalkLocalization.Get(STRINGS.ONITALK.MEMORY_PRESETS.CUSTOM);
                    default:
                        return ONITalkLocalization.Get(STRINGS.ONITALK.MEMORY_PRESETS.BALANCED);
                }
            }

            public string GetToolTipText() => GetProperName();
        }
    }
}
