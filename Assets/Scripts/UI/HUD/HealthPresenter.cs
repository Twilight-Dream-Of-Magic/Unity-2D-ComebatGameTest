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

		void Awake() {
			if (hpText != null && string.IsNullOrEmpty(hpText.text))
			{
				hpText.text = "HP --/--";
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
			if (res != null) { res.OnHealthChanged += OnHp; }
		}
		void UnbindFromResources() {
			if (res != null) { res.OnHealthChanged -= OnHp; }
			res = null;
		}
		void HydrateOnce() {
			var f = isP1 ? round?.p1 : round?.p2;
			if (f != null) { OnHp(f.currentHealth, f.stats != null ? f.stats.maxHealth : 100); }
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