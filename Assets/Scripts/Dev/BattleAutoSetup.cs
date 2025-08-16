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
			if (createManagers) ManagersBootstrapper.EnsureManagers(arenaHalfExtents);
			if (RuntimeConfig.Instance) RuntimeConfig.Instance.SetUIMode(initialUIMode);
			if (createGround) ArenaBuilder.CreateGround(arenaHalfExtents);
			var p1 = FighterFactory.CreateFighter("Player", new Vector3(-1.6f, -1f, 0f), new Color(0.2f,0.6f,1f,1f), true, inputTuning);
			var p2 = FighterFactory.CreateFighter("Enemy(AI)", new Vector3(1.6f, -1f, 0f), new Color(1f,0.4f,0.3f,1f), false, inputTuning);
			FightLinker.LinkOpponents(p1, p2, arenaHalfExtents);
			if (createUI) UIBootstrapper.BuildHUD(p1, p2);
			Debug.Log("BattleAutoSetup ready: A/D move, Space jump, S crouch, J/K attack, Shift block, L dodge");
		}
	}
}