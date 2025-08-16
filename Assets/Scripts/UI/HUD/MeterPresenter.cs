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
		bool _bound;

		void Awake() {
			if (meterText != null && string.IsNullOrEmpty(meterText.text))
			{
				meterText.text = "Meter --/--";
			}
		}
		void OnEnable() {
			if (round == null) { round = Systems.RoundManager.Instance; }
			if (!_bound)
			{
				BindToResources();
				HydrateOnce();
			}
		}
		void OnDisable() {
			UnbindFromResources();
		}

		void BindToResources() {
			if (round == null) { return; }
			var newRes = isP1 ? round.p1Resources : round.p2Resources;
			if (newRes == res && _bound)
			{
				return;
			}
			UnbindFromResources();
			res = newRes;
			if (res != null)
			{
				res.OnMeterChanged += OnMeter;
				_bound = true;
			}
		}
		void UnbindFromResources() {
			if (res != null)
			{
				res.OnMeterChanged -= OnMeter;
			}
			res = null;
			_bound = false;
		}
		void HydrateOnce() {
			var f = isP1 ? round?.p1 : round?.p2;
			if (f != null) { OnMeter(f.meter, f.maxMeter); }
		}
		public void ForceRebind() {
			if (round == null) { round = Systems.RoundManager.Instance; }
			var newRes = round == null ? null : (isP1 ? round.p1Resources : round.p2Resources);
			if (newRes == res && _bound)
			{
				HydrateOnce();
				return;
			}
			UnbindFromResources();
			res = newRes;
			if (res != null)
			{
				res.OnMeterChanged += OnMeter;
				_bound = true;
			}
			HydrateOnce();
		}
		public void SetIsP1(bool value) {
			if (isP1 == value) { return; }
			isP1 = value;
			ForceRebind();
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