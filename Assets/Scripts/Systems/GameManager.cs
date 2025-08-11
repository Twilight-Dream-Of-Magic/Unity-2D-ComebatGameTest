using UnityEngine;
using Framework;

namespace Systems {
    public enum Difficulty { Easy, Normal, Hard }

    public class GameManager : MonoSingleton<GameManager> {
        public Difficulty difficulty = Difficulty.Normal;

        protected override void OnSingletonInit() {}

        public void SetDifficulty(int idx) { difficulty = (Difficulty)Mathf.Clamp(idx, 0, 2); }
        public void SetMasterVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.masterVolume = v; }
        public void SetBgmVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.bgmVolume = v; }
        public void SetSfxVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.sfxVolume = v; }
    }
}