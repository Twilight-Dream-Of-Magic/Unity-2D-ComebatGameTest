using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class HpBarBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Slider slider;
        Fighter.Core.FighterResources res;
        void OnEnable() {
            if (!fighter) fighter = GetComponentInParent<Fighter.FighterController>();
            if (!slider) slider = GetComponent<Slider>();
            res = fighter ? fighter.GetComponent<Fighter.Core.FighterResources>() : null;
            if (res != null) res.OnHealthChanged += OnHealthChanged;
            InitNow();
        }
        void OnDisable() { if (res != null) res.OnHealthChanged -= OnHealthChanged; }
        void InitNow() {
            if (!fighter || !slider) return;
            int maxHp = fighter.stats ? fighter.stats.maxHealth : 100;
            slider.value = maxHp > 0 ? (float)fighter.currentHealth / maxHp : 0f;
        }
        void OnHealthChanged(int current, int max) { if (slider) slider.value = max > 0 ? (float)current / max : 0f; }
    }
}