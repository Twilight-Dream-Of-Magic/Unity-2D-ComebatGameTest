using UnityEngine;

namespace Dev
{
	/// <summary>
	/// Ensures that a HUD Canvas exists in the scene and attaches required UI components.
	/// 確保場景中存在 HUD 畫布並掛載所需的 UI 組件。
	/// </summary>
	public static class UIBootstrapper
	{
		/// <summary>
		/// Build or retrieve the HUD canvas with required components.
		/// 建立或獲取 HUD 畫布，並附加必要的 UI 組件。
		/// </summary>
		public static void BuildHUD()
		{
			// Try to find existing Canvas
			var canvasGo = GameObject.Find("Canvas");
			if (canvasGo == null)
			{
				canvasGo = new GameObject("Canvas");
			}

			// Ensure required UI components exist
			if (canvasGo.GetComponent<UI.CanvasRoot>() == null)
			{
				canvasGo.AddComponent<UI.CanvasRoot>();
			}

			if (canvasGo.GetComponent<UI.BattleHUD>() == null)
			{
				canvasGo.AddComponent<UI.BattleHUD>();
			}
		}
	}
}