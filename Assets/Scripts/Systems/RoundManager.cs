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
        float timeLeft;
        bool ended;

        private void Start() { timeLeft = roundTime; Time.timeScale = 1f; }
        private void Update() {
            if (ended) return;
            timeLeft -= Time.deltaTime;
            if (p1Hp) p1Hp.value = p1 && p1.stats ? (float)p1.currentHealth / p1.stats.maxHealth : 0f;
            if (p2Hp) p2Hp.value = p2 && p2.stats ? (float)p2.currentHealth / p2.stats.maxHealth : 0f;
            if (timerText) timerText.text = Mathf.CeilToInt(Mathf.Max(0, timeLeft)).ToString();

            if ((p1 && p1.currentHealth == 0) || (p2 && p2.currentHealth == 0) || timeLeft <= 0) {
                EndRound();
            }
        }

        private void EndRound() {
            ended = true;
            Time.timeScale = 0f;
            if (resultPanel) resultPanel.Show();
        }
    }
}