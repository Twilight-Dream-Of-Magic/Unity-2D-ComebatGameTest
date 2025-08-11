using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI {
    public class MainMenuController : MonoBehaviour {
        public string battleSceneName = "Battle";
        public void StartGame() { SceneManager.LoadScene(battleSceneName); Time.timeScale = 1f; }
        public void Quit() { Application.Quit(); }
    }
}