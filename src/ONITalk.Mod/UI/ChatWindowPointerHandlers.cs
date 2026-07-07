using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ONITalk.UI {
    internal static class ChatWindowGeometry {
        internal static void ClampToParent(RectTransform target, RectTransform parent) {
            Vector2 parentSize = parent.rect.size;
            Vector2 size = target.rect.size;
            float x = Mathf.Clamp(target.anchoredPosition.x, 0f,
                Mathf.Max(0f, parentSize.x - size.x));
            float top = Mathf.Clamp(-target.anchoredPosition.y, 0f,
                Mathf.Max(0f, parentSize.y - size.y));
            target.anchoredPosition = new Vector2(x, -top);
        }

        internal static float CanvasScale(RectTransform target) {
            Canvas? canvas = target.GetComponentInParent<Canvas>();
            return canvas == null ? 1f : Mathf.Max(0.01f, canvas.rootCanvas.scaleFactor);
        }
    }

    internal sealed class ChatWindowDragHandle : MonoBehaviour, IBeginDragHandler,
            IDragHandler, IEndDragHandler {
        private System.Action? completed;
        private RectTransform? parentRect;
        private RectTransform? target;

        internal void Initialize(RectTransform moveTarget, RectTransform parent,
                System.Action onCompleted) {
            target = moveTarget;
            parentRect = parent;
            completed = onCompleted;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left)
                target?.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left || target == null ||
                    parentRect == null)
                return;
            target.anchoredPosition += eventData.delta / ChatWindowGeometry.CanvasScale(target);
            ChatWindowGeometry.ClampToParent(target, parentRect);
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left)
                completed?.Invoke();
        }
    }

    internal sealed class ChatWindowResizeHandle : MonoBehaviour, IBeginDragHandler,
            IDragHandler, IEndDragHandler {
        private const float MinimumHeight = 180f;
        private const float MinimumWidth = 320f;
        private System.Action? completed;
        private RectTransform? parentRect;
        private RectTransform? target;

        internal void Initialize(RectTransform resizeTarget, RectTransform parent,
                System.Action onCompleted) {
            target = resizeTarget;
            parentRect = parent;
            completed = onCompleted;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left)
                target?.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left || target == null ||
                    parentRect == null)
                return;

            Vector2 delta = eventData.delta / ChatWindowGeometry.CanvasScale(target);
            Vector2 parentSize = parentRect.rect.size;
            float width = Mathf.Clamp(target.sizeDelta.x + delta.x, MinimumWidth,
                Mathf.Max(MinimumWidth, parentSize.x * 0.8f));
            float height = Mathf.Clamp(target.sizeDelta.y - delta.y, MinimumHeight,
                Mathf.Max(MinimumHeight, parentSize.y * 0.8f));
            target.sizeDelta = new Vector2(width, height);
            ChatWindowGeometry.ClampToParent(target, parentRect);
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left)
                completed?.Invoke();
        }
    }
}
