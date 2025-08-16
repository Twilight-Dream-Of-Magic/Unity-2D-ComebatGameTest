using UnityEngine;
using FightingGame.Combat;

namespace Fighter.InputSystem
{
	/// <summary>
	/// Player input reader and dispatcher into CommandQueue and FighterActor
	/// </summary>
	[DefaultExecutionOrder(30)]
	public class PlayerBrain : MonoBehaviour
	{
		[Header("Wiring")] public FightingGame.Combat.Actors.FighterActor fighter;
		[Header("Reader")] public float horizontalScale = 1f;
		public Data.InputTuningConfig inputTuning;

		CommandQueue commandQueue;

		float nextHealAt;

		void Awake()
		{
			if (!fighter)
			{
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
			commandQueue = GetComponent<CommandQueue>();
			if (!commandQueue)
			{
				commandQueue = gameObject.AddComponent<CommandQueue>();
			}
			if (inputTuning)
			{
				commandQueue.tuning = inputTuning;
			}
		}

		void Update()
		{
			ReadKeyboard();
		}

		void ReadKeyboard()
		{
			var cfg = Systems.RuntimeConfig.Instance;
			var c = new FightingGame.Combat.Actors.FighterCommands();
			// 只保留 WASD
			float x = 0f; if (Input.GetKey(KeyCode.A)) x -= 1f; if (Input.GetKey(KeyCode.D)) x += 1f;
			c.moveX = x * horizontalScale;
			c.jump = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
			c.crouch = Input.GetKey(KeyCode.S);
			c.block = Input.GetKey(cfg != null ? cfg.playerBlockKey : KeyCode.L);
			c.dodge = Input.GetKey(cfg != null ? cfg.playerDodgeKey : KeyCode.LeftShift);
			bool lightDown = Input.GetKeyDown(KeyCode.J);
			bool heavyDown = Input.GetKeyDown(KeyCode.K);
			c.light = lightDown;
			c.heavy = heavyDown;
			fighter.SetCommands(in c);

			// 不再記錄方向鍵歷史（禁用序列系統）

			if (lightDown && fighter.CurrentMove == null) { fighter.EnterAttackHFSM("Light"); }
			if (heavyDown && fighter.CurrentMove == null) { fighter.EnterAttackHFSM("Heavy"); }

			// Throw: direct domain call（不走序列）
			if (Input.GetKeyDown(KeyCode.U))
			{
				var off = fighter.HRoot?.Offense;
				var opp = fighter.opponent ? fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (off != null)
				{
					if (!fighter.IsGrounded()) off.BeginAirThrowFlat();
					else if (opp && opp.PendingCommands.block && fighter.IsOpponentInThrowRange(1.0f)) off.BeginGuardBreakThrowFlat();
					else off.BeginThrowFlat();
				}
			}

			// Super（關閉）：受 RuntimeConfig.superInputEnabled 控制，默認關閉
			if (cfg != null && cfg.superInputEnabled)
			{
				if (Input.GetKeyDown(cfg.playerSuperKey))
				{
					fighter.HRoot?.Offense?.BeginSuperFlat();
				}
			}

			// Heal：屬防禦域；冷卻與低血量保底
			if (cfg != null && Input.GetKeyDown(cfg.playerHealKey))
			{
				if (Time.time >= nextHealAt)
				{
					bool lowEnough = !cfg.playerLowHPGuardEnabled || (fighter.currentHealth <= cfg.playerLowHPThreshold);
					if (lowEnough)
					{
						fighter.HRoot?.Defense?.BeginHealFlatDefense("Heal");
						nextHealAt = Time.time + Mathf.Max(0.1f, cfg.playerHealCooldown);
					}
				}
			}
		}
	}
}