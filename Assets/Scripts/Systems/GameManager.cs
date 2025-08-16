using UnityEngine;
using Framework;

namespace Systems {
    /// <summary>Difficulty levels. 难度枚举。</summary>
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>
    /// Simple game-wide settings holder for difficulty and audio volume proxies.
    /// 全局设置管理：难度与音量（转发到 AudioManager）。
    /// </summary>
    public class GameManager : MonoSingleton<GameManager> {
        public Difficulty difficulty = Difficulty.Normal;

        protected override void DoAwake() {}

        /// <summary>Set difficulty by index (0..2). 通过索引设置难度（0..2）。</summary>
        public void SetDifficulty(int idx) { difficulty = (Difficulty)Mathf.Clamp(idx, 0, 2); }
        /// <summary>Set master volume if AudioManager exists. 若存在则设置总音量。</summary>
        public void SetMasterVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.masterVolume = v; }
        /// <summary>Set bgm volume if AudioManager exists. 若存在则设置 BGM 音量。</summary>
        public void SetBgmVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.bgmVolume = v; }
        /// <summary>Set sfx volume if AudioManager exists. 若存在则设置音效音量。</summary>
        public void SetSfxVolume(float v) { if (AudioManager.Instance) AudioManager.Instance.sfxVolume = v; }
    }
}