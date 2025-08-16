using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "KOF/Input Tuning Config", fileName = "InputTuningConfig")]
    /// <summary>
    /// Designer-exposed tuning for input buffers and special detection windows.
    /// 面向策划的输入缓冲与搓招识别时窗配置。
    /// </summary>
    public class InputTuningConfig : ScriptableObject {
        [Header("Command Queue")]
        [Tooltip("Seconds a command token stays in the queue before expiring.")]
        /// <summary>Command buffer window in seconds. 指令缓冲窗口（秒）。</summary>
        public float commandBufferWindow = 0.25f;

        [Header("Special Input")] 
        [Tooltip("How long to keep token history for special detection.")]
        /// <summary>Special detection history lifetime. 搓招识别历史寿命。</summary>
        public float specialHistoryLifetime = 0.8f;
        [Tooltip("Default special input window when a SpecialMoveSet entry has 0.")]
        /// <summary>Default special window when entry is 0. 默认搓招时窗。</summary>
        public float defaultSpecialWindowSeconds = 0.6f;
    }
}