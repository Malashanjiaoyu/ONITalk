using System;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONITalk.UI {
    internal sealed class ONITalkMemoryPreviewPanel : MonoBehaviour {
        private const float HeaderHeight = 38f;
        private System.Action? closed;
        private bool closing;

        internal static ONITalkMemoryPreviewPanel Create(GameObject parent, string text,
                System.Action onClosed) {
            GameObject root = PUIElements.CreateUI(parent, "ONITalkMemoryPreviewPanel");
            try {
                RectTransform? parentRect = parent.GetComponent<RectTransform>();
                if (parentRect == null)
                    parentRect = parent.GetComponentInParent<Canvas>()?
                        .GetComponent<RectTransform>();
                if (parentRect == null)
                    throw new InvalidOperationException(
                        "The game overlay canvas has no RectTransform.");
                var panel = root.AddComponent<ONITalkMemoryPreviewPanel>();
                panel.Initialize(parentRect, text, onClosed);
                return panel;
            } catch {
                Destroy(root);
                throw;
            }
        }

        private void Initialize(RectTransform parentRect, string preview,
                System.Action onClosed) {
            closed = onClosed;
            RectTransform window = gameObject.rectTransform();
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(
                Mathf.Clamp(parentRect.rect.width * 0.48f, 480f, 720f),
                Mathf.Clamp(parentRect.rect.height * 0.62f, 360f, 620f));
            window.anchoredPosition = Vector2.zero;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.AddComponent<Image>().color = new Color32(20, 23, 29, 247);

            BuildHeader(window, parentRect);
            BuildBody(preview ?? string.Empty);
        }

        private void BuildHeader(RectTransform window, RectTransform parentRect) {
            GameObject header = PUIElements.CreateUI(gameObject, "Header");
            RectTransform headerRect = header.rectTransform();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -HeaderHeight);
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color32(135, 69, 102, 255);
            header.AddComponent<ChatWindowDragHandle>().Initialize(window, parentRect,
                () => { });

            GameObject title = PUIElements.CreateUI(header, "Title");
            RectTransform titleRect = title.rectTransform();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-48f, 0f);
            LocText titleText = PUIElements.AddLocText(title,
                PUITuning.Fonts.UILightStyle.DeriveStyle(size: 16,
                    style: FontStyles.Bold));
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.raycastTarget = false;
            titleText.text = ONITalkLocalization.Get(
                STRINGS.ONITALK.UI.MEMORY_PREVIEW.TITLE);

            GameObject close = new PButton("Close") {
                Text = "×",
                ToolTip = ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.MEMORY_PREVIEW.CLOSE_TOOLTIP),
                OnClick = _ => Close(),
                Margin = new RectOffset(4, 4, 2, 2),
                DynamicSize = true
            }.SetKleiBlueStyle().Build();
            close.SetParent(header);
            RectTransform closeRect = close.rectTransform();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(28f, 27f);
            closeRect.anchoredPosition = new Vector2(-20f, 0f);
        }

        private void BuildBody(string preview) {
            GameObject viewport = PUIElements.CreateUI(gameObject, "Body");
            RectTransform viewportRect = viewport.rectTransform();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 10f);
            viewportRect.offsetMax = new Vector2(-10f, -HeaderHeight - 8f);
            viewport.AddComponent<Image>().color = new Color32(11, 13, 18, 210);
            viewport.AddComponent<RectMask2D>();

            var scroll = viewport.AddComponent<KScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewportRect;

            GameObject content = PUIElements.CreateUI(viewport, "Content");
            RectTransform contentRect = content.rectTransform();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObject = PUIElements.CreateUI(content, "PreviewText");
            LocText label = PUIElements.AddLocText(textObject,
                PUITuning.Fonts.TextLightStyle.DeriveStyle(size: 14));
            label.alignment = TextAlignmentOptions.TopLeft;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = preview;
            textObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
        }

        private void Close() {
            if (closing)
                return;
            closing = true;
            System.Action? callback = closed;
            closed = null;
            callback?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy() {
            if (closing)
                return;
            closing = true;
            System.Action? callback = closed;
            closed = null;
            callback?.Invoke();
        }
    }
}
