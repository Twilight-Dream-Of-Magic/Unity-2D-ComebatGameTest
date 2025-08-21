using UnityEngine;
using FightingGame.Combat;

namespace Fighter.Core
{
	/// <summary>
	/// Handles receiving damage and routing to defense finite state machine states.
	/// 負責處理角色受傷並路由到防禦狀態機。
	/// </summary>
	public class DamageReceiver : MonoBehaviour
	{
		[Header("References")]
		public FightingGame.Combat.Actors.FighterActor fighter; // Reference to the fighter actor / 角色實例
		public Animator animator;                               // Animator reference / 動畫器

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
		}

		/// <summary>
		/// Process an incoming hit and apply damage, block, or invulnerability checks.
		/// 處理進攻方輸入的打擊，應用傷害、防禦或無敵判定。
		/// </summary>
		/// <param name="damageInfo">Damage information / 傷害資訊</param>
		/// <param name="attacker">The attacking fighter / 攻擊方角色</param>
		public void TakeHit(DamageInfo damageInfo, FightingGame.Combat.Actors.FighterActor attacker)
		{
			// === Dodge invulnerability (except super) / 閃避無敵（超必殺除外） ===
			if (fighter != null && fighter.GetCurrentStateName() == "Dodge" && !damageInfo.isSuper)
			{
#if UNITY_EDITOR
				Debug.Log("[DamageReceiver] Dodge invulnerability ignored a non-super hit on " + (fighter != null ? fighter.name : "<null>"));
#endif
				return;
			}

			// === Upper body invulnerability ===
			if ((damageInfo.level == HitLevel.High || damageInfo.level == HitLevel.Overhead) && fighter.UpperBodyInvulnerable)
			{
#if UNITY_EDITOR
				Debug.Log("[DamageReceiver] Upper body invulnerability ignored the hit on " + (fighter != null ? fighter.name : "<null>"));
#endif
				return;
			}

			// === Lower body invulnerability ===
			if (damageInfo.level == HitLevel.Low && fighter.LowerBodyInvulnerable)
			{
#if UNITY_EDITOR
				Debug.Log("[DamageReceiver] Lower body invulnerability ignored the hit on " + (fighter != null ? fighter.name : "<null>"));
#endif
				return;
			}

			// === Blocking check ===
			bool isBlocked = false;
			Systems.RuntimeConfig runtimeConfig = Systems.RuntimeConfig.Instance;

			if (runtimeConfig != null)
			{
				isBlocked = GuardEvaluator.CanBlockTimed(
					fighter,
					fighter.PendingCommands.block,
					fighter.IsGrounded(),
					fighter.IsCrouching,
					damageInfo.level,
					runtimeConfig.blockMaxHoldSeconds
				) && damageInfo.canBeBlocked;

				// If holding block too long, trigger a temporary lock cooldown
				// 若玩家持續按住超過窗口，觸發一次鎖定冷卻
				if (!isBlocked && fighter.PendingCommands.block && fighter.GetBlockHeldSeconds() > runtimeConfig.blockMaxHoldSeconds)
				{
					fighter.gameObject.SendMessage("__LockBlockForSeconds", runtimeConfig.blockCooldownSeconds, SendMessageOptions.DontRequireReceiver);
				}
			}

			FighterResources resourceComponent = fighter.GetComponent<Fighter.Core.FighterResources>();
			int healthBefore = fighter.currentHealth;

			if (!isBlocked)
			{
				// === Apply damage ===
				int nonNegativeDamage = Mathf.Max(0, damageInfo.damage);
				int healthAfter = Mathf.Max(0, healthBefore - nonNegativeDamage);

#if UNITY_EDITOR
				string attackerName = attacker != null ? attacker.name : "<null>";
				string defenderName = fighter != null ? fighter.name : "<null>";
				Debug.Log("[DamageReceiver] HIT  attacker=" + attackerName + "  defender=" + defenderName + "  damage=" + nonNegativeDamage + "  health=" + healthBefore + "->" + healthAfter);
#endif

				if (resourceComponent != null)
				{
					resourceComponent.DecreaseHealth(nonNegativeDamage);
				}
				else
				{
					fighter.currentHealth = healthAfter;
				}

				if (animator != null && animator.runtimeAnimatorController != null)
				{
					animator.SetTrigger("Hit");
				}

				fighter.MarkHitConfirmed(damageInfo.hitstopOnHit);
				Systems.CameraShaker.Instance?.Shake(0.1f, damageInfo.hitstopOnHit);

				// Award meter to attacker if the special system is enabled / 攻擊方獲得氣槽（若系統啟用）
				if (attacker != null && Systems.RuntimeConfig.Instance != null && Systems.RuntimeConfig.Instance.specialsEnabled)
				{
					FighterResources attackerResources = attacker.GetComponent<Fighter.Core.FighterResources>();
					if (attackerResources != null && damageInfo.meterOnHit > 0)
					{
						attackerResources.IncreaseMeter(damageInfo.meterOnHit);
					}
				}

				Systems.DamageBus.Raise(
					Mathf.Max(0, damageInfo.damage),
					fighter.transform.position + new Vector3(0f, 1f, 0f),
					false,
					attacker,
					fighter
				);

				var defense = fighter.HRoot != null ? fighter.HRoot.Defense : null;
				if (defense != null)
				{
					if (damageInfo.knockdownKind != KnockdownKind.None)
					{
						bool isHardKnockdown = damageInfo.knockdownKind == KnockdownKind.Hard;
						var downedState = defense.Downed;
						if (downedState != null)
						{
							downedState.Begin(isHardKnockdown, damageInfo.hitstun);
							fighter.HMachine.ChangeState(downedState);
						}
					}
					else
					{
						var hitstunState = defense.Hitstun;
						if (hitstunState != null)
						{
							hitstunState.Begin(damageInfo.hitstun);
							fighter.HMachine.ChangeState(hitstunState);
						}
					}
				}
			}
			else
			{
				// === Blocked ===
#if UNITY_EDITOR
				string attackerName = attacker != null ? attacker.name : "<null>";
				string defenderName = fighter != null ? fighter.name : "<null>";
				Debug.Log("[DamageReceiver] BLOCK  attacker=" + attackerName + "  defender=" + defenderName + "  level=" + damageInfo.level);
#endif

				fighter.MarkHitConfirmed(damageInfo.hitstopOnBlock);
				Systems.CameraShaker.Instance?.Shake(0.05f, damageInfo.hitstopOnBlock);

				if (attacker != null && Systems.RuntimeConfig.Instance != null && Systems.RuntimeConfig.Instance.specialsEnabled)
				{
					FighterResources attackerResources = attacker.GetComponent<Fighter.Core.FighterResources>();
					if (attackerResources != null && damageInfo.meterOnBlock > 0)
					{
						attackerResources.IncreaseMeter(damageInfo.meterOnBlock);
					}
				}

				Systems.DamageBus.Raise(
					0,
					fighter.transform.position + new Vector3(0f, 1f, 0f),
					true,
					attacker,
					fighter
				);

				var defense = fighter.HRoot != null ? fighter.HRoot.Defense : null;
				if (defense != null)
				{
					bool isCrouchingGuard = fighter.IsCrouching || fighter.PendingCommands.crouch;
					fighter.HMachine.ChangeState(isCrouchingGuard ? (FightingGame.Combat.State.HFSM.HState)defense.BlockCrouch : (FightingGame.Combat.State.HFSM.HState)defense.BlockStand);
				}
			}

			// === Notify damage received ===
			if (fighter.currentHealth < healthBefore)
			{
				fighter.NotifyDamagedForReceivers(attacker);
			}
		}
	}
}
