using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class HpTextBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Text text;
        private void Update() {
            if (!fighter || !text) return;
            int max = fighter.stats ? fighter.stats.maxHealth : 100;
            text.text = fighter.currentHealth.ToString();
        }
    }
}