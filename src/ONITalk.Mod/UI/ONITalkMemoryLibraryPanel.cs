using System;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONITalk.UI {
    internal sealed class ONITalkMemoryLibraryPanel : MonoBehaviour {
        private const float FooterHeight = 52f;
        private const float HeaderHeight = 38f;
        private System.Action? closed;
        private System.Func<string>? clearMemory;
        private RectTransform? contentRect;
        private System.Func<string>? contentProvider;
        private System.Func<string>? exportMemory;
        private LocText? contentText;
        private bool closing;

        internal static ONITalkMemoryLibraryPanel Create(GameObject parent,
                System.Func<string> getContent, System.Func<string> onExport,
                System.Func<string> onClear, System.Action onClosed) {
            GameObject root = PUIElements.CreateUI(parent, "ONITalkMemoryLibraryPanel");
            try {
                RectTransform? parentRect = parent.GetComponent<RectTransform>();
                if (parentRect == null)
                    parentRect = parent.GetComponentInParent<Canvas>()?
                        .GetComponent<RectTransform>();
                if (parentRect == null)
                    throw new InvalidOperationException(
                        "The game overlay canvas has no RectTransform.");
                var panel = root.AddComponent<ONITalkMemoryLibraryPanel>();
                panel.Initialize(parentRect, getContent, onExport, onClear, onClosed);
                return panel;
            } catch {
                Destroy(root);
                throw;
            }
        }

        private void Initialize(RectTransform parentRect,
                System.Func<string> getContent, System.Func<string> onExport,
                System.Func<string> onClear, System.Action onClosed) {
            contentProvider = getContent;
            exportMemory = onExport;
            clearMemory = onClear;
            closed = onClosed;

            RectTransform window = gameObject.rectTransform();
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(
                Mathf.Clamp(parentRect.rect.width * 0.56f, 560f, 820f),
                Mathf.Clamp(parentRect.rect.height * 0.70f, 430f, 700f));
            window.anchoredPosition = Vector2.zero;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 51;
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.AddComponent<Image>().color = new Color32(20, 23, 29, 247);

            BuildHeader(window, parentRect);
            BuildBody();
            BuildFooter();
            Refresh();
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
                STRINGS.ONITALK.UI.MEMORY_LIBRARY.TITLE);

            GameObject close = BuildButton("Close", "×",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLOSE_TOOLTIP),
                _ => Close(), true);
            close.SetParent(header);
            RectTransform closeRect = close.rectTransform();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(28f, 27f);
            closeRect.anchoredPosition = new Vector2(-20f, 0f);
        }

        private void BuildBody() {
            GameObject viewport = PUIElements.CreateUI(gameObject, "Body");
            RectTransform viewportRect = viewport.rectTransform();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, FooterHeight + 6f);
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
            contentRect = content.rectTransform();
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
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObject = PUIElements.CreateUI(content, "LibraryText");
            contentText = PUIElements.AddLocText(textObject,
                PUITuning.Fonts.TextLightStyle.DeriveStyle(size: 14));
            contentText.alignment = TextAlignmentOptions.TopLeft;
            contentText.overflowMode = TextOverflowModes.Overflow;
            contentText.raycastTarget = false;
            contentText.textWrappingMode = TextWrappingModes.Normal;
            textObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
        }

        private void BuildFooter() {
            GameObject footer = PUIElements.CreateUI(gameObject, "Footer");
            RectTransform footerRect = footer.rectTransform();
            footerRect.anchorMin = Vector2.zero;
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.offsetMin = new Vector2(10f, 6f);
            footerRect.offsetMax = new Vector2(-10f, FooterHeight);
            var layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            BuildButton("Refresh",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.REFRESH_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.REFRESH_TOOLTIP),
                _ => Refresh(), false).SetParent(footer);
            BuildButton("Export",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.EXPORT_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.EXPORT_TOOLTIP),
                _ => Export(), false).SetParent(footer);
            BuildButton("Clear",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_TOOLTIP),
                _ => ConfirmClear(), false).SetParent(footer);
        }

        private static GameObject BuildButton(string name, string text, string tooltip,
                PUIDelegates.OnButtonPressed clicked, bool blue) {
            var button = new PButton(name) {
                Text = text,
                ToolTip = tooltip,
                OnClick = clicked,
                Margin = new RectOffset(8, 8, 3, 3),
                DynamicSize = true
            };
            return (blue ? button.SetKleiBlueStyle() : button.SetKleiPinkStyle()).Build();
        }

        private void Refresh() {
            if (contentText == null)
                return;
            try {
                contentText.text = contentProvider?.Invoke() ??
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.NO_INFORMATION);
            } catch (Exception error) {
                contentText.text = ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.MEMORY_LIBRARY.READ_FAILED, error.Message);
            }
            if (contentRect != null) {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }

        private void Export() {
            string result;
            try {
                result = exportMemory?.Invoke() ??
                    ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.MEMORY_LIBRARY.EXPORT_UNAVAILABLE);
            } catch (Exception error) {
                result = ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.MEMORY_LIBRARY.EXPORT_FAILED, error.Message);
            }
            ShowTopMessage(result);
        }

        private void ConfirmClear() {
            ShowTopConfirm(
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_CONFIRM),
                () => ShowTopConfirm(
                    ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_FINAL_CONFIRM),
                    ClearConfirmed, null,
                    ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.MEMORY_LIBRARY.CONFIRM_CLEAR_BUTTON),
                    ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CANCEL_BUTTON)),
                null,
                ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.MEMORY_LIBRARY.CONTINUE_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.MEMORY_LIBRARY.CANCEL_BUTTON));
        }

        private void ClearConfirmed() {
            string result;
            try {
                result = clearMemory?.Invoke() ??
                    ONITalkLocalization.Get(
                        STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_UNAVAILABLE);
            } catch (Exception error) {
                result = ONITalkLocalization.Format(
                    STRINGS.ONITALK.UI.MEMORY_LIBRARY.CLEAR_FAILED, error.Message);
            }
            Refresh();
            ShowTopMessage(result);
        }

        private static void ShowTopMessage(string message) {
            BringDialogToFront(PUIElements.ShowMessageDialog(null!, message));
        }

        private static void ShowTopConfirm(string message, System.Action onConfirm,
                System.Action? onCancel, string confirmText, string cancelText) {
            BringDialogToFront(PUIElements.ShowConfirmDialog(null!, message, onConfirm,
                onCancel!, confirmText, cancelText));
        }

        private static void BringDialogToFront(ConfirmDialogScreen? dialog) {
            if (dialog == null)
                return;
            GameObject root = dialog.gameObject;
            root.transform.SetAsLastSibling();
            Canvas canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
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
