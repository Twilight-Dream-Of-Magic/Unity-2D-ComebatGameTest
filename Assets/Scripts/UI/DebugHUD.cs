using UnityEngine;
using UnityEngine.UI;
using Combat;
using Systems;

namespace UI {
    public class DebugHUD : MonoBehaviour {
        public Fighter.FighterController fighter;
        public InputBuffer inputBuffer;
        public Text stateText;
        public Text inputText;

        private void Reset() {
            if (fighter == null) fighter = FindObjectOfType<Fighter.FighterController>();
            if (inputBuffer == null && fighter != null) inputBuffer = fighter.GetComponent<InputBuffer>();
        }

        private void Update() {
            if (fighter && stateText) {
                var st = fighter.StateMachine?.Current?.Name ?? "None";
                string moveLine = "";
                if (fighter.CurrentMove != null) {
                    // Estimate advantage in frames: hitstun/blockstun - recovery
                    int advHit = FrameClock.SecondsToFrames(Mathf.Max(0f, fighter.CurrentMove.hitstun - fighter.CurrentMove.recovery));
                    int advBlk = FrameClock.SecondsToFrames(Mathf.Max(0f, fighter.CurrentMove.blockstun - fighter.CurrentMove.recovery));
                    // Phase info comes from FighterController public fields (if any), otherwise show durations
                    moveLine = $"\nMove: {fighter.CurrentMove.moveId} ({fighter.CurrentMove.triggerName})\n  startup:{FrameClock.SecondsToFrames(fighter.CurrentMove.startup)}f active:{FrameClock.SecondsToFrames(fighter.CurrentMove.active)}f rec:{FrameClock.SecondsToFrames(fighter.CurrentMove.recovery)}f\n  adv(Hit):{advHit}f  adv(Block):{advBlk}f";
                }
                stateText.text = $"State: {st}{moveLine}\nHP: {fighter.currentHealth}  Meter: {fighter.meter}";
            }
            if (inputText && inputBuffer) {
                inputText.text = "Inputs: (last 0.4s)"; // placeholder; can be extended to list tokens
            }
        }
    }
}