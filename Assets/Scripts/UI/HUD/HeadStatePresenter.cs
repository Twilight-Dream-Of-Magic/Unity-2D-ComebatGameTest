using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Screen-space label that follows a fighter's head (for 2D overlay).
	/// </summary>
	public class HeadStatePresenter : MonoBehaviour {
		public bool isP1 = true;
		public FightingGame.Combat.Actors.FighterActor fighter;
		public Transform target; // optional anchor under fighter
		public Text stateText;
		public Vector3 offset = new Vector3(0f, 2.2f, 0f);
		Camera _cam;
		bool _bound;
		void Awake() {
			_cam = Camera.main;
			if (stateText == null)
			{
				stateText = GetComponent<Text>();
			}
			if (stateText != null && string.IsNullOrEmpty(stateText.text)) { stateText.text = "--"; }
		}
		void Start() {
			if (!_bound)
			{
				var round = Systems.RoundManager.Instance;
				var f = isP1 ? (round != null ? round.p1 : null) : (round != null ? round.p2 : null);
				if (f != null && stateText != null)
				{
					// auto find anchor child if exists
					var anchor = f != null ? f.transform.Find("HeadStateAnchor") : null;
					ForceBind(f, stateText, anchor);
				}
			}
		}
		void OnDisable() { Unbind(); }
		public void ForceBind(FightingGame.Combat.Actors.FighterActor f, Text t, Transform anchor = null) {
			Unbind();
			fighter = f;
			target = anchor;
			if (t != null) { stateText = t; }
			if (fighter != null)
			{
				_bound = true;
				fighter.OnStateChanged += OnState;
				if (stateText != null)
				{
					bool p1 = fighter.team == FightingGame.Combat.Actors.FighterTeam.Player;
					stateText.color = p1 ? new Color(0.9f, 0.95f, 1f, 1f) : new Color(1f, 0.85f, 0.85f, 1f);
					if (string.IsNullOrEmpty(stateText.text)) { stateText.text = "--"; }
				}
				OnState(fighter.GetCurrentStateName(), fighter.debugMoveName ?? "");
			}
			else if (stateText != null)
			{
				stateText.text = "--";
			}
		}
		void Unbind() {
			if (_bound && fighter != null)
			{
				fighter.OnStateChanged -= OnState;
			}
			_bound = false;
		}
		void LateUpdate() {
			if (fighter == null || stateText == null) { return; }
			if (_cam == null) { _cam = Camera.main; }
			Transform follow = target != null ? target : fighter.transform;
			Vector3 worldPos = follow.position + (target != null ? Vector3.zero : offset);
			Vector3 screenPos = _cam != null ? _cam.WorldToScreenPoint(worldPos) : worldPos;
			var canvas = stateText.canvas;
			var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
			if (canvasRect != null)
			{
				Vector2 localPos;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam, out localPos);
				stateText.rectTransform.anchoredPosition = localPos;
			}
		}
		void OnState(string state, string move) {
			if (stateText == null) { return; }
			string st = string.IsNullOrEmpty(state) ? "--" : state;
			string mv = string.IsNullOrEmpty(move) ? string.Empty : (" " + move);
			stateText.text = st + mv;
		}
	}
}