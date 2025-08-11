using UnityEngine;

namespace Systems {
    public class FrameClock : MonoBehaviour {
        public static FrameClock Instance { get; private set; }
        public static int Now => Instance ? Instance.nowFrame : 0;
        int nowFrame;
        private void Awake() { if (Instance != null) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); }
        private void FixedUpdate() { nowFrame++; }
        public static int SecondsToFrames(float seconds) { return Mathf.Max(0, Mathf.RoundToInt(seconds / Time.fixedDeltaTime)); }
        public static float FramesToSeconds(int frames) { return frames * Time.fixedDeltaTime; }
    }
}