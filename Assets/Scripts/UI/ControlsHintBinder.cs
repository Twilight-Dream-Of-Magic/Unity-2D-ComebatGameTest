using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ControlsHintBinder : MonoBehaviour {
        public Text text;
        public bool useDefault = true;
        [TextArea]
        public string custom;
        [Header("Dynamic Tips")]
        public string wakeupTip = "Hold Back to backrise, Hold Forward to forward roll";
        public string techTip = "Press J near enemy to Throw; press J again to Tech";
        public bool showWakeupTip;
        public bool showTechTip;
        private void Start() {
            if (!text) text = GetComponent<Text>();
            if (!text) return;
            var baseLine = useDefault ? "Move: A/D or Arrow Keys  Jump: Space/W/Up  Crouch: S/Down  Block: Left Shift  Dodge: L/Ctrl  Light: J  Heavy: K" : custom;
            if (showWakeupTip) baseLine += "\n" + wakeupTip;
            if (showTechTip) baseLine += "\n" + techTip;
            text.text = baseLine;
        }
    }
}