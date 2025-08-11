using UnityEngine;
using Combat;
using Data;

namespace Fighter {
    public class ComboHandler : MonoBehaviour {
        public FighterController fighter;
        public InputBuffer inputBuffer;

        float attackElapsed;
        bool inAttack;

        private void Reset() {
            if (fighter == null) fighter = GetComponent<FighterController>();
            if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
        }

        private void Update() {
            if (fighter == null) return;

            if (fighter.AnimatorIsTag("Attack")) {
                if (!inAttack) { inAttack = true; attackElapsed = 0f; }
                attackElapsed += Time.deltaTime;
                TryComboCancel();
            } else {
                inAttack = false; attackElapsed = 0f;
            }
        }

        void TryComboCancel() {
            var md = fighter.CurrentMove;
            if (md == null || inputBuffer == null) return;
            // allow cancel only when rules permit. For simplicity, we allow during "Active/Recovery" as before;
            // fine-grained windows could be added later.
            bool canCancel = md.canCancelOnHit || md.canCancelOnBlock; // conservative; real logic governed by hit/block event
            if (!canCancel) return;
            // check input buffer for allowed next triggers
            if (md.cancelIntoTriggers != null && md.cancelIntoTriggers.Length > 0) {
                foreach (var trig in md.cancelIntoTriggers) {
                    if (MatchTrigger(trig)) { fighter.RequestComboCancel(trig); inputBuffer.Clear(); break; }
                }
            } else {
                // default: prefer Heavy then Light
                if (MatchTrigger("Heavy")) { fighter.RequestComboCancel("Heavy"); inputBuffer.Clear(); }
                else if (MatchTrigger("Light")) { fighter.RequestComboCancel("Light"); inputBuffer.Clear(); }
            }
        }

        bool MatchTrigger(string trig) {
            switch (trig) {
                case "Light": return inputBuffer.Match(CommandToken.Light);
                case "Heavy": return inputBuffer.Match(CommandToken.Heavy);
                case "Super": return inputBuffer.Match(CommandToken.Heavy); // Super is matched by SpecialInputResolver setting RequestComboCancel
                default: return false;
            }
        }
    }
}