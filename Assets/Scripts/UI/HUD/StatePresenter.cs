using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Presenter for current state/move text.
	/// </summary>
	public class StatePresenter : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public Text stateText;

		void Awake() {
			if (stateText != null && string.IsNullOrEmpty(stateText.text))
			{
				stateText.text = "State --";
			}
		}
		void OnEnable() {
			if (fighter == null && Systems.RoundManager.Instance != null)
			{
				fighter = Systems.RoundManager.Instance.p1;
			}
			if (fighter != null)
			{
				fighter.OnStateChanged += OnState;
			}
			Init();
		}
		void OnDisable() {
			if (fighter != null)
			{
				fighter.OnStateChanged -= OnState;
			}
		}
		void Init() {
			if (fighter == null || stateText == null)
			{
				return;
			}
			OnState(fighter.GetCurrentStateName(), fighter.debugMoveName ?? "");
		}
		void OnState(string state, string move) {
			if (stateText == null)
			{
				return;
			}
			string movePart = string.IsNullOrEmpty(move) ? string.Empty : (" " + move);
			stateText.text = state + movePart;
		}
	}
}