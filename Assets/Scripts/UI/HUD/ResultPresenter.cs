using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD {
	/// <summary>
	/// Presenter for round result panel and text.
	/// </summary>
	public class ResultPresenter : MonoBehaviour {
		public GameObject panel;
		public Text resultText;

		Systems.RoundManager round;

		void Awake() {
			round = Systems.RoundManager.Instance;
			if (panel == null)
			{
				panel = gameObject;
			}
			if (panel)
			{
				panel.SetActive(false);
			}
			if (resultText != null)
			{
				// 預設紫色，便於區分結果文字
				resultText.color = new Color(0.7f, 0.5f, 1f, 1f);
			}
		}
		void OnEnable() {
			if (round)
			{
				round.OnRoundEnd += OnRoundEnd;
				// 安全1：若回合已經結束且有結果文字，立即補顯
				if (round.IsEnded && !string.IsNullOrEmpty(round.LastResultText))
				{
					OnRoundEnd(round.LastResultText);
				}
			}
		}
		void OnDisable() {
			if (round)
			{
				round.OnRoundEnd -= OnRoundEnd;
			}
		}
		void OnRoundEnd(string text) {
			// 安全2：若未傳入，使用 RoundManager 的最後結果文字
			if (string.IsNullOrEmpty(text) && round != null)
			{
				text = round.LastResultText;
			}
			if (panel)
			{
				panel.SetActive(true);
			}
			if (resultText)
			{
				resultText.text = text;
				// 仍保留 P1/P2 配色；其他情況預設紫色
				resultText.color = text.StartsWith("P1") ? new Color(0.4f,0.8f,1f,1f) : text.StartsWith("P2") ? new Color(1f,0.4f,0.4f,1f) : new Color(0.7f, 0.5f, 1f, 1f);
			}
		}
	}
}