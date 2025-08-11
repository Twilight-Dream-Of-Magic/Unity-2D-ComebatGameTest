using UnityEngine;

namespace Systems {
    public class CameraShaker : MonoBehaviour {
        public static CameraShaker Instance { get; private set; }
        Vector3 originalPos; float timeLeft; float amplitude;
        private void Awake() { Instance = this; originalPos = transform.localPosition; }
        public void Shake(float amp, float duration) { amplitude = amp; timeLeft = duration; }
        private void LateUpdate() {
            if (timeLeft > 0) {
                timeLeft -= Time.unscaledDeltaTime;
                transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * amplitude;
                if (timeLeft <= 0) transform.localPosition = originalPos;
            }
        }
    }
}