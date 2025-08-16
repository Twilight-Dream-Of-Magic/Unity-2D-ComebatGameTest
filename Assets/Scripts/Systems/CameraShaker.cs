using UnityEngine;

namespace Systems {
    /// <summary>
    /// Simple camera shaker using unscaled time and random 2D offsets.
    /// 简单摄像机震动：基于非缩放时间的随机二维偏移。
    /// </summary>
    public class CameraShaker : MonoBehaviour {
        /// <summary>Singleton instance. 单例。</summary>
        public static CameraShaker Instance { get; private set; }
        Vector3 originalPos; float timeLeft; float amplitude;
        private void Awake() { Instance = this; originalPos = transform.localPosition; }
        /// <summary>Shake with amplitude and duration. 以幅度与时长震动。</summary>
        public void Shake(float amp, float duration) { amplitude = Mathf.Max(amplitude, amp); timeLeft = Mathf.Max(timeLeft, duration); }
        private void LateUpdate() {
            if (timeLeft > 0) {
                timeLeft -= Time.unscaledDeltaTime;
                transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * amplitude;
                if (timeLeft <= 0) transform.localPosition = originalPos;
            }
        }
    }
}