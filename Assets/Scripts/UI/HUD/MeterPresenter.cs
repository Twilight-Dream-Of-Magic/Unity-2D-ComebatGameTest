using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Presenter for meter numeric text and optional bar. Subscribes FighterResources from RoundManager.
	/// </summary>
	public class MeterPresenter : MonoBehaviour {
		public Systems.RoundManager round;
		public bool isP1 = true;
		public Text meterText;
		public MeterBarView bar;

		Fighter.Core.FighterResources res;

		void Awake() {
			if (meterText != null && string.IsNullOrEmpty(meterText.text))
			{
				meterText.text = "Meter --/--";
			}
		}
		void OnEnable() {
			if (round == null) { round = Systems.RoundManager.Instance; }
			BindToResources();
			HydrateOnce();
		}
		void OnDisable() {
			UnbindFromResources();
		}

		void BindToResources() {
			UnbindFromResources();
			if (round == null) { return; }
			res = isP1 ? round.p1Resources : round.p2Resources;
			if (res != null) { res.OnMeterChanged += OnMeter; }
		}
		void UnbindFromResources() {
			if (res != null) { res.OnMeterChanged -= OnMeter; }
			res = null;
		}
		void HydrateOnce() {
			var f = isP1 ? round?.p1 : round?.p2;
			if (f != null) { OnMeter(f.meter, f.maxMeter); }
		}
		void OnMeter(int current, int max) {
			if (meterText != null)
			{
				meterText.text = "Meter " + current + "/" + max;
			}
			if (bar != null)
			{
				bar.SetMax(max);
				bar.SetValue(current);
			}
		}
	}
}