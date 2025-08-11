using UnityEngine;

namespace Systems {
    public enum Difficulty { Easy, Normal, Hard }

    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }
        public Difficulty difficulty = Difficulty.Normal;

        private void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public void SetDifficulty(int idx) { difficulty = (Difficulty)Mathf.Clamp(idx, 0, 2); }
        public void SetMasterVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.masterVolume = v; }
        public void SetBgmVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.bgmVolume = v; }
        public void SetSfxVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.sfxVolume = v; }
    }
}