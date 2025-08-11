using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ControlsHintBinder : MonoBehaviour {
        public Text text;
        public bool useDefault = true;
        [TextArea]
        public string custom;
        private void Start() {
            if (!text) text = GetComponent<Text>();
            if (!text) return;
            text.text = useDefault ? "Move: A/D  Jump: Space/W  Crouch: S  Block: Left Shift  Dodge: L/Ctrl  Light: J  Heavy: K" : custom;
        }
    }
}