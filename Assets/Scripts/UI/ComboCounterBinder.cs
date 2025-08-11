using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ComboCounterBinder : MonoBehaviour {
        public Text text;
        void OnEnable() {
            if (!text) text = GetComponent<Text>();
            if (Systems.ComboCounter.Instance) Systems.ComboCounter.Instance.OnComboChanged += OnCombo;
            InitNow();
        }
        void OnDisable() { if (Systems.ComboCounter.Instance) Systems.ComboCounter.Instance.OnComboChanged -= OnCombo; }
        void InitNow() { OnCombo(0, null); }
        void OnCombo(int count, Fighter.FighterController attacker) {
            if (!text) return;
            if (count <= 0) { text.text = ""; return; }
            text.text = attacker != null && attacker.team == Fighter.FighterTeam.Player ? $"P1 {count} HIT" : $"P2 {count} HIT";
        }
    }
}