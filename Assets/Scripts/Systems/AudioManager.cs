using UnityEngine;
using Framework;

namespace Systems {
    public class AudioManager : MonoSingleton<AudioManager> {
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        [Range(0f,1f)] public float masterVolume = 1f;
        [Range(0f,1f)] public float bgmVolume = 0.7f;
        [Range(0f,1f)] public float sfxVolume = 1f;

        protected override void OnSingletonInit() {
            // nothing for now
        }

        private void Update() {
            if (bgmSource) bgmSource.volume = masterVolume * bgmVolume;
            if (sfxSource) sfxSource.volume = masterVolume * sfxVolume;
        }

        public void PlayBGM(AudioClip clip, bool loop = true) {
            if (!bgmSource || clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip; bgmSource.loop = loop; bgmSource.Play();
        }
        public void PlaySFX(AudioClip clip) {
            if (!sfxSource || clip == null) return;
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
        }
    }
}