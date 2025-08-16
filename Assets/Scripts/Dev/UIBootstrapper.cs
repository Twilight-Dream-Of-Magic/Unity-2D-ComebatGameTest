using UnityEngine;

namespace Dev {
	public static class UIBootstrapper
	{
		public static void BuildHUD()
		{
			// 尝试查找名为 "Canvas" 的游戏对象
			var canvasGo = GameObject.Find("Canvas");
			// 如果找不到则创建新的
			if (canvasGo == null)
			{
				canvasGo = new GameObject("Canvas");
			}
			// 添加所需组件
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