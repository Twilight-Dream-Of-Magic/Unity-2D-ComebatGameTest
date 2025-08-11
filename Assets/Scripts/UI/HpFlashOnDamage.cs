using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class HpFlashOnDamage : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Slider slider;
        public Color flashColor = new Color(1f, 1f, 1f, 1f);
        public float flashDuration = 0.15f;

        Image fill; Color original;
        float t;

        void Awake() {
            if (slider) fill = slider.fillRect ? slider.fillRect.GetComponent<Image>() : null;
            if (fill) original = fill.color;
            if (fighter) fighter.OnDamaged += HandleDamage;
        }
        void OnDestroy() { if (fighter) fighter.OnDamaged -= HandleDamage; }

        void HandleDamage(Fighter.FighterController _) {
            t = flashDuration;
            if (fill) fill.color = flashColor;
        }

        void Update() {
            if (t > 0) {
                t -= Time.unscaledDeltaTime;
                if (t <= 0 && fill) fill.color = original;
            }
        }
    }
}