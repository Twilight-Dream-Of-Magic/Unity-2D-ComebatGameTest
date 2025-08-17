using UnityEngine;
using FightingGame.Combat.Actors;
using Fighter.AI;
using Systems;
using Data;

namespace Dev {
	public class BattleAutoSetup : MonoBehaviour {
		[Header("Scene Wiring")]
		public bool createManagers = true;
		public bool createUI = true;
		public bool createGround = true;
		public Vector2 arenaHalfExtents = new Vector2(256f, 3.5f);
		[Header("Configs")]
		public UIMode initialUIMode = UIMode.Debug;
		[Header("Tuning Assets")] public InputTuningConfig inputTuning; public CommandSequenceSet commandSequenceSet;

		private void Start() {
			ArenaBuilder.CreateGround(arenaHalfExtents);
			ManagersBootstrapper.EnsureManagers(arenaHalfExtents);
			if (RuntimeConfig.Instance)
				RuntimeConfig.Instance.SetUIMode(initialUIMode);
			if (createUI) 
				UIBootstrapper.BuildHUD();
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