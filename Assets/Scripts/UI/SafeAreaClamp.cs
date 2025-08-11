using UnityEngine;

namespace UI {
    public class SafeAreaClamp : MonoBehaviour {
        public Vector2 margin = new Vector2(16f, 16f);
        public bool runEveryFrame = true;

        RectTransform rectTransform;
        Canvas rootCanvas;
        RectTransform canvasRect;

        void Awake() {
            rectTransform = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            canvasRect = rootCanvas ? rootCanvas.GetComponent<RectTransform>() : null;
        }

        void Start() { Clamp(); }
        void LateUpdate() { if (runEveryFrame) Clamp(); }

        void Clamp() {
            if (!rectTransform || !canvasRect) return;

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 half = canvasSize * 0.5f;

            // Compute anchor point in canvas local space (relative to center)
            Vector2 anchor = new Vector2(
                (rectTransform.anchorMin.x - 0.5f) * canvasSize.x,
                (rectTransform.anchorMin.y - 0.5f) * canvasSize.y
            );

            // Current local position relative to canvas center
            Vector2 local = anchor + rectTransform.anchoredPosition;

            // Clamp to safe area
            Vector2 clamped = new Vector2(
                Mathf.Clamp(local.x, -half.x + margin.x, half.x - margin.x),
                Mathf.Clamp(local.y, -half.y + margin.y, half.y - margin.y)
            );

            rectTransform.anchoredPosition = clamped - anchor;
        }
    }
}