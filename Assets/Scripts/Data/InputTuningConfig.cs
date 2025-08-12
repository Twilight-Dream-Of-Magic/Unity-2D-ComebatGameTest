using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "KOF/Input Tuning Config", fileName = "InputTuningConfig")]
    public class InputTuningConfig : ScriptableObject {
        [Header("Command Queue")]
        [Tooltip("Seconds a command token stays in the queue before expiring.")]
        public float commandBufferWindow = 0.25f;

        [Header("Special Input")] 
        [Tooltip("How long to keep token history for special detection.")]
        public float specialHistoryLifetime = 0.8f;
        [Tooltip("Default special input window when a SpecialMoveSet entry has 0.")]
        public float defaultSpecialWindowSeconds = 0.6f;
    }
}