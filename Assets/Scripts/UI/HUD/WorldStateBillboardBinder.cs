using UnityEngine;

namespace UI.HUD {
	/// <summary>
	/// Binds a WorldStateBillboard to a FighterActor's state events.
	/// Handles subscribe/unsubscribe lifecycle safely.
	/// </summary>
	public class WorldStateBillboardBinder : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public WorldStateBillboard billboard;
		public string labelPrefix = ""; // e.g., "P1" / "AI"
		public Color labelColor = Color.white;

		void Awake() {
			if (billboard == null) { billboard = GetComponent<WorldStateBillboard>(); }
			if (fighter == null) { fighter = GetComponentInParent<FightingGame.Combat.Actors.FighterActor>(); }
		}
		void OnEnable() {
			if (billboard != null)
			{
				billboard.SetColor(labelColor);
			}
			if (fighter != null)
			{
				fighter.OnStateChanged += OnStateChanged;
			}
			// 初始一次
			OnStateChanged(fighter != null ? fighter.GetCurrentStateName() : "--", fighter != null ? fighter.debugMoveName : string.Empty);
		}
		void OnDisable() {
			if (fighter != null)
			{
				fighter.OnStateChanged -= OnStateChanged;
			}
		}
		void OnStateChanged(string state, string move) {
			if (billboard == null)
			{
				return;
			}
			string st = string.IsNullOrEmpty(state) ? "--" : state;
			string mv = string.IsNullOrEmpty(move) ? string.Empty : (" " + move);
			string prefix = string.IsNullOrEmpty(labelPrefix) ? string.Empty : (labelPrefix + ": ");
			billboard.SetText(prefix + st + mv);
		}
	}
}