using UnityEngine;

namespace Dev {
	public static class UIBootstrapper
	{
		public static void BuildHUD()
		{
			// ���Բ�����Ϊ "Canvas" ����Ϸ����
			var canvasGo = GameObject.Find("Canvas");
			// ����Ҳ����򴴽��µ�
			if (canvasGo == null)
			{
				canvasGo = new GameObject("Canvas");
			}
			// ����������
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