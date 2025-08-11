using UnityEngine;
using UnityEngine.UI;
using Fighter;

namespace Systems {
    /// <summary>
    /// Manages a single round: syncs HP bars, updates a countdown timer, detects end conditions (KO or time over),
    /// decides the winner text, pauses time, and shows result UI. Exposes references for HUD binding.
    /// </summary>
    public class RoundManager : MonoBehaviour {
        /// <summary>Player 1 and Player 2 fighter references.</summary>
        public FighterController p1, p2;
        /// <summary>HP sliders bound to P1/P2 current health.</summary>
        public Slider p1Hp, p2Hp;
        /// <summary>Round duration in seconds.</summary>
        public float roundTime = 60f;
        /// <summary>Timer text UI to display remaining time.</summary>
        public Text timerText;
        /// <summary>Optional result panel controller (restart/back to menu).</summary>
        public UI.ResultPanel resultPanel;
        /// <summary>Winner/draw text element.</summary>
        public Text resultText;
        float timeLeft;
        bool ended;
        bool timeout;

        private void Start() { timeLeft = roundTime; Time.timeScale = 1f; if (resultText) { resultText.gameObject.SetActive(false); resultText.fontSize = 40; } }
        private void Update() {
            if (!p1 || !p2) { var fighters = FindObjectsOfType<FighterController>(); if (fighters.Length >= 2) { p1 = fighters[0]; p2 = fighters[1]; } }
            if (!p1Hp || !p2Hp) { var sliders = FindObjectsOfType<Slider>(); if (sliders.Length >= 2) { p1Hp = sliders[0]; p2Hp = sliders[1]; } }
            if (ended) return;
            timeLeft -= Time.deltaTime;
            if (p1Hp) p1Hp.value = p1 && p1.stats ? (float)p1.currentHealth / p1.stats.maxHealth : 0f;
            if (p2Hp) p2Hp.value = p2 && p2.stats ? (float)p2.currentHealth / p2.stats.maxHealth : 0f;
            if (timerText) timerText.text = Mathf.CeilToInt(Mathf.Max(0, timeLeft)).ToString();

            if ((p1 && p1.currentHealth == 0) || (p2 && p2.currentHealth == 0) || timeLeft <= 0) {
                timeout = timeLeft <= 0;
                EndRound();
            }
        }

        /// <summary>
        /// Pause time, compute winner/draw text, and reveal result UI overlay.
        /// </summary>
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

            if (resultText) {
                resultText.text = txt;
                resultText.color = txt.Contains("P1") ? new Color(0.4f,0.8f,1f,1f) : txt.Contains("P2") ? new Color(1f,0.4f,0.4f,1f) : Color.white;
                resultText.gameObject.SetActive(true);
            }
            if (resultPanel) resultPanel.Show();
        }
    }
}