using System;
using System.Collections.Generic;
using ONITalk.Core;
using ONITalk.Infrastructure;
using ONITalk.LocalizationSupport;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ONITalk.UI {
    internal sealed class ONITalkChatPanel : MonoBehaviour, IPointerEnterHandler,
            IPointerExitHandler {
        private const float HeaderHeight = 34f;
        private const float MinimumHeight = 180f;
        private const float MinimumWidth = 320f;
        private readonly List<LocText> messageLabels = new List<LocText>();
        private readonly List<GameObject> messageRows = new List<GameObject>();

        private Image? background;
        private Image? bodyBackground;
        private Image? headerBackground;
        private ONITalkConfig? config;
        private RectTransform? contentRect;
        private System.Action? historyCleared;
        private System.Action? memoryPreviewRequested;
        private System.Action? memoryLibraryRequested;
        private TextStyleSetting? messageStyle;
        private GameObject? messageViewport;
        private GameObject? resizeHandle;
        private KScrollRect? scrollRect;
        private GameObject? libraryButton;
        private LocText? libraryButtonText;
        private GameObject? memoryButton;
        private LocText? memoryButtonText;
        private GameObject? clearButton;
        private LocText? clearButtonText;
        private GameObject? minimizeButton;
        private LocText? minimizeButtonText;
        private LocText? titleText;
        private RectTransform? parentRect;
        private bool pointerInside;
        private Vector2 previousParentSize;
        private RectTransform? windowRect;

        internal static ONITalkChatPanel Create(GameObject parent, ONITalkConfig options,
                System.Action onHistoryCleared, System.Action onMemoryPreview,
                System.Action onMemoryLibrary) {
            GameObject root = PUIElements.CreateUI(parent, "ONITalkChatPanel");
            try {
                RectTransform? overlayRect = parent.GetComponent<RectTransform>();
                if (overlayRect == null) {
                    Canvas? parentCanvas = parent.GetComponentInParent<Canvas>();
                    overlayRect = parentCanvas?.GetComponent<RectTransform>();
                }
                if (overlayRect == null)
                    throw new InvalidOperationException(
                        "The game overlay canvas has no RectTransform.");
                var panel = root.AddComponent<ONITalkChatPanel>();
                panel.Initialize(overlayRect, options, onHistoryCleared,
                    onMemoryPreview, onMemoryLibrary);
                return panel;
            } catch {
                Destroy(root);
                throw;
            }
        }

        internal void AddMessage(DialogueMessage message) {
            if (contentRect == null || config == null || !message.IsValid)
                return;

            string target = string.IsNullOrWhiteSpace(message.Listener)
                ? message.Speaker
                : message.Speaker + " → " + message.Listener;
            string display = target + "\n" + message.Text;
            bool alternate = messageRows.Count % 2 == 1;
            GameObject row = CreateMessageRow(display, alternate);
            messageRows.Add(row);
            LocText? label = row.GetComponentInChildren<LocText>();
            if (label != null)
                messageLabels.Add(label);
            TrimRows();
            ScrollToLatest();
        }

        internal void ApplyOptions(ONITalkConfig options) {
            config = options;
            gameObject.SetActive(options.ChatWindowEnabled);
            RefreshLocalization();
            ApplyOpacity();
            foreach (LocText label in messageLabels)
                label.fontSize = options.ChatFontSize;
            if (messageStyle != null)
                messageStyle.fontSize = options.ChatFontSize;
            TrimRows();
            ApplyLayout();
        }

        internal void ClearMessages() {
            foreach (GameObject row in messageRows)
                if (row != null)
                    Destroy(row);
            messageRows.Clear();
            messageLabels.Clear();
        }

        internal void Rebuild(IReadOnlyList<DialogueMessage> messages) {
            ClearMessages();
            foreach (DialogueMessage message in messages)
                AddMessage(message);
            ScrollToLatest();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            pointerInside = true;
            ApplyOpacity();
        }

        public void OnPointerExit(PointerEventData eventData) {
            pointerInside = false;
            ApplyOpacity();
        }

        private void Initialize(RectTransform parent, ONITalkConfig options,
                System.Action onHistoryCleared, System.Action onMemoryPreview,
                System.Action onMemoryLibrary) {
            parentRect = parent;
            config = options;
            historyCleared = onHistoryCleared;
            memoryPreviewRequested = onMemoryPreview;
            memoryLibraryRequested = onMemoryLibrary;
            messageStyle = PUITuning.Fonts.TextLightStyle.DeriveStyle(
                size: options.ChatFontSize);
            messageStyle.enableWordWrapping = true;
            windowRect = gameObject.rectTransform();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0f, 1f);
            windowRect.pivot = new Vector2(0f, 1f);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;
            gameObject.AddComponent<GraphicRaycaster>();

            background = gameObject.AddComponent<Image>();
            background.color = new Color32(31, 34, 43, 235);

            try {
                BuildHeader();
            } catch (Exception error) {
                throw new InvalidOperationException("Header creation failed.", error);
            }
            try {
                BuildMessageArea();
            } catch (Exception error) {
                throw new InvalidOperationException("Message area creation failed.", error);
            }
            try {
                BuildResizeHandle();
            } catch (Exception error) {
                throw new InvalidOperationException("Resize handle creation failed.", error);
            }
            try {
                ApplyOptions(options);
            } catch (Exception error) {
                throw new InvalidOperationException("Initial layout failed.", error);
            }
            previousParentSize = parent.rect.size;
        }

        private void BuildHeader() {
            if (windowRect == null || parentRect == null)
                return;

            GameObject header = PUIElements.CreateUI(gameObject, "Header");
            RectTransform headerRect = header.rectTransform();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -HeaderHeight);
            headerRect.offsetMax = Vector2.zero;
            headerBackground = header.AddComponent<Image>();
            headerBackground.color = new Color32(135, 69, 102, 255);
            header.AddComponent<ChatWindowDragHandle>().Initialize(windowRect, parentRect,
                PersistLayout);

            GameObject title = PUIElements.CreateUI(header, "Title");
            RectTransform titleRect = title.rectTransform();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10f, 0f);
            titleRect.offsetMax = new Vector2(-151f, 0f);
            titleText = PUIElements.AddLocText(title,
                PUITuning.Fonts.UILightStyle.DeriveStyle(size: 15, style: FontStyles.Bold));
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.raycastTarget = false;
            titleText.text = ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.TITLE);

            libraryButton = CreateHeaderButton(header, "Library",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.LIBRARY_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.LIBRARY_TOOLTIP), -114f,
                _ => memoryLibraryRequested?.Invoke(), out libraryButtonText);
            memoryButton = CreateHeaderButton(header, "Memory",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.MEMORY_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.MEMORY_TOOLTIP), -85f,
                _ => memoryPreviewRequested?.Invoke(), out memoryButtonText);
            clearButton = CreateHeaderButton(header, "Clear",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.CLEAR_BUTTON),
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.CLEAR_TOOLTIP), -56f,
                _ => {
                    historyCleared?.Invoke();
                    ClearMessages();
                }, out clearButtonText);
            minimizeButton = CreateHeaderButton(header, "Minimize", "—",
                ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.MINIMIZE_TOOLTIP), -27f,
                _ => ToggleMinimized(), out minimizeButtonText);
        }

        private static GameObject CreateHeaderButton(GameObject header, string name, string text,
                string tooltip, float rightOffset, PUIDelegates.OnButtonPressed clicked,
                out LocText? buttonText) {
            GameObject button = new PButton(name) {
                Text = text,
                ToolTip = tooltip,
                OnClick = clicked,
                Margin = new RectOffset(3, 3, 2, 2),
                DynamicSize = true
            }.SetKleiBlueStyle().Build();
            button.SetParent(header);
            RectTransform rect = button.rectTransform();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(25f, 24f);
            rect.anchoredPosition = new Vector2(rightOffset, 0f);
            buttonText = button.GetComponentInChildren<LocText>();
            return button;
        }

        private void RefreshLocalization() {
            if (titleText != null)
                titleText.text = ONITalkLocalization.Get(STRINGS.ONITALK.UI.CHAT.TITLE);
            if (libraryButtonText != null)
                libraryButtonText.text = ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.LIBRARY_BUTTON);
            if (memoryButtonText != null)
                memoryButtonText.text = ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.MEMORY_BUTTON);
            if (clearButtonText != null)
                clearButtonText.text = ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.CLEAR_BUTTON);
            if (libraryButton != null)
                PUIElements.SetToolTip(libraryButton, ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.LIBRARY_TOOLTIP));
            if (memoryButton != null)
                PUIElements.SetToolTip(memoryButton, ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.MEMORY_TOOLTIP));
            if (clearButton != null)
                PUIElements.SetToolTip(clearButton, ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.CLEAR_TOOLTIP));
            if (minimizeButton != null)
                PUIElements.SetToolTip(minimizeButton, ONITalkLocalization.Get(
                    STRINGS.ONITALK.UI.CHAT.MINIMIZE_TOOLTIP));
        }

        private void BuildMessageArea() {
            GameObject viewport = PUIElements.CreateUI(gameObject, "Messages");
            messageViewport = viewport;
            RectTransform viewportRect = viewport.rectTransform();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(5f, 7f);
            viewportRect.offsetMax = new Vector2(-5f, -HeaderHeight - 4f);
            bodyBackground = viewport.AddComponent<Image>();
            bodyBackground.color = new Color32(20, 22, 29, 235);
            viewport.AddComponent<RectMask2D>();

            scrollRect = viewport.AddComponent<KScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewportRect;

            GameObject content = PUIElements.CreateUI(viewport, "Content");
            contentRect = content.rectTransform();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;
        }

        private void BuildResizeHandle() {
            if (windowRect == null || parentRect == null)
                return;
            GameObject handle = PUIElements.CreateUI(gameObject, "ResizeHandle");
            resizeHandle = handle;
            RectTransform rect = handle.rectTransform();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(22f, 22f);
            rect.anchoredPosition = Vector2.zero;
            Image image = handle.AddComponent<Image>();
            image.color = new Color32(135, 69, 102, 230);
            handle.AddComponent<ChatWindowResizeHandle>().Initialize(windowRect, parentRect,
                PersistLayout);

            GameObject glyph = PUIElements.CreateUI(handle, "Glyph");
            RectTransform glyphRect = glyph.rectTransform();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            LocText hint = PUIElements.AddLocText(glyph,
                PUITuning.Fonts.UILightStyle.DeriveStyle(size: 12));
            hint.alignment = TextAlignmentOptions.BottomRight;
            hint.raycastTarget = false;
            hint.text = "//";
        }

        private GameObject CreateMessageRow(string text, bool alternate) {
            if (contentRect == null || config == null)
                throw new InvalidOperationException("Chat panel is not initialized.");
            GameObject row = PUIElements.CreateUI(contentRect.gameObject, "Message");
            Image image = row.AddComponent<Image>();
            image.color = alternate ? new Color32(255, 255, 255, 12) :
                new Color32(255, 255, 255, 5);
            image.raycastTarget = false;

            var rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(7, 7, 5, 5);
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject textObject = PUIElements.CreateUI(row, "MessageText");
            LocText label = PUIElements.AddLocText(textObject, messageStyle ??
                PUITuning.Fonts.TextLightStyle);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;

            var fitter = row.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.AddComponent<LayoutElement>().minHeight = 36f;
            return row;
        }

        private void ApplyLayout() {
            if (config == null || parentRect == null || windowRect == null)
                return;
            Vector2 parentSize = parentRect.rect.size;
            if (parentSize.x < 1f || parentSize.y < 1f)
                return;

            float width = Mathf.Clamp(parentSize.x * config.ChatWindowWidth, MinimumWidth,
                Mathf.Max(MinimumWidth, parentSize.x * 0.8f));
            float expandedHeight = Mathf.Clamp(parentSize.y * config.ChatWindowHeight,
                MinimumHeight, Mathf.Max(MinimumHeight, parentSize.y * 0.8f));
            float visibleHeight = config.ChatWindowMinimized ? HeaderHeight : expandedHeight;
            windowRect.sizeDelta = new Vector2(width, visibleHeight);
            windowRect.anchoredPosition = new Vector2(parentSize.x * config.ChatWindowX,
                -parentSize.y * config.ChatWindowY);
            ChatWindowGeometry.ClampToParent(windowRect, parentRect);
            SetMinimizedVisual(config.ChatWindowMinimized);
        }

        private void ApplyOpacity() {
            if (config == null)
                return;
            float idle = config.ChatWindowOpacity;
            float bodyOpacity = config.ChatWindowAutoFade && pointerInside ?
                Mathf.Max(idle, 0.65f) : idle;
            float headerOpacity = config.ChatWindowAutoFade && pointerInside ?
                Mathf.Max(idle, 0.78f) : Mathf.Max(idle, 0.35f);
            byte bodyAlpha = (byte)Mathf.RoundToInt(bodyOpacity * 255f);
            byte headerAlpha = (byte)Mathf.RoundToInt(headerOpacity * 255f);
            if (background != null)
                background.color = new Color32(31, 34, 43, 0);
            if (bodyBackground != null)
                bodyBackground.color = new Color32(20, 22, 29, bodyAlpha);
            if (headerBackground != null)
                headerBackground.color = new Color32(135, 69, 102, headerAlpha);
        }

        private void PersistLayout() {
            if (config == null || parentRect == null || windowRect == null)
                return;
            Vector2 parentSize = parentRect.rect.size;
            if (parentSize.x < 1f || parentSize.y < 1f)
                return;
            config.ChatWindowX = Mathf.Clamp01(windowRect.anchoredPosition.x / parentSize.x);
            config.ChatWindowY = Mathf.Clamp01(-windowRect.anchoredPosition.y / parentSize.y);
            if (!config.ChatWindowMinimized) {
                config.ChatWindowWidth = Mathf.Clamp(windowRect.sizeDelta.x / parentSize.x,
                    0.2f, 0.8f);
                config.ChatWindowHeight = Mathf.Clamp(windowRect.sizeDelta.y / parentSize.y,
                    0.15f, 0.8f);
            }
            config.Save();
        }

        private void ScrollToLatest() {
            if (contentRect == null || scrollRect == null)
                return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void SetMinimizedVisual(bool minimized) {
            messageViewport?.SetActive(!minimized);
            resizeHandle?.SetActive(!minimized);
            if (minimizeButtonText != null)
                minimizeButtonText.text = minimized ? "+" : "—";
        }

        private void ToggleMinimized() {
            if (config == null || parentRect == null || windowRect == null)
                return;
            bool minimize = !config.ChatWindowMinimized;
            if (minimize) {
                config.ChatWindowMinimized = true;
                windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, HeaderHeight);
            } else {
                config.ChatWindowMinimized = false;
                float height = Mathf.Clamp(parentRect.rect.height * config.ChatWindowHeight,
                    MinimumHeight, Mathf.Max(MinimumHeight, parentRect.rect.height * 0.8f));
                windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, height);
            }
            SetMinimizedVisual(minimize);
            ChatWindowGeometry.ClampToParent(windowRect, parentRect);
            PersistLayout();
        }

        private void TrimRows() {
            if (config == null)
                return;
            while (messageRows.Count > config.ChatHistoryLimit) {
                GameObject oldest = messageRows[0];
                messageRows.RemoveAt(0);
                if (messageLabels.Count > 0)
                    messageLabels.RemoveAt(0);
                if (oldest != null)
                    Destroy(oldest);
            }
        }

        private void Update() {
            if (parentRect == null)
                return;
            Vector2 current = parentRect.rect.size;
            if ((current - previousParentSize).sqrMagnitude > 1f) {
                previousParentSize = current;
                ApplyLayout();
            }
        }
    }
}
