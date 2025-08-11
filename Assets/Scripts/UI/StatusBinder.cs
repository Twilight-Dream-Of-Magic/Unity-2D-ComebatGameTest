using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class StatusBinder : MonoBehaviour {
        public Fighter.FighterController fighter;
        public Text text;
        public Vector2 screenOffset = new Vector2(0f, 60f);
        public bool follow = true;
        public Vector2 margin = new Vector2(20f, 20f);
        Canvas rootCanvas; RectTransform canvasRect; RectTransform rt;

        void Awake() {
            if (!text) text = GetComponent<Text>();
            rootCanvas = GetComponentInParent<Canvas>();
            canvasRect = rootCanvas ? rootCanvas.GetComponent<RectTransform>() : null;
            rt = GetComponent<RectTransform>();
        }

        void Update() {
            if (!fighter || !text) return;
            int hp = fighter.currentHealth;
            int meter = fighter.meter;
            text.text = (fighter.team == Fighter.FighterTeam.Player ? "P1" : "P2") + $"\nHP: {hp}  Meter: {meter}";

            if (follow && canvasRect && rt) {
                Vector3 world = fighter.transform.position + new Vector3(0f, 1.4f, 0f);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local)) {
                    Vector2 half = canvasRect.rect.size * 0.5f;
                    Vector2 pos = local + screenOffset;
                    pos.x = Mathf.Clamp(pos.x, -half.x + margin.x, half.x - margin.x);
                    pos.y = Mathf.Clamp(pos.y, -half.y + margin.y, half.y - margin.y);
                    rt.anchoredPosition = pos;
                }
            }
        }
    }
}