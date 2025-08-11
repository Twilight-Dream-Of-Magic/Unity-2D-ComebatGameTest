using UnityEngine;
using UnityEngine.UI;

namespace UI {
    /// <summary>
    /// Binds a Text component to display the fighter's current HP as a number.
    /// </summary>
    public class HpTextBinder : MonoBehaviour {
        /// <summary>Fighter to read HP from.</summary>
        public Fighter.FighterController fighter;
        /// <summary>Target Text to write into.</summary>
        public Text text;
        private void Update() {
            if (!fighter || !text) return;
            int max = fighter.stats ? fighter.stats.maxHealth : 100;
            text.text = fighter.currentHealth.ToString();
        }
    }
}