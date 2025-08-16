using UnityEngine;

namespace Systems {
	/// <summary>
	/// Fits this RectTransform to the platform safe area by adjusting anchors (with optional margins).
	/// 透過調整 anchors 將此 RectTransform 鎖定到裝置安全區（可設置邊距）。
	/// 適配 CanvasScaler.ScaleWithScreenSize：會把 Screen.safeArea 映射到 Canvas 尺寸。
	/// </summary>
	[ExecuteAlways]
	public class SafeAreaClamp : MonoBehaviour {
		public Vector2 margin = new Vector2(16f, 16f);
		public bool runEveryFrame = true;

		RectTransform rectTransform;
		Canvas rootCanvas;
		RectTransform canvasRect;
		Rect lastAppliedSafeArea;

		void Awake() {
			rectTransform = GetComponent<RectTransform>();
			rootCanvas = GetComponentInParent<Canvas>();
			canvasRect = rootCanvas ? rootCanvas.GetComponent<RectTransform>() : null;
		}
		void OnEnable() { Apply(); }
		void Start() { Apply(); }
		void LateUpdate() {
			if (runEveryFrame)
			{
				Apply();
			}
		}

		void Apply() {
			if (!rectTransform || !canvasRect)
			{
				return;
			}
			Rect sa = Screen.safeArea;
			if (sa.width <= 0 || sa.height <= 0)
			{
				return;
			}
			if (lastAppliedSafeArea.Equals(sa) && !runEveryFrame)
			{
				return;
			}

			Vector2 canvasSize = canvasRect.rect.size;
			Vector2 screenSize = new Vector2(Screen.width, Screen.height);
			Vector2 scale = new Vector2(
				canvasSize.x / Mathf.Max(1f, screenSize.x),
				canvasSize.y / Mathf.Max(1f, screenSize.y)
			);
			Vector2 safeMin = Vector2.Scale(sa.position, scale);
			Vector2 safeSize = Vector2.Scale(sa.size, scale);
			Vector2 safeMax = safeMin + safeSize;

			Vector2 anchorMin = new Vector2(
				Mathf.Clamp01(safeMin.x / Mathf.Max(1f, canvasSize.x)),
				Mathf.Clamp01(safeMin.y / Mathf.Max(1f, canvasSize.y))
			);
			Vector2 anchorMax = new Vector2(
				Mathf.Clamp01(safeMax.x / Mathf.Max(1f, canvasSize.x)),
				Mathf.Clamp01(safeMax.y / Mathf.Max(1f, canvasSize.y))
			);

			Vector2 m = new Vector2(
				margin.x / Mathf.Max(1f, canvasSize.x),
				margin.y / Mathf.Max(1f, canvasSize.y)
			);
			anchorMin += m;
			anchorMax -= m;

			rectTransform.anchorMin = anchorMin;
			rectTransform.anchorMax = anchorMax;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			lastAppliedSafeArea = sa;
		}
	}
}