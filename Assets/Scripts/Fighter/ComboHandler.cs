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

            bool allow = false;
            if (md.canCancelOnWhiff && attackElapsed >= md.onWhiffCancelWindow.x && attackElapsed <= md.onWhiffCancelWindow.y) allow = true;
            if (md.canCancelOnHit && attackElapsed >= md.onHitCancelWindow.x && attackElapsed <= md.onHitCancelWindow.y) allow = true;
            if (md.canCancelOnBlock && attackElapsed >= md.onBlockCancelWindow.x && attackElapsed <= md.onBlockCancelWindow.y) allow = true;
            if (!allow) return;

            var cq = fighter.GetComponent<CommandQueue>();
            if (!cq) return;
            if (md.cancelIntoTriggers != null && md.cancelIntoTriggers.Length > 0) {
                foreach (var trig in md.cancelIntoTriggers) {
                    if (MatchTrigger(trig)) { cq.EnqueueCombo(trig == "Light" ? CommandToken.Light : CommandToken.Heavy); inputBuffer.Clear(); break; }
                }
            } else {
                if (MatchTrigger("Heavy")) { cq.EnqueueCombo(CommandToken.Heavy); inputBuffer.Clear(); }
                else if (MatchTrigger("Light")) { cq.EnqueueCombo(CommandToken.Light); inputBuffer.Clear(); }
            }
        }

        bool MatchTrigger(string trig) {
            switch (trig) {
                case "Light": return inputBuffer.Match(CommandToken.Light);
                case "Heavy": return inputBuffer.Match(CommandToken.Heavy);
                case "Super": return inputBuffer.Match(CommandToken.Heavy);
                default: return false;
            }
        }
    }
}