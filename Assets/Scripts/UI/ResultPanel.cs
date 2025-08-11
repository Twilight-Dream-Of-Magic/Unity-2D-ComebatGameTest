using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI {
    public class ResultPanel : MonoBehaviour {
        public GameObject panel;
        public string menuSceneName = "MainMenu";
        public UnityEngine.UI.Text resultText;
        public Systems.RoundManager round;
        private void Start() {
            if (panel) panel.SetActive(false);
            if (!round) round = FindObjectOfType<Systems.RoundManager>();
            if (round) round.OnRoundEnd += OnRoundEnd;
        }
        private void OnDestroy() { if (round) round.OnRoundEnd -= OnRoundEnd; }
        void OnRoundEnd(string txt) {
            if (panel) panel.SetActive(true);
            if (resultText) { resultText.text = txt; resultText.color = txt.Contains("P1") ? new Color(0.4f,0.8f,1f,1f) : txt.Contains("P2") ? new Color(1f,0.4f,0.4f,1f) : Color.white; }
        }
        public void Restart() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void BackToMenu() { Time.timeScale = 1f; SceneManager.LoadScene(menuSceneName); }
    }
}