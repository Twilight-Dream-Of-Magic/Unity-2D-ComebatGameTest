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
		FightingGame.Combat.SpecialInputResolver specialResolver;
		Data.CommandSequenceSet commandSequenceSet;

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
			// 搓招解析器與資料（若場景未掛載，這裡動態補）
			specialResolver = GetComponent<FightingGame.Combat.SpecialInputResolver>();
			if (!specialResolver) specialResolver = gameObject.AddComponent<FightingGame.Combat.SpecialInputResolver>();
			// 優先使用新的 CommandSequenceSet
			if (!commandSequenceSet)
			{
				commandSequenceSet = FindObjectOfType<Data.CommandSequenceSet>();
			}
			if (commandSequenceSet)
				specialResolver.SetConfig(fighter, inputTuning, commandSequenceSet);
		}

		void Update()
		{
			ReadKeyboard();
		}

		void FeedDirectionalTokens(float xInput, bool isJumpDown, bool isCrouchDown)
		{
			if (specialResolver == null) return;
			// 方向（相對於面向）：按下瞬間才推送；不推 Neutral
			float facingSign = (fighter != null && fighter.facingRight) ? 1f : -1f;
			// 基於鍵位的瞬間：A/D 轉為相對 Forward/Back
			bool aDown = Input.GetKeyDown(KeyCode.A);
			bool dDown = Input.GetKeyDown(KeyCode.D);
			if (aDown || dDown)
			{
				float rel = (aDown ? -1f : 0f) + (dDown ? 1f : 0f);
				rel *= facingSign;
				if (rel > 0.5f) specialResolver.Push(CommandToken.Forward);
				else if (rel < -0.5f) specialResolver.Push(CommandToken.Back);
			}
			// 垂直（只記錄按下瞬間）
			if (isCrouchDown) specialResolver.Push(CommandToken.Down);
			if (isJumpDown) specialResolver.Push(CommandToken.Up);
		}

		float _lastRel;
		CommandToken _lastDirToken;

		void FeedAttackTokens(bool lightDown, bool heavyDown)
		{
			if (specialResolver == null) return;
			if (lightDown) specialResolver.Push(CommandToken.Light);
			if (heavyDown) specialResolver.Push(CommandToken.Heavy);
		}

		void ReadKeyboard()
		{
			var runtimeConfig = Systems.RuntimeConfig.Instance;
			var fighterCommands = new FightingGame.Combat.Actors.FighterCommands();
			// 只保留 WASD
			float x = 0f; if (Input.GetKey(KeyCode.A)) x -= 1f; if (Input.GetKey(KeyCode.D)) x += 1f;
			fighterCommands.moveX = x * horizontalScale;
			bool jumpHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
			bool crouchHeld = Input.GetKey(KeyCode.S);
			fighterCommands.jump = jumpHeld;
			fighterCommands.crouch = crouchHeld;
			fighterCommands.block = Input.GetKey(runtimeConfig != null ? runtimeConfig.playerBlockKey : KeyCode.L);
			fighterCommands.dodge = Input.GetKey(runtimeConfig != null ? runtimeConfig.playerDodgeKey : KeyCode.LeftShift);
			bool lightDown = Input.GetKeyDown(KeyCode.J);
			bool heavyDown = Input.GetKeyDown(KeyCode.K);
			fighterCommands.light = lightDown;
			fighterCommands.heavy = heavyDown;
			fighter.SetCommands(in fighterCommands);

			// 餵入搓招解析器 token（方向+攻擊鍵）
			bool jumpDown = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
			bool crouchDown = Input.GetKeyDown(KeyCode.S);
			FeedDirectionalTokens(x, jumpDown, crouchDown);
			FeedAttackTokens(lightDown, heavyDown);

			// 先嘗試解析搓招；命中後直接返回（避免重複觸發）
			if (specialResolver != null && specialResolver.TryResolveAndExecute())
			{
				return;
			}

			// 基礎攻擊：當前非攻擊中才直入
			if (lightDown && fighter.CurrentMove == null) { fighter.EnterAttackHFSM("Light"); }
			if (heavyDown && fighter.CurrentMove == null) { fighter.EnterAttackHFSM("Heavy"); }

			// Throw: direct domain call（不走序列）
			if (Input.GetKeyDown(KeyCode.U))
			{
				var offens = fighter.HRoot?.Offense;
				var opponentActor = fighter.opponent ? fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (offens != null)
				{
					if (!fighter.IsGrounded()) offens.BeginAirThrowFlat();
					else if (opponentActor && opponentActor.PendingCommands.block && fighter.IsOpponentInThrowRange(1.0f)) offens.BeginGuardBreakThrowFlat();
					else offens.BeginThrowFlat();
				}
			}

			// Super（關閉）：受 RuntimeConfig.superInputEnabled 控制，默認關閉
			if (runtimeConfig != null && runtimeConfig.superInputEnabled && Input.GetKey(runtimeConfig.playerSuperKey))
			{
				fighter.HRoot?.Offense?.BeginSuperFlat();
			}

			// Heal：屬防禦域；冷卻與低血量保底
			if (runtimeConfig != null && runtimeConfig.healInputEnabled && Input.GetKey(runtimeConfig.playerHealKey))
			{
				if (Time.time >= nextHealAt)
				{
					bool lowEnough = !runtimeConfig.playerLowHPGuardEnabled || (fighter.currentHealth <= runtimeConfig.playerLowHPThreshold);
					if (lowEnough)
					{
						fighter.HRoot?.Defense?.BeginHealFlatDefense("Heal");
						nextHealAt = Time.time + Mathf.Max(0.1f, runtimeConfig.playerHealCooldown);
					}
				}
			}
		}
	}
}