using UnityEngine;
using UnityEngine.UI;

namespace UI {
	/// <summary>
	/// Ensures a Canvas on this GameObject and applies Systems.SafeAreaClamp at root.
	/// Child UI should anchor within this canvas; no other SafeAreaClamp should be used in children.
	/// </summary>
	[DefaultExecutionOrder(-50)]
	public class CanvasRoot : MonoBehaviour {
		public Canvas Canvas
		{
			get;
			private set;
		}

		public RectTransform Rect
		{
			get;
			private set;
		}

		void Awake() {
			EnsureCanvas();
			EnsureSafeAreaClamp();
		}

		void EnsureCanvas() {
			Canvas existing = GetComponent<Canvas>();
			if (existing == null)
			{
				existing = gameObject.AddComponent<Canvas>();
			}
			existing.renderMode = RenderMode.ScreenSpaceOverlay;
			existing.sortingOrder = 10;
			CanvasScaler scaler = GetComponent<CanvasScaler>();
			if (scaler == null)
			{
				scaler = gameObject.AddComponent<CanvasScaler>();
			}
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			Canvas = existing;
			Rect = GetComponent<RectTransform>();
		}

		void EnsureSafeAreaClamp() {
			var clamp = GetComponent<Systems.SafeAreaClamp>();
			if (clamp == null)
			{
				clamp = gameObject.AddComponent<Systems.SafeAreaClamp>();
			}
			clamp.runEveryFrame = true;
		}
	}
}