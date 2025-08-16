using UnityEngine;
using Framework;

namespace Systems {
    /// <summary>
    /// Central audio routing for BGM and SFX with master/bgm/sfx volume controls.
    /// 音频中心：统一管理 BGM 与音效，并提供主音量与子通道音量控制。
    /// </summary>
    public class AudioManager : MonoSingleton<AudioManager> {
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        [Range(0f,1f)] public float masterVolume = 1f;
        [Range(0f,1f)] public float bgmVolume = 0.7f;
        [Range(0f,1f)] public float sfxVolume = 1f;

        protected override void DoAwake() { }

        protected override void DoUpdate() {
            if (bgmSource) bgmSource.volume = masterVolume * bgmVolume;
            if (sfxSource) sfxSource.volume = masterVolume * sfxVolume;
        }

        /// <summary>Play BGM with optional loop. 播放 BGM（可循环）。</summary>
        public void PlayBGM(AudioClip clip, bool loop = true) {
            if (!bgmSource || clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip; bgmSource.loop = loop; bgmSource.Play();
        }
        /// <summary>Play one-shot SFX respecting volumes. 播放一次性音效（受音量控制）。</summary>
        public void PlaySFX(AudioClip clip) {
            if (!sfxSource || clip == null) return;
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
        }
    }
}