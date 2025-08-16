using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Pure view for a slim meter bar. No data binding inside.
	/// </summary>
	public class MeterBarView : MonoBehaviour {
		public Slider slider;
		public Image fill;

		void Awake() {
			EnsureVisuals();
		}

		public void SetMax(int max) {
			if (slider == null)
			{
				return;
			}
			slider.minValue = 0f;
			slider.maxValue = max;
			slider.value = 0f;
		}

		public void SetValue(int value) {
			if (slider == null)
			{
				return;
			}
			slider.value = value;
		}

		public void SetColor(Color c) {
			if (fill != null)
			{
				fill.color = c;
			}
		}

		void EnsureVisuals() {
			if (slider == null)
			{
				slider = gameObject.AddComponent<Slider>();
			}
			Image bg = GetComponent<Image>();
			if (bg == null)
			{
				bg = gameObject.AddComponent<Image>();
			}
			bg.color = new Color(0f, 0f, 0f, 0.45f);
			var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
			fillAreaObject.transform.SetParent(transform, false);
			var fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
			fillAreaRect.anchorMin = new Vector2(0.02f, 0.2f);
			fillAreaRect.anchorMax = new Vector2(0.98f, 0.8f);
			fillAreaRect.offsetMin = Vector2.zero;
			fillAreaRect.offsetMax = Vector2.zero;
			var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
			fillObject.transform.SetParent(fillAreaObject.transform, false);
			fill = fillObject.GetComponent<Image>();
			fill.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
			slider.fillRect = fill.GetComponent<RectTransform>();
			slider.direction = Slider.Direction.LeftToRight;
		}
	}
}