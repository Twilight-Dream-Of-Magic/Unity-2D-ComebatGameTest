using UnityEngine;
using UnityEngine.UI;

namespace UI {
    /// <summary>
    /// Briefly flashes the HP slider fill color when the bound fighter actually loses HP (not chip-only).
    /// Subscribes to FighterController.OnDamaged.
    /// </summary>
    public class HpFlashOnDamage : MonoBehaviour {
        /// <summary>Fighter to observe for damage events.</summary>
        public Fighter.FighterController fighter;
        /// <summary>Slider whose fill will flash on damage.</summary>
        public Slider slider;
        /// <summary>Flash color.</summary>
        public Color flashColor = new Color(1f, 1f, 1f, 1f);
        /// <summary>Duration in seconds.</summary>
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