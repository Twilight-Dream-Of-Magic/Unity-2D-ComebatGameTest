using UnityEngine;
using UnityEngine.UI;
using Combat;
using Systems;

namespace UI {
    /// <summary>
    /// Lightweight on-screen debug display for fighter states and simple input echoing.
    /// In compact mode shows current FSM state and active move; in detailed mode shows frame data and advantage.
    /// </summary>
    public class DebugHUD : MonoBehaviour {
        /// <summary>Legacy single fighter reference; use fighterP1/fighterP2 for dual display.</summary>
        public Fighter.FighterController fighter; // legacy single
        /// <summary>Player 1 fighter used for dual-line status.</summary>
        public Fighter.FighterController fighterP1;
        /// <summary>Player 2 fighter used for dual-line status.</summary>
        public Fighter.FighterController fighterP2;
        /// <summary>Optional input buffer to display recent tokens (currently minimal).</summary>
        public InputBuffer inputBuffer;
        /// <summary>Primary text element to print fighter states.</summary>
        public Text stateText;
        /// <summary>Optional text for input tokens.</summary>
        public Text inputText;
        [Header("Display")]
        /// <summary>When true, show frame data details (startup/active/recovery/advantage).</summary>
        public bool showDetails = false; // compact by default

        private void Reset() {
            if (fighter == null) fighter = FindObjectOfType<Fighter.FighterController>();
            if (inputBuffer == null && fighter != null) inputBuffer = fighter.GetComponent<InputBuffer>();
        }

        private void Update() {
            if (!stateText) return;
            var p1 = fighterP1 != null ? fighterP1 : fighter;
            var p2 = fighterP2;

            string BuildLine(Fighter.FighterController f) {
                if (f == null) return "-";
                var st = f.StateMachine?.Current?.Name ?? "None";
                if (!showDetails) {
                    string moveLine = f.CurrentMove != null ? $"{f.CurrentMove.triggerName} A:{f.debugHitActive}" : "";
                    return $"{(f.team==Fighter.FighterTeam.Player?"P1":"P2")}  {st}  {moveLine}";
                } else {
                    string moveLine = "";
                    if (f.CurrentMove != null) {
                        int advHit = FrameClock.SecondsToFrames(Mathf.Max(0f, f.CurrentMove.hitstun - f.CurrentMove.recovery));
                        int advBlk = FrameClock.SecondsToFrames(Mathf.Max(0f, f.CurrentMove.blockstun - f.CurrentMove.recovery));
                        moveLine = $" | {f.CurrentMove.triggerName} A:{f.debugHitActive} st:{FrameClock.SecondsToFrames(f.CurrentMove.startup)} a:{FrameClock.SecondsToFrames(f.CurrentMove.active)} r:{FrameClock.SecondsToFrames(f.CurrentMove.recovery)} advH:{advHit} advB:{advBlk} HP:{f.currentHealth} M:{f.meter}";
                    }
                    return $"{(f.team==Fighter.FighterTeam.Player?"P1":"P2")} {st}{moveLine}";
                }
            }

            if (p1 && p2) stateText.text = BuildLine(p1) + "\n" + BuildLine(p2);
            else if (p1) stateText.text = BuildLine(p1);

            if (inputText && inputBuffer) {
                inputText.text = "";
            }
        }
    }
}