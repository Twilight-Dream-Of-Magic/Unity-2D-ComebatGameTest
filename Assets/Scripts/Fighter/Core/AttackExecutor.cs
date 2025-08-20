using UnityEngine;
using Data;
using Systems;

namespace Fighter.Core
{
	/// <summary>
	/// Encapsulates the full attack lifecycle:
	/// - Select move data by trigger
	/// - Handle meter cost and deduction
	/// - Toggle hitboxes during active frames
	/// - Apply local hit-stop and feedback
	/// 封裝整個攻擊流程：根據 Trigger 選取招式、處理氣槽消耗、在生效幀開關命中盒、施加打停與反饋。
	/// </summary>
	public class CriticalAttackExecutor : MonoBehaviour
	{
		[Header("References")]
		public FightingGame.Combat.Actors.FighterActor fighter; // Fighter actor reference / 角色實例
		public Animator animator;                               // Animator reference / 動畫器
		public FightingGame.Combat.Hitbox[] hitboxes;           // Assigned hitboxes / 命中盒陣列

		private bool hitStopApplied;                            // Prevent duplicate hit-stop / 防止重複打停
		private Vector3[] originalLocalPos;                     // Cached original hitbox local positions / 命中盒原始位置緩存
		private bool activeState;                               // Attack active state flag / 攻擊是否處於生效狀態

		/// <summary>
		/// Resolve hitboxes from FighterActor or local cache.
		/// 優先從 FighterActor 獲取命中盒，否則回退到本地緩存。
		/// </summary>
		private FightingGame.Combat.Hitbox[] ResolveHitboxes()
		{
			if (fighter != null && fighter.hitboxes != null && fighter.hitboxes.Length > 0)
			{
				return fighter.hitboxes;
			}
			if (hitboxes == null || hitboxes.Length == 0)
			{
				hitboxes = GetComponentsInChildren<FightingGame.Combat.Hitbox>(true);
			}
			return hitboxes;
		}

		private void Awake()
		{
			if (!fighter)
			{
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
			if (!animator)
			{
				animator = GetComponent<Animator>();
			}

			// Ensure owner binding for all hitboxes
			var hitboxArray = ResolveHitboxes();
			if (hitboxArray != null)
			{
				foreach (var hitbox in hitboxArray)
				{
					if (hitbox != null && hitbox.owner == null)
					{
						hitbox.owner = fighter;
					}
				}
			}
			CacheOriginals();
		}

		/// <summary>
		/// Trigger an attack move via trigger string.
		/// Handles meter deduction and sets animator trigger.
		/// 通過 Trigger 啟動攻擊，處理氣槽消耗並設置動畫器觸發器。
		/// </summary>
		public void TriggerAttack(string trigger)
		{
			fighter.SetDebugMoveName(trigger);
			if (fighter.actionSet != null)
			{
				fighter.SetCurrentMove(fighter.actionSet.Get(trigger));
			}
			CombatActionDefinition actionData = fighter.CurrentAction;
			FighterResources fighterResources = fighter.GetComponent<FighterResources>();
			if (actionData != null && actionData.meterCost > 0)
			{
				if (fighterResources == null)
				{
					fighterResources = fighter.gameObject.AddComponent<FighterResources>();
				}
				bool ok = fighterResources.DecreaseMeter(actionData.meterCost);
				if (!ok)
				{
					fighter.SetCurrentMove(null);
					return;
				}
			}
			if (animator && animator.runtimeAnimatorController)
			{
				animator.SetTrigger(trigger);
			}
		}

		/// <summary>
		/// Enable or disable attack active frames.
		/// 切換攻擊生效幀，並重置狀態與同步視覺效果。
		/// </summary>
		public void SetAttackActive(bool on)
		{
			if (activeState == on)
			{
				return; // Already in this state / 狀態未變
			}
			activeState = on;
			fighter.SetDebugHitActive(on);

			if (on)
			{
				fighter.ClearHitVictimsSet();
				hitStopApplied = false;
			}

			var hitboxArray = ResolveHitboxes();
			if (hitboxArray == null)
			{
				Debug.LogWarning($"[AttackExecutor] No hitboxes found for {fighter?.name} when SetAttackActive({on})");
				return;
			}

			// Late bind owners if needed (hitboxes may be created after Awake)
			for (int i = 0; i < hitboxArray.Length; i++)
			{
				if (hitboxArray[i] != null && hitboxArray[i].owner == null)
				{
					hitboxArray[i].owner = fighter;
				}
			}

			if (on)
			{
				MaybeOffsetAerialHeavy();
			}
			else
			{
				RestoreHitboxPositions();
			}

			foreach (var hitbox in hitboxArray)
			{
				if (hitbox != null)
				{
					hitbox.active = on;
				}
			}
			fighter.SetActiveColor(on);

#if UNITY_EDITOR
			Debug.Log($"[AttackExecutor] {(on ? "Enable" : "Disable")} {hitboxArray.Length} hitboxes for {fighter?.name}, move={fighter?.CurrentAction?.triggerName}");
#endif
		}

		/// <summary>
		/// Local hit-confirm feedback (hit-stop + camera shake).
		/// 本地命中反饋：打停 + 震屏。
		/// </summary>
		public void OnHitConfirmedLocal(float seconds)
		{
			if (hitStopApplied)
			{
				return;
			}
			hitStopApplied = true;
			int frames = FrameClock.SecondsToFrames(seconds);
			fighter.FreezeFrames(frames);
			Systems.CameraShaker.Instance?.Shake(0.12f, seconds);
		}

		/// <summary>
		/// Cache original local positions of hitboxes for restoration.
		/// 快取命中盒的原始局部位置，便於恢復。
		/// </summary>
		private void CacheOriginals()
		{
			var hitboxArray = ResolveHitboxes();
			if (hitboxArray == null)
				return;
			originalLocalPos = new Vector3[hitboxArray.Length];
			for (int i = 0; i < hitboxArray.Length; i++)
			{
				originalLocalPos[i] = hitboxArray[i].transform.localPosition;
			}
		}

		/// <summary>
		/// Apply small positional offset for aerial heavy attacks to better match visuals.
		/// 空中 Heavy 攻擊時偏移命中盒位置以更貼合視覺效果。
		/// </summary>
		private void MaybeOffsetAerialHeavy()
		{
			if (fighter.IsGrounded())
				return;
			var move = fighter.CurrentAction;
			if (move == null || move.triggerName != "Heavy")
				return;

			float forward = fighter.facingRight ? 1f : -1f;
			var hitboxArray = ResolveHitboxes();
			for (int i = 0; i < hitboxArray.Length; i++)
			{
				var transformComponent = hitboxArray[i].transform;
				transformComponent.localPosition =
					(originalLocalPos != null && i < originalLocalPos.Length)
						? originalLocalPos[i] + new Vector3(0.45f * forward, -0.25f, 0f)
						: transformComponent.localPosition + new Vector3(0.45f * forward, -0.25f, 0f);
			}
		}

		/// <summary>
		/// Restore hitbox positions to original local positions.
		/// 將命中盒恢復到初始局部位置。
		/// </summary>
		private void RestoreHitboxPositions()
		{
			var hitboxArray = ResolveHitboxes();
			if (hitboxArray == null || originalLocalPos == null)
				return;
			for (int i = 0; i < hitboxArray.Length && i < originalLocalPos.Length; i++)
			{
				hitboxArray[i].transform.localPosition = originalLocalPos[i];
			}
		}
	}
}
