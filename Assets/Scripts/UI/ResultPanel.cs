using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI {
    public class ResultPanel : MonoBehaviour {
        public GameObject panel;
        public string menuSceneName = "MainMenu";
        private void Start() { if (panel) panel.SetActive(false); }
        public void Show() { if (panel) panel.SetActive(true); }
        public void Restart() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void BackToMenu() { Time.timeScale = 1f; SceneManager.LoadScene(menuSceneName); }
    }
}