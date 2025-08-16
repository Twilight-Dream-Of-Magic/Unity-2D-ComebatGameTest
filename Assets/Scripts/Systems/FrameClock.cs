using UnityEngine;
using Framework;

namespace Systems {
    /// <summary>
    /// FixedUpdate-based frame clock and conversions. Provides a global frame counter (unscaled).
    /// 基于 FixedUpdate 的帧时钟与换算，提供全局帧计数（不受缩放）。
    /// </summary>
    public class FrameClock : MonoSingleton<FrameClock> {
        /// <summary>Current frame number. 当前帧数。</summary>
        public static int Now => Instance ? Instance.nowFrame : 0;
        int nowFrame;
        protected override void DoAwake() { DontDestroyOnLoad(gameObject); }
        protected override void DoFixedUpdate() { nowFrame++; }
        /// <summary>Seconds to frames for current fixedDeltaTime. 将秒换算为帧。</summary>
        public static int SecondsToFrames(float seconds) { return Mathf.Max(0, Mathf.RoundToInt(seconds / Time.fixedDeltaTime)); }
        /// <summary>Frames to seconds for current fixedDeltaTime. 将帧换算为秒。</summary>
        public static float FramesToSeconds(int frames) { return frames * Time.fixedDeltaTime; }
    }
}