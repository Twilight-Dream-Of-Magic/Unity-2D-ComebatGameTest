using UnityEngine;
using UnityEngine.UI;
using Fighter;
using Combat;

namespace UI {
    public class StateTextBinder : MonoBehaviour {
        public FighterController fighter;
        public Text text;
        public string format = "{state} {move}";
        public bool showGuardPrompt = true;
        public string guardHint = "";
        void Awake() { if (!text) text = GetComponent<Text>(); }
        void Update() {
            if (!fighter || !text) return;
            string state = fighter.StateMachine?.Current?.Name ?? "-";
            string move = fighter.debugMoveName ?? "";
            var line = format.Replace("{state}", state).Replace("{move}", move);
            if (showGuardPrompt && fighter.IsGrounded()) {
                // 简单基于姿态给提示：站立提示防高/中，蹲下提示防低；空中不提示
                if (!fighter.PendingCommands.block) {
                    if (fighter.IsCrouching) guardHint = "低招请蹲防 (S/Down + Shift)";
                    else guardHint = "高/中请站防 (Shift)；Overhead 请站防";
                    line += "\n" + guardHint;
                }
            }
            text.text = line.Trim();
        }
    }
}