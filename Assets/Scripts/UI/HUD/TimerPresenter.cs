using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Presenter for round timer.
	/// </summary>
	public class TimerPresenter : MonoBehaviour {
		public Text timerText;

		Systems.RoundManager round;

		void Awake() {
			round = Systems.RoundManager.Instance;
			if (timerText != null && string.IsNullOrEmpty(timerText.text))
			{
				timerText.text = "--"; // 預設佔位，避免空白
			}
		}
		void OnEnable() {
			if (round != null)
			{
				round.OnTimerChanged += OnTimer;
			}
			Init();
		}
		void OnDisable() {
			if (round != null)
			{
				round.OnTimerChanged -= OnTimer;
			}
		}
		void Init() {
			int seconds = 60;
			if (round != null)
			{
				seconds = Mathf.CeilToInt(roundTime(round));
			}
			if (timerText != null)
			{
				timerText.text = seconds.ToString();
			}
		}
		float roundTime(Systems.RoundManager r) {
			var f = typeof(Systems.RoundManager).GetField("roundTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			if (r == null || f == null)
			{
				return 60f;
			}
			return (float)f.GetValue(r);
		}
		void OnTimer(int seconds) {
			if (timerText != null)
			{
				timerText.text = seconds.ToString();
			}
		}
	}
}