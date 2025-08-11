using UnityEngine;
using UnityEngine.UI;
using Fighter;

namespace Systems {
    public class RoundManager : MonoBehaviour {
        public FighterController p1, p2;
        public Slider p1Hp, p2Hp;
        public float roundTime = 60f;
        public Text timerText;
        public UI.ResultPanel resultPanel;
        public Text resultText;
        float timeLeft;
        bool ended;
        bool timeout;

        private void Start() { timeLeft = roundTime; Time.timeScale = 1f; if (resultText) resultText.gameObject.SetActive(false); }
        private void Update() {
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

        private void EndRound() {
            ended = true;
            Time.timeScale = 0f;
            string txt = "Draw";

            if (timeout) {
                txt = "Draw"; // time over -> draw
            } else if (p1 && p2) {
                if (p1.currentHealth == 0 && p2.currentHealth == 0) txt = "Draw";
                else if (p1.currentHealth == p2.currentHealth) txt = "Draw";
                else if (p1.currentHealth > p2.currentHealth) txt = "P1 Wins";
                else txt = "P2 Wins";
            }

            if (resultText) { resultText.text = txt; resultText.gameObject.SetActive(true); }
            if (resultPanel) resultPanel.Show();
        }
    }
}