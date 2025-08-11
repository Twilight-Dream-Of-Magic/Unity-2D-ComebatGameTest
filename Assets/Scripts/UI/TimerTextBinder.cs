using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class TimerTextBinder : MonoBehaviour {
        public Systems.RoundManager round;
        public Text text;
        void OnEnable() {
            if (!text) text = GetComponent<Text>();
            if (!round) round = FindObjectOfType<Systems.RoundManager>();
            if (round) round.OnTimerChanged += OnTimerChanged;
            if (round && text) text.text = Mathf.CeilToInt(roundTime()).ToString();
        }
        void OnDisable() { if (round) round.OnTimerChanged -= OnTimerChanged; }
        void OnTimerChanged(int seconds) { if (text) text.text = seconds.ToString(); }
        float roundTime() { return round ? round.GetType().GetField("roundTime", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public).GetValue(round) is float f ? f : 60f : 60f; }
    }
}