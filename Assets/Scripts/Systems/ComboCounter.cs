using UnityEngine;
using System;

namespace Systems {
    public class ComboCounter : MonoBehaviour {
        public static ComboCounter Instance { get; private set; }
        public int currentCount { get; private set; }
        public Fighter.FighterController currentAttacker { get; private set; }
        public float timeoutSeconds = 1.2f;
        float timeLeft;

        void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Fighter.FighterController.OnAnyDamage += OnAnyDamage;
        }
        void OnDestroy() { Fighter.FighterController.OnAnyDamage -= OnAnyDamage; if (Instance==this) Instance=null; }

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
        }

        public void ResetCombo() { currentCount = 0; currentAttacker = null; timeLeft = 0; }
    }
}