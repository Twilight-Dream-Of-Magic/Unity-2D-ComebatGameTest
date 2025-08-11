using UnityEngine;
using System;
using Framework;

namespace Systems {
    public class ComboCounter : MonoSingleton<ComboCounter> {
        public int currentCount { get; private set; }
        public Fighter.FighterController currentAttacker { get; private set; }
        public float timeoutSeconds = 1.2f;
        float timeLeft;

        public event Action<int,Fighter.FighterController> OnComboChanged; // (count, attacker)

        protected override void OnSingletonInit() {
            Fighter.FighterController.OnAnyDamage += OnAnyDamage;
        }
        protected override void OnDestroy() {
            Fighter.FighterController.OnAnyDamage -= OnAnyDamage;
            base.OnDestroy();
        }

        void Update() {
            if (currentCount > 0) {
                timeLeft -= Time.unscaledDeltaTime;
                if (timeLeft <= 0) ResetCombo();
            }
        }

        void OnAnyDamage(Fighter.FighterController victim, Fighter.FighterController attacker) {
            if (currentAttacker == attacker) {
                currentCount++;
            } else {
                currentAttacker = attacker; currentCount = 1;
            }
            timeLeft = timeoutSeconds;
            OnComboChanged?.Invoke(currentCount, currentAttacker);
        }

        public void ResetCombo() { currentCount = 0; currentAttacker = null; timeLeft = 0; OnComboChanged?.Invoke(0, null); }
    }
}