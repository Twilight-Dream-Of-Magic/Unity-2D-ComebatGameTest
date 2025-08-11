using UnityEngine;
using Combat;
using Data;

namespace Fighter {
    public class ComboHandler : MonoBehaviour {
        public FighterController fighter;
        public InputBuffer inputBuffer;

        float attackElapsed;
        bool inAttack;
        bool contactOccurred; // set by external events in future; here we infer conservatively

        private void Reset() {
            if (fighter == null) fighter = GetComponent<FighterController>();
            if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
        }

        private void Update() {
            if (fighter == null) return;

            if (fighter.AnimatorIsTag("Attack")) {
                if (!inAttack) { inAttack = true; attackElapsed = 0f; contactOccurred = false; }
                attackElapsed += Time.deltaTime;
                TryComboCancel();
            } else {
                inAttack = false; attackElapsed = 0f; contactOccurred = false;
            }
        }

        void TryComboCancel() {
            var md = fighter.CurrentMove;
            if (md == null || inputBuffer == null) return;

            bool allow = false;
            // On whiff window
            if (md.canCancelOnWhiff && attackElapsed >= md.onWhiffCancelWindow.x && attackElapsed <= md.onWhiffCancelWindow.y) allow = true;
            // On hit/block windows are further gated by states; as a fallback we allow if elapsed is within either window
            if (md.canCancelOnHit && attackElapsed >= md.onHitCancelWindow.x && attackElapsed <= md.onHitCancelWindow.y) allow = true;
            if (md.canCancelOnBlock && attackElapsed >= md.onBlockCancelWindow.x && attackElapsed <= md.onBlockCancelWindow.y) allow = true;
            if (!allow) return;

            if (md.cancelIntoTriggers != null && md.cancelIntoTriggers.Length > 0) {
                foreach (var trig in md.cancelIntoTriggers) {
                    if (MatchTrigger(trig)) { fighter.RequestComboCancel(trig); inputBuffer.Clear(); break; }
                }
            } else {
                if (MatchTrigger("Heavy")) { fighter.RequestComboCancel("Heavy"); inputBuffer.Clear(); }
                else if (MatchTrigger("Light")) { fighter.RequestComboCancel("Light"); inputBuffer.Clear(); }
            }
        }

        bool MatchTrigger(string trig) {
            switch (trig) {
                case "Light": return inputBuffer.Match(CommandToken.Light);
                case "Heavy": return inputBuffer.Match(CommandToken.Heavy);
                case "Super": return inputBuffer.Match(CommandToken.Heavy); // SpecialInputResolver sets RequestComboCancel for real supers
                default: return false;
            }
        }
    }
}