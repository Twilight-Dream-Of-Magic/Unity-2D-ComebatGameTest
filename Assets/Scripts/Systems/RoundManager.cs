using UnityEngine;
using UnityEngine.UI;
using Fighter;

namespace Systems {
    /// <summary>
    /// Manages a single round: updates a countdown timer, detects end conditions (KO or time over),
    /// decides the winner text, pauses time, and broadcasts timer/end events.
    /// </summary>
    public class RoundManager : MonoBehaviour {
        public FighterController p1, p2;
        public Slider p1Hp, p2Hp; // legacy references (no longer written)
        public float roundTime = 60f;
        public Text timerText; // legacy optional
        public UI.ResultPanel resultPanel;
        public Text resultText; // legacy optional

        public System.Action<int> OnTimerChanged;     // seconds left
        public System.Action<string> OnRoundEnd;      // result string

        float timeLeft;
        bool ended;
        bool timeout;
        int lastSeconds = -1;

        private void Start() {
            timeLeft = roundTime; Time.timeScale = 1f;
            if (resultText) { resultText.gameObject.SetActive(false); resultText.fontSize = 40; }
            BroadcastTimerIfChanged();
        }
        private void Update() {
            if (!p1 || !p2) { var fighters = FindObjectsOfType<FighterController>(); if (fighters.Length >= 2) { p1 = fighters[0]; p2 = fighters[1]; } }
            if (ended) return;
            timeLeft -= Time.deltaTime;
            BroadcastTimerIfChanged();

            if ((p1 && p1.currentHealth == 0) || (p2 && p2.currentHealth == 0) || timeLeft <= 0) {
                timeout = timeLeft <= 0;
                EndRound();
            }
        }

        void BroadcastTimerIfChanged() {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeLeft));
            if (seconds != lastSeconds) {
                lastSeconds = seconds;
                OnTimerChanged?.Invoke(seconds);
                if (timerText) timerText.text = seconds.ToString();
            }
        }

        private void EndRound() {
            ended = true;
            Time.timeScale = 0f;
            string txt = "Draw";

            if (timeout) {
                txt = "Time Over - Draw";
            } else if (p1 && p2) {
                if (p1.currentHealth == 0 && p2.currentHealth == 0) txt = "Double KO - Draw";
                else if (p1.currentHealth == p2.currentHealth) txt = "Draw";
                else if (p1.currentHealth > p2.currentHealth) txt = "P1 Wins";
                else txt = "P2 Wins";
            }

            OnRoundEnd?.Invoke(txt);
            if (resultText) {
                resultText.text = txt;
                resultText.color = txt.Contains("P1") ? new Color(0.4f,0.8f,1f,1f) : txt.Contains("P2") ? new Color(1f,0.4f,0.4f,1f) : Color.white;
                resultText.gameObject.SetActive(true);
            }
            if (resultPanel && resultPanel.panel) resultPanel.panel.SetActive(true);
        }
    }
}