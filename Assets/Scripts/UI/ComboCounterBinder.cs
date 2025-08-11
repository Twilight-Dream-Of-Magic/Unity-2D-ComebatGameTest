using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ComboCounterBinder : MonoBehaviour {
        public Text text;
        public Color playerColor = new Color(0.2f,0.6f,1f,1f);
        public Color aiColor = new Color(1f,0.35f,0.35f,1f);
        void Update() {
            var inst = Systems.ComboCounter.Instance; if (!inst || !text) return;
            int n = inst.currentCount;
            if (n <= 1) { text.text = ""; return; }
            bool player = inst.currentAttacker && inst.currentAttacker.team == Fighter.FighterTeam.Player;
            text.color = player ? playerColor : aiColor;
            text.text = n.ToString() + " HIT";
        }
    }
}