using UnityEngine;
using Framework;

namespace Systems {
    public class FrameClock : MonoSingleton<FrameClock> {
        public static int Now => Instance ? Instance.nowFrame : 0;
        int nowFrame;
        protected override void OnSingletonInit() { DontDestroyOnLoad(gameObject); }
        private void FixedUpdate() { nowFrame++; }
        public static int SecondsToFrames(float seconds) { return Mathf.Max(0, Mathf.RoundToInt(seconds / Time.fixedDeltaTime)); }
        public static float FramesToSeconds(int frames) { return frames * Time.fixedDeltaTime; }
    }
}