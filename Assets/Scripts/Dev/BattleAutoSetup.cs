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
		[Header("Modes")] public bool demoScripted = false; public bool playerIsHuman = true; public UIMode initialUIMode = UIMode.Debug;
		[Header("Tuning Assets")] public InputTuningConfig inputTuning;

		private void Start() {
			ArenaBuilder.CreateGround(arenaHalfExtents);
			ManagersBootstrapper.EnsureManagers(arenaHalfExtents);
			if (RuntimeConfig.Instance)
				RuntimeConfig.Instance.SetUIMode(initialUIMode);
			if (createUI) 
				UIBootstrapper.BuildHUD();
			Debug.Log("BattleAutoSetup ready: A/D move, Space jump, S crouch, J/K attack, Shift block, L dodge");
		}
	}
}