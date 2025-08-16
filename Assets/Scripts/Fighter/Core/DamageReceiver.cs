using UnityEngine;
using FightingGame.Combat;

namespace Fighter.Core {
	/// <summary>
	/// Handles receiving damage and routing to Defense flat FSM states.
	/// </summary>
	public class DamageReceiver : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public Animator animator;

		void Awake() {
			if (!fighter)
			{
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
			if (!animator)
			{
				animator = GetComponent<Animator>();
			}
		}

		public void TakeHit(DamageInfo info, FightingGame.Combat.Actors.FighterActor attacker) {
			// Dodge: fully invulnerable to all but super
			if (fighter != null && fighter.GetCurrentStateName() == "Dodge" && !info.isSuper)
			{
				#if UNITY_EDITOR
				Debug.Log($"[DamageReceiver] DODGE i-frame ignored non-super hit on {fighter?.name}");
				#endif
				return;
			}
			if ((info.level == HitLevel.High || info.level == HitLevel.Overhead) && fighter.UpperBodyInvuln)
			{
				#if UNITY_EDITOR
				Debug.Log($"[DamageReceiver] Upper-body invulnerability ignored hit on {fighter?.name}");
				#endif
				return;
			}
			if ((info.level == HitLevel.Low) && fighter.LowerBodyInvuln)
			{
				#if UNITY_EDITOR
				Debug.Log($"[DamageReceiver] Lower-body invulnerability ignored hit on {fighter?.name}");
				#endif
				return;
			}
			bool blocked = false;
			var cfg = Systems.RuntimeConfig.Instance;
			if (cfg != null)
			{
				blocked = GuardEvaluator.CanBlockTimed(fighter, fighter.PendingCommands.block, fighter.IsGrounded(), fighter.IsCrouching, info.level, cfg.blockMaxHoldSeconds) && info.canBeBlocked;
				// 若玩家持續按住超過窗口，觸發一次鎖定冷卻
				if (!blocked && fighter.PendingCommands.block && fighter.GetBlockHeldSeconds() > cfg.blockMaxHoldSeconds)
				{
					// 設置鎖定時間（通過外部方法避免直接引用私有欄位）
					fighter.gameObject.SendMessage("__LockBlockForSeconds", cfg.blockCooldownSeconds, SendMessageOptions.DontRequireReceiver);
				}
			}
			var resources = fighter.GetComponent<Fighter.Core.FighterResources>();
			int before = fighter.currentHealth;
			if (!blocked)
			{
				#if UNITY_EDITOR
				if (attacker && attacker.team == FightingGame.Combat.Actors.FighterTeam.AI && fighter.team == FightingGame.Combat.Actors.FighterTeam.Player)
				{
					Debug.Log($"[AI->Player] HIT dmg={info.damage} hp={before}->{Mathf.Max(0, before - Mathf.Max(0, info.damage))}");
				}
				if (attacker && attacker.team == FightingGame.Combat.Actors.FighterTeam.Player && fighter.team == FightingGame.Combat.Actors.FighterTeam.AI)
				{
					Debug.Log($"[Player->AI] HIT dmg={info.damage} hp={before}->{Mathf.Max(0, before - Mathf.Max(0, info.damage))}");
				}
				Debug.Log($"[DamageReceiver] HIT {attacker?.name} -> {fighter?.name} dmg={info.damage} hp={before}->{Mathf.Max(0, before - Mathf.Max(0, info.damage))}");
				#endif
				int damage = Mathf.Max(0, info.damage);
				if (resources)
				{
					resources.DecreaseHealth(damage);
				}
				else
				{
					fighter.currentHealth = Mathf.Max(0, fighter.currentHealth - damage);
				}
				if (animator && animator.runtimeAnimatorController)
				{
					animator.SetTrigger("Hit");
				}
				fighter.MarkHitConfirmed(info.hitstopOnHit);
				Systems.CameraShaker.Instance?.Shake(0.1f, info.hitstopOnHit);
				// Meter 獎勵僅在開啟特殊系統時才生效
				if (attacker && Systems.RuntimeConfig.Instance != null && Systems.RuntimeConfig.Instance.specialsEnabled)
				{
					var atkRes = attacker.GetComponent<Fighter.Core.FighterResources>();
					if (atkRes && info.meterOnHit > 0) atkRes.IncreaseMeter(info.meterOnHit);
				}
				Systems.DamageBus.Raise(Mathf.Max(0, info.damage), fighter.transform.position + new Vector3(0f, 1f, 0f), false, attacker, fighter);
				var def = fighter.HRoot != null ? fighter.HRoot.Defense : null;
				if (def != null)
				{
					if (info.knockdownKind != KnockdownKind.None)
					{
						def.BeginDownedFlat(info.knockdownKind == KnockdownKind.Hard, info.hitstun);
					}
					else
					{
						def.BeginHitstunFlat(info.hitstun);
					}
				}
			}
			else
			{
				#if UNITY_EDITOR
				if (attacker && attacker.team == FightingGame.Combat.Actors.FighterTeam.AI && fighter.team == FightingGame.Combat.Actors.FighterTeam.Player)
				{
					Debug.Log($"[AI->Player] BLOCKED level={info.level}");
				}
				if (attacker && attacker.team == FightingGame.Combat.Actors.FighterTeam.Player && fighter.team == FightingGame.Combat.Actors.FighterTeam.AI)
				{
					Debug.Log($"[Player->AI] BLOCKED level={info.level}");
				}
				Debug.Log($"[DamageReceiver] BLOCK {attacker?.name} -> {fighter?.name} level={info.level}");
				#endif
				fighter.MarkHitConfirmed(info.hitstopOnBlock);
				Systems.CameraShaker.Instance?.Shake(0.05f, info.hitstopOnBlock);
				if (attacker && Systems.RuntimeConfig.Instance != null && Systems.RuntimeConfig.Instance.specialsEnabled)
				{
					var atkRes = attacker.GetComponent<Fighter.Core.FighterResources>();
					if (atkRes && info.meterOnBlock > 0) atkRes.IncreaseMeter(info.meterOnBlock);
				}
				Systems.DamageBus.Raise(0, fighter.transform.position + new Vector3(0f, 1f, 0f), true, attacker, fighter);
				var def = fighter.HRoot != null ? fighter.HRoot.Defense : null;
				if (def != null)
				{
					bool crouchingGuard = fighter.IsCrouching || fighter.PendingCommands.crouch;
					def.Flat.ChangeState(new FightingGame.Combat.State.HFSM.DefenseDomainState.BlockFlat(fighter, crouchingGuard));
				}
			}

			if (fighter.currentHealth < before)
			{
				fighter.NotifyDamagedForReceivers(attacker);
			}
		}
	}
}