using UnityEngine;
using FightingGame.Combat;
using Data;
using Systems;

namespace FightingGame.Combat.Actors
{
	public partial class FighterActor
	{
		public void NotifyStateChanged()
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log($"[FighterActor] StateChanged name={GetCurrentStateName()} move={debugMoveName ?? ""} by {name}");
#endif
			OnStateChanged?.Invoke(GetCurrentStateName(), debugMoveName ?? "");
		}
		public string GetCurrentStateName()
		{
			return HMachine != null && HMachine.Current != null
				? HMachine.Current.Name
				: string.Empty;
		}

		public void EnterAttackHFSM(string trigger)
		{
			var off = HRoot.Offense;
			if (off != null)
			{
				FightingGame.Combat.State.HFSM.AttackState target = null;
				bool grounded = IsGrounded();
				if (grounded)
				{
					target = (trigger == "Light") ? off.GroundLight : off.GroundHeavy;
				}
				else
				{
					target = (trigger == "Light") ? off.AirLight : off.AirHeavy;
				}
				HMachine.ChangeState(target);
				return;
			}
			var locomotionState = HRoot.Locomotion;
			FightingGame.Combat.State.HFSM.AttackState attackTargetState = null;
			if (IsGrounded())
			{
				attackTargetState = (trigger == "Light") ? locomotionState.Grounded.AttackLight : locomotionState.Grounded.AttackHeavy;
			}
			else
			{
				attackTargetState = (trigger == "Light") ? locomotionState.Air.AirLight : locomotionState.Air.AirHeavy;
			}
			HMachine.ChangeState(attackTargetState);
		}
		public void EnterThrowHFSM()
		{
			var off = HRoot.Offense;
			if (off != null)
			{
				HMachine.ChangeState(off.Throw);
				return;
			}
			HMachine.ChangeState(HRoot.Locomotion.Grounded.Throw);
		}
		public void EnterHitstunHFSM(float seconds)
		{
			var def = HRoot.Defense;
			if (def != null)
			{
				var hs = def.Hitstun;
				hs.Begin(seconds);
				HMachine.ChangeState(hs);
				return;
			}
			var locomotionState = HRoot.Locomotion;
			var hitstunState = IsGrounded() ? locomotionState.Grounded.Hitstun : locomotionState.Air.Hitstun;
			hitstunState.Begin(seconds);
			HMachine.ChangeState(hitstunState);
		}
		public void EnterDownedHFSM(bool hard, float duration)
		{
			var def = HRoot.Defense;
			if (def != null)
			{
				var dn = def.Downed;
				dn.Begin(hard, duration);
				HMachine.ChangeState(dn);
				TryShowWakeupHint();
				return;
			}
			var locomotionState = HRoot.Locomotion;
			var downedState = locomotionState.Grounded.Downed;
			downedState.Begin(hard, duration);
			HMachine.ChangeState(downedState);
			TryShowWakeupHint();
		}
		void TryShowWakeupHint()
		{
			/* no-op: UI hint removed */
		}

		public void Move(float x)
		{
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController)
			{
				locomotionController.Move(x);
			}
			else
			{
				rigidbody2D.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rigidbody2D.velocity.y);
			}
		}
		public void HaltHorizontal()
		{
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController)
			{
				locomotionController.HaltHorizontal();
			}
			else
			{
				rigidbody2D.velocity = new Vector2(0, rigidbody2D.velocity.y);
			}
		}
		public void AirMove(float x)
		{
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController)
			{
				locomotionController.AirMove(x);
			}
			else
			{
				rigidbody2D.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rigidbody2D.velocity.y);
			}
		}
		public bool CanJump()
		{
			if (!jumpRule)
			{
				jumpRule = GetComponent<Fighter.Core.JumpRule>();
			}
			return jumpRule ? jumpRule.CanPerformJump(IsGrounded()) : IsGrounded();
		}
		public void DoJump()
		{
			if (jumpRule)
			{
				jumpRule.NotifyJumpExecuted(IsGrounded());
			}
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController)
			{
				locomotionController.Jump();
			}
			else
			{
				rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, stats != null ? stats.jumpForce : 12f);
				if (AnimatorReady())
				{
					animator.SetTrigger("Jump");
				}
			}
		}

		public void TriggerAttack(string trigger)
		{
			#if UNITY_EDITOR
			Debug.Log($"[FighterActor] TriggerAttack {trigger} by {name} state={GetCurrentStateName()}");
			#endif
			var attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor)
			{
				attackExecutor.TriggerAttack(trigger);
				NotifyStateChanged();
				return;
			}

			debugMoveName = trigger;
			if (actionSet != null)
			{
				CurrentMove = actionSet.Get(trigger);
			}

			var resources = GetComponent<Fighter.Core.FighterResources>();
			if (CurrentMove != null && CurrentMove.meterCost > 0)
			{
				bool hasSufficientMeter = resources
					? resources.DecreaseMeter(CurrentMove.meterCost)
					: (meter >= CurrentMove.meterCost ? (meter -= CurrentMove.meterCost) >= 0 : false);
				if (!hasSufficientMeter)
				{
					CurrentMove = null;
					NotifyStateChanged();
					return;
				}
			}

			if (AnimatorReady())
			{
				animator.SetTrigger(trigger);
			}
			NotifyStateChanged();
		}

		public void ExecuteHeal(string trigger)
		{
			var healExecutor = GetComponent<Fighter.Core.HealExecutor>();
			if (healExecutor)
			{
				healExecutor.Execute(trigger);
				NotifyStateChanged();
				return;
			}

			debugMoveName = trigger;
			if (actionSet != null)
			{
				CurrentMove = actionSet.Get(trigger);
			}

			var resources = GetComponent<Fighter.Core.FighterResources>();
			if (CurrentMove != null && CurrentMove.meterCost > 0)
			{
				bool hasSufficientMeter = resources
					? resources.DecreaseMeter(CurrentMove.meterCost)
					: (meter >= CurrentMove.meterCost ? (meter -= CurrentMove.meterCost) >= 0 : false);
				if (!hasSufficientMeter)
				{
					CurrentMove = null;
					NotifyStateChanged();
					return;
				}
			}

			if (CurrentMove != null && CurrentMove.healAmount > 0)
			{
				if (resources)
				{
					resources.IncreaseHealth(CurrentMove.healAmount);
				}
				else
				{
					int maxHp = stats != null ? stats.maxHealth : 100;
					currentHealth = Mathf.Clamp(currentHealth + CurrentMove.healAmount, 0, maxHp);
				}
			}

			if (AnimatorReady())
			{
				animator.SetTrigger(trigger);
			}
			NotifyStateChanged();
		}

		public void ClearCurrentMove()
		{
			CurrentMove = null;
			debugMoveName = null;
			NotifyStateChanged();
		}
		public void SetAttackActive(bool on)
		{
			var attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor)
			{
#if UNITY_EDITOR
				Debug.Log($"[FighterActor] SetAttackActive({on}) by {name} move={CurrentMove?.triggerName}");
#endif
				attackExecutor.SetAttackActive(on);
				return;
			}
			debugHitActive = on;
			if (on)
			{
				attackInstanceId++;
				hitVictims.Clear();
				hitStopApplied = false;
			}
			if (hitboxes == null)
			{
				return;
			}
			foreach (var hitbox in hitboxes)
			{
				if (hitbox != null)
				{
					hitbox.active = on;
				}
			}
			if (spriteRendererVisual)
			{
				spriteRendererVisual.color = on ? Color.yellow : spriteRendererDefaultColor;
			}
		}
		public void RequestComboCancel(string trigger)
		{
			pendingCancelTrigger = trigger;
			hasPendingCancel = true;
		}
		public bool TryConsumeComboCancel(out string trigger)
		{
			trigger = null;
			if (!hasPendingCancel)
			{
				return false;
			}
			hasPendingCancel = false;
			trigger = pendingCancelTrigger;
			return true;
		}
		public void SetCurrentMove(Data.CombatActionDefinition md)
		{
			CurrentMove = md;
		}
		public void IncrementAttackInstanceId()
		{
			attackInstanceId++;
		}
		public void ClearHitVictimsSet()
		{
			hitVictims.Clear();
		}
		public void SetActiveColor(bool on)
		{
			if (hasSpriteRendererVisual)
			{
				spriteRendererVisual.color = on ? Color.yellow : spriteRendererDefaultColor;
			}
		}
		public void AddExternalImpulse(float dx)
		{
			externalImpulseX += dx;
		}
		public bool CanBlock(DamageInfo info)
		{
			return GuardEvaluator.CanBlock(PendingCommands.block, IsGrounded(), IsCrouching, info.level);
		}
		public void SetUpperBodyInvuln(bool on)
		{
			UpperBodyInvuln = on;
		}
		public void SetLowerBodyInvuln(bool on)
		{
			LowerBodyInvuln = on;
		}
		public void SetUpperLowerInvuln(bool up, bool low)
		{
			var res = GetComponent<Fighter.Core.FighterResources>();
			if (res)
			{
				res.SetUpperBodyInvuln(up);
				res.SetLowerBodyInvuln(low);
			}
			else
			{
				UpperBodyInvuln = up;
				LowerBodyInvuln = low;
			}
		}
		public void TakeHit(DamageInfo info, FighterActor attacker)
		{
			var recv = GetComponent<Fighter.Core.DamageReceiver>();
			if (recv)
			{
#if UNITY_EDITOR
				Debug.Log($"[FighterActor] TakeHit by {attacker?.name} on {name} dmg={info.damage} level={info.level} canBlock={info.canBeBlocked}");
#endif
				recv.TakeHit(info, attacker);
				return;
			}
		}
		public event System.Action<float> OnHitConfirm;
		public void OnHitConfirmedLocal(float seconds)
		{
			var attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor)
			{
				attackExecutor.OnHitConfirmedLocal(seconds);
				return;
			}
			if (hitStopApplied) return;
			hitStopApplied = true;
			int frames = FrameClock.SecondsToFrames(seconds);
			FreezeFrames(frames);
			Systems.CameraShaker.Instance?.Shake(0.1f, seconds);
			OnHitConfirm?.Invoke(seconds);
		}
		public void NotifyDamagedForReceivers(FighterActor attacker)
		{
			OnDamaged?.Invoke(this);
			OnAnyDamage?.Invoke(attacker, this);
		}
		public void MarkHitConfirmed(float duration = 0.35f)
		{
			hitConfirmTimer = Mathf.Max(hitConfirmTimer, duration);
		}
		public bool HasRecentHitConfirm()
		{
			return hitConfirmTimer > 0f;
		}
        void AutoFaceOpponent()
		{
			if (!opponent)
			{
				return;
			}
			bool shouldFaceRight = transform.position.x <= opponent.position.x;
			if (shouldFaceRight != facingRight)
			{
				facingRight = shouldFaceRight;
				var s = transform.localScale;
				s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1);
				transform.localScale = s;
			}
		}
		public bool IsGrounded()
		{
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController)
			{
				return locomotionController.IsGrounded(groundMask);
			}
			if (bodyCollider == null)
			{
				return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
			}
			var b = bodyCollider.bounds;
			Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
			Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
			return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
		}
		public void SetAnimatorBool(string key, bool v)
		{
			if (AnimatorReady())
			{
				animator.SetBool(key, v);
			}
		}
		public void SetDebugMoveName(string name)
		{
			debugMoveName = name;
		}
		public void SetDebugHitActive(bool on)
		{
			debugHitActive = on;
		}
		public bool TryConsumeDashRequest(out bool isBack)
		{
			if (!dashRequested)
			{
				isBack = false;
				return false;
			}
			isBack = dashBack;
			dashRequested = false;
			return true;
		}
		public void RequestDash(bool back)
		{
			dashRequested = true;
			dashBack = back;
		}
		public void ApplyThrowOn(FighterActor victim)
		{
			if (victim == null)
			{
				return;
			}
			var info = new DamageInfo
			{
				damage = 6,
				hitstun = 0.2f,
				blockstun = 0f,
				canBeBlocked = false,
				hitstopOnHit = 0.08f,
				pushbackOnHit = 0.2f,
				pushbackOnBlock = 0.0f,
				level = HitLevel.Mid,
				knockdownKind = KnockdownKind.Soft,
			};
			victim.TakeHit(info, this);
			victim.StartUkemiWindow(0.4f);
		}
		public bool CanHitTarget(FighterActor target)
		{
			if (target == null || target == this)
			{
				return false;
			}
			if (hitVictims.Contains(target))
			{
				return false;
			}
			hitVictims.Add(target);
			return true;
		}
		public bool IsOpponentInThrowRange(float maxDist)
		{
			if (!opponent)
			{
				return false;
			}
			if (!IsGrounded())
			{
				return false;
			}
			var opponentController = opponent.GetComponent<FighterActor>();
			if (!opponentController || !opponentController.IsGrounded())
			{
				return false;
			}
			float dx = Mathf.Abs(opponent.position.x - transform.position.x);
			float dy = Mathf.Abs(opponent.position.y - transform.position.y);
			return dx <= maxDist && dy <= 1.0f;
		}

		// Added: tech/ukemi/dodge helpers and UI gating
		public void StartThrowTechWindow(float seconds)
		{
			throwTechWindow = Mathf.Max(throwTechWindow, seconds);
		}
		public bool WasTechTriggeredAndClear()
		{
			bool v = techTriggered;
			techTriggered = false;
			return v;
		}
		public void StartUkemiWindow(float seconds)
		{
			ukemiWindow = Mathf.Max(ukemiWindow, seconds);
		}
		public void ConsumeTech()
		{
			if (throwTechWindow > 0f)
			{
				techTriggered = true;
			}
		}
		public void StartDodge()
		{
			SetUpperLowerInvuln(true, true);
			float dur = stats != null ? stats.dodgeInvuln : 0.2f;
			if (dur > 0f)
			{
				StartCoroutine(DodgeInvulnCoroutine(dur));
			}
		}
		public void TryPerformDodgeTeleport(float inputX)
		{
			if (stats == null)
			{
				return;
			}
			float distance = Mathf.Max(0f, stats.dodgeTeleportDistance);
			if (distance <= 0f)
			{
				return;
			}
			float sign = 0f;
			if (inputX > 0.1f) sign = 1f; else if (inputX < -0.1f) sign = -1f;
			if (Mathf.Abs(sign) < 0.5f)
			{
				return; // no horizontal intent -> no teleport
			}
			Bounds b = bodyCollider ? bodyCollider.bounds : new Bounds(transform.position, new Vector3(1f, 2f, 0f));
			Vector2 size = new Vector2(b.size.x * 0.98f, b.size.y * 0.98f);
			Vector2 origin = b.center;
			Vector2 dir = new Vector2(sign, 0f);
			var hit = Physics2D.BoxCast(origin, size, 0f, dir, distance, groundMask);
			float allowed = distance;
			if (hit.collider != null)
			{
				allowed = Mathf.Max(0f, hit.distance - 0.01f);
			}
			if (allowed > 0f)
			{
				transform.position = new Vector3(transform.position.x + sign * allowed, transform.position.y, transform.position.z);
			}
			SetDodgeCooldown(stats.dodgeTeleportCooldown);
		}
		System.Collections.IEnumerator DodgeInvulnCoroutine(float seconds)
		{
			yield return new WaitForSeconds(seconds);
			SetUpperLowerInvuln(false, false);
		}
		public bool AnimatorIsTag(string tag)
		{
			return animator && animator.runtimeAnimatorController && animator.GetCurrentAnimatorStateInfo(0).IsTag(tag);
		}
		public void UpdateHurtboxEnable()
		{
			if (hurtboxes == null)
			{
				return;
			}
			bool grounded = IsGrounded();
			foreach (var hb in hurtboxes)
			{
				if (hb == null)
				{
					continue;
				}
				bool postureActive = grounded ? (IsCrouching ? hb.activeCrouching : hb.activeStanding) : hb.activeAirborne;
				bool regionInvuln = (hb.region == HurtRegion.Legs) ? LowerBodyInvuln : UpperBodyInvuln;
				hb.enabledThisFrame = postureActive && !regionInvuln;
			}
		}
	}
}