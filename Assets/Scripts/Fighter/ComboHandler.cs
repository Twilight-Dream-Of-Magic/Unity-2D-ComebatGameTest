using UnityEngine;
using Combat;
using Data;

namespace Fighter {
    public class ComboHandler : MonoBehaviour {
        public FighterController fighter;
        public InputBuffer inputBuffer;
        public ComboDefinition[] combos;

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
            if (combos == null || inputBuffer == null) return;
            foreach (var c in combos) {
                if (c == null) continue;
                if (attackElapsed < c.cancelWindowStart || attackElapsed > c.cancelWindowEnd) continue;
                if (inputBuffer.Match(c.sequence)) {
                    fighter.RequestComboCancel(c.attackTrigger);
                    inputBuffer.Clear();
                    break;
                }
            }
        }
    }
}