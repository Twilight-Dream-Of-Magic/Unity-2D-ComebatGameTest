using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class MeterTextBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Text text;
        Fighter.Core.FighterResources res;
        void OnEnable() {
            if (!text) text = GetComponent<Text>();
            if (!fighter) fighter = GetComponentInParent<Fighter.FighterController>();
            res = fighter ? fighter.GetComponent<Fighter.Core.FighterResources>() : null;
            if (res != null) res.OnMeterChanged += OnMeterChanged;
            InitNow();
        }
        void OnDisable() { if (res != null) res.OnMeterChanged -= OnMeterChanged; }
        void InitNow() { if (text && fighter) text.text = fighter.meter + "/" + fighter.maxMeter; }
        void OnMeterChanged(int current, int max) { if (text) text.text = current + "/" + max; }
    }
}