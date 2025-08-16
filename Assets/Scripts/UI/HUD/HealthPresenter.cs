using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Presenter that subscribes P1/P2 FighterResources events directly from RoundManager references.
	/// </summary>
	public class HealthPresenter : MonoBehaviour {
		public Systems.RoundManager round;
		public bool isP1 = true;
		public HealthBarView bar;
		public Text hpText;

		Fighter.Core.FighterResources res;
		bool _bound;

		void Awake() {
			if (hpText != null && string.IsNullOrEmpty(hpText.text))
			{
				hpText.text = "HP --/--";
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
				res.OnHealthChanged += OnHp;
				_bound = true;
			}
		}
		void UnbindFromResources() {
			if (res != null)
			{
				res.OnHealthChanged -= OnHp;
			}
			res = null;
			_bound = false;
		}
		void HydrateOnce() {
			var f = isP1 ? round?.p1 : round?.p2;
			if (f != null) { OnHp(f.currentHealth, f.stats != null ? f.stats.maxHealth : 100); }
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
				res.OnHealthChanged += OnHp;
				_bound = true;
			}
			HydrateOnce();
		}
		public void SetIsP1(bool value) {
			if (isP1 == value) { return; }
			isP1 = value;
			ForceRebind();
		}
		void OnHp(int current, int max) {
			if (bar != null)
			{
				bar.SetMax(max);
				bar.SetValue(current);
			}
			if (hpText != null)
			{
				hpText.text = "HP " + current + "/" + max;
			}
		}
	}
}