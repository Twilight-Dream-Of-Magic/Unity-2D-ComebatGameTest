using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class EnergyBarBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Slider slider;
        private void Update() {
            if (!fighter || !slider) return;
            slider.value = fighter.maxMeter > 0 ? (float)fighter.meter / fighter.maxMeter : 0f;
        }
    }
}