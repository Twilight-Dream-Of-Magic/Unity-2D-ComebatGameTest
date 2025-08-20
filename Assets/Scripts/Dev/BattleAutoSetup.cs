using UnityEngine;
using Systems;
using Data;

namespace Dev
{
	/// <summary>
	/// Automatically sets up a simple battle scene for development testing.
	/// 自動建立開發測試用的戰鬥場景（場地、管理器、UI、玩家配置）。
	/// </summary>
	public class BattleAutoSetup : MonoBehaviour
	{
		[Header("Scene Wiring")]
		public bool createUI = true;       // Whether to auto-create HUD UI / 是否自動生成戰鬥UI
		public Vector2 arenaHalfExtents = new Vector2(256f, 3.5f); // Arena size half extents / 場地半尺寸

		[Header("Configs")]
		public UIMode initialUIMode = UIMode.Debug; // Initial UI mode / 初始UI模式

		[Header("Tuning Assets")]
		public InputTuningConfig inputTuning;       // Input tuning config / 輸入調參配置
		public CommandSequenceSet commandSequenceSet; // Command sequence set / 搓招序列配置

		/// <summary>
		/// Unity lifecycle: called once on scene start.
		/// Unity 生命週期：場景開始時調用。
		/// </summary>
		private void Start()
		{
			ArenaBuilder.CreateGround(arenaHalfExtents);
			ManagersBootstrapper.EnsureManagers(arenaHalfExtents);

			if (RuntimeConfig.Instance)
			{
				RuntimeConfig.Instance.SetUIMode(initialUIMode);
			}

			if (createUI)
			{
				UIBootstrapper.BuildHUD();
			}

			// Inject ScriptableObjects into Fighter via AutoSetup
			// 透過 AutoSetup 注入 ScriptableObject（工廠創建的 Fighter 也能獲取配置）
			var fighterPlayer = Systems.RoundManager.Instance ? Systems.RoundManager.Instance.p1 : null;
			if (fighterPlayer)
			{
				var resolver = fighterPlayer.GetComponent<FightingGame.Combat.SpecialInputResolver>();
				if (resolver && commandSequenceSet)
				{
					resolver.SetConfig(fighterPlayer, inputTuning, commandSequenceSet);
				}
			}

			Debug.Log("BattleAutoSetup ready: A/D move, Space jump, S crouch, J/K attack, L block, LeftShift dodge");
		}
	}
}
