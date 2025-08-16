using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI {
    /// <summary>
    /// Minimal main menu controller to start battle or quit application.
    /// 简约主菜单控制器：开始战斗或退出应用。
    /// </summary>
    public class MainMenuController : MonoBehaviour {
        public string battleSceneName = "Battle";
        /// <summary>Load battle scene and reset time scale. 载入战斗场景并重置时间缩放。</summary>
        public void StartGame() { SceneManager.LoadScene(battleSceneName); Time.timeScale = 1f; }
        /// <summary>Quit application. 退出应用。</summary>
        public void Quit() { Application.Quit(); }
    }
}