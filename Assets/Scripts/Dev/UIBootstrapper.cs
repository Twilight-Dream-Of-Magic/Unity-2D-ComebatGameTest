using UnityEngine;

namespace Dev
{
	/// <summary>
	/// Ensures that a HUD Canvas exists in the scene and attaches required UI components.
	/// �_�������д��� HUD �����K���d����� UI �M����
	/// </summary>
	public static class UIBootstrapper
	{
		/// <summary>
		/// Build or retrieve the HUD canvas with required components.
		/// ������@ȡ HUD �������K���ӱ�Ҫ�� UI �M����
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