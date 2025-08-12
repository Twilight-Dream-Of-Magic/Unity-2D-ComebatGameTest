using UnityEngine;
using UnityEngine.UI;
using Fighter;

namespace UI {
    public class StateTextBinder : MonoBehaviour {
        public FighterController fighter;
        public Text text;
        public string format = "{state} {move}";
        public bool showGuardPrompt = true;
        public string guardHint = "";
        void OnEnable() {
            if (!text) text = GetComponent<Text>();
            if (!fighter) fighter = GetComponentInParent<FighterController>();
            if (fighter) fighter.OnStateChanged += OnState;
            InitNow();
        }
        void OnDisable() { if (fighter) fighter.OnStateChanged -= OnState; }
        void InitNow() { OnState(fighter ? fighter.GetCurrentStateName() : "-", fighter?.debugMoveName ?? ""); }
        void OnState(string state, string move) {
            if (!text) return;
            var line = format.Replace("{state}", state ?? "-").Replace("{move}", move ?? "");
            if (showGuardPrompt && fighter && fighter.IsGrounded() && !fighter.PendingCommands.block) {
                if (fighter.IsCrouching) guardHint = "低招请蹲防 (S/Down + Shift)"; else guardHint = "高/中请站防 (Shift)；Overhead 请站防";
                line += "\n" + guardHint;
            }
            text.text = line.Trim();
        }
    }
}