using UnityEngine;

namespace UI.HUD {
	/// <summary>
	/// World-space floating text billboard.
	/// - Positions at anchor (or fighter.transform + offset)
	/// - Faces camera each frame
	/// - Exposes SetText/SetColor for external data binding
	/// </summary>
	public class WorldStateBillboard : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public Transform anchor; // optional; fall back to fighter.transform + offset if null
		public Vector3 offset = new Vector3(0f, 1.2f, 0f);

		TextMesh _text;
		Camera _cam;

		void Awake() {
			_text = GetComponent<TextMesh>();
			if (_text == null) { _text = gameObject.AddComponent<TextMesh>(); }
			if (string.IsNullOrEmpty(_text.text)) { _text.text = "--"; }
			_text.anchor = TextAnchor.MiddleCenter;
			_text.alignment = TextAlignment.Center;
			_text.fontSize = 48;
			_text.characterSize = 0.06f;
			_cam = Camera.main;
			if (fighter == null) { fighter = GetComponentInParent<FightingGame.Combat.Actors.FighterActor>(); }
		}
		void LateUpdate() {
			if (_cam == null) { _cam = Camera.main; }
			UpdateTransformOnly();
		}
		public void SetText(string value) {
			if (_text == null) { return; }
			_text.text = string.IsNullOrEmpty(value) ? "--" : value;
		}
		public void SetColor(Color value) {
			if (_text == null) { return; }
			_text.color = value;
		}
		void UpdateTransformOnly() {
			if (_text == null) { return; }
			Transform follow = (anchor != null) ? anchor : (fighter != null ? fighter.transform : null);
			if (follow == null) { return; }
			Vector3 worldPos = follow.position + (anchor != null ? Vector3.zero : offset);
			transform.position = worldPos;
			if (_cam != null)
			{
				transform.rotation = Quaternion.LookRotation(_cam.transform.forward, Vector3.up);
			}
			// Fix mirrored text when parent has negative X scale
			var ls = transform.localScale;
			float sx = Mathf.Abs(ls.x) > 0f ? Mathf.Abs(ls.x) : 1f;
			float parentSign = (follow.lossyScale.x < 0f) ? -1f : 1f;
			ls.x = parentSign * sx;
			transform.localScale = ls;
		}
	}
}