using UnityEngine;

namespace Data
{
	/// <summary>
	/// Designer-exposed tuning for input buffers and special detection windows.
	/// 面向策劃人員的輸入緩衝與搓招識別時窗配置。
	/// </summary>
	[CreateAssetMenu(menuName = "KOF/Input Tuning Config", fileName = "InputTuningConfig")]
	public class InputTuningConfig : ScriptableObject
	{
		[Header("Command Queue")]
		[Tooltip("Seconds a command token stays in the queue before expiring / 指令保留在佇列中的秒數（超時後移除）。")]
		/// <summary>
		/// Command buffer window in seconds.
		/// 指令緩衝窗口（秒）。
		/// </summary>
		public float commandBufferWindow = 0.25f;

		[Header("Special Input")]
		[Tooltip("How long to keep token history for special detection / 搓招識別時保留的歷史時長。")]
		/// <summary>
		/// Special detection history lifetime.
		/// 搓招識別歷史壽命。
		/// </summary>
		public float specialHistoryLifetime = 1.2f;

		[Tooltip("Default special window when entry is 0 / 當條目為0時的默認搓招時窗（秒）。")]
		/// <summary>
		/// Default special window when entry is 0.
		/// 默認搓招時窗。
		/// </summary>
		public float defaultSpecialWindowSeconds = 1.0f;
	}
}