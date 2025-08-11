using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class StateTextBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Text text;
        [TextArea]
        public string format = "{team}  {state}  {move}"; // placeholders
        public bool showMoveName = true;
        void Awake() { if (!text) text = GetComponent<Text>(); }
        void Update() {
            if (!fighter || !text) return;
            string team = fighter.team == Fighter.FighterTeam.Player ? "P1" : "P2";
            string state = fighter.StateMachine?.Current?.Name ?? "-";
            string move = showMoveName && !string.IsNullOrEmpty(fighter.debugMoveName) ? fighter.debugMoveName : "";
            string s = format.Replace("{team}", team).Replace("{state}", state).Replace("{move}", move);
            text.text = s.Trim();
        }
    }
}