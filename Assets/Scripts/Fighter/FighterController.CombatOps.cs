using UnityEngine;
using Systems;

namespace FightingGame.Combat.Actors
{
	public partial class FighterActor
	{
		/// <summary>
		/// Notify that the hierarchical finite state machine state changed.
		/// ֪ͨ�������״̬����״̬�ѱ仯��
		/// </summary>
		public void NotifyStateChanged()
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log("[FighterActor] State Changed. " + "stateName=" + GetCurrentStateName() + " move=" + (DebugActionName ?? string.Empty) + " actorName=" + name);
#endif
			if (OnStateChanged != null)
			{
				OnStateChanged.Invoke(GetCurrentStateName(), DebugActionName ?? string.Empty);
			}
		}

		/// <summary>
		/// Get current state name of the hierarchical machine.
		/// ��ȡ�ֲ�״̬����ǰ״̬����
		/// </summary>
		public string GetCurrentStateName()
		{
			if (HMachine.Current != null)
			{
				return HMachine.Current.Name;
			}
			else
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Enter an attack state based on trigger and grounded status.
		/// ���ݴ������Ƿ��ڵ�����빥��״̬��
		/// </summary>
		public void EnterAttackHFSM(string triggerName)
		{
			State.HFSM.OffenseDomainState offenseDomain = HRoot.Offense;
			if (offenseDomain != null)
			{
				FightingGame.Combat.State.HFSM.AttackState targetStateFromOffenseRoot = null;
				bool isOnGround = IsGrounded();

				if (isOnGround)
				{
					if (triggerName == "Light")
					{
						targetStateFromOffenseRoot = offenseDomain.GroundLight;
					}
					else
					{
						targetStateFromOffenseRoot = offenseDomain.GroundHeavy;
					}
				}
				else
				{
					if (triggerName == "Light")
					{
						targetStateFromOffenseRoot = offenseDomain.AirLight;
					}
					else
					{
						targetStateFromOffenseRoot = offenseDomain.AirHeavy;
					}
				}

				HMachine.ChangeState(targetStateFromOffenseRoot);
				return;
			}

			State.HFSM.LocomotionState locomotionRoot = HRoot.Locomotion;
			FightingGame.Combat.State.HFSM.AttackState targetStateFromLocomotion = null;
			bool groundedState = IsGrounded();

			if (groundedState)
			{
				if (triggerName == "Light")
				{
					targetStateFromLocomotion = locomotionRoot.Grounded.AttackLight;
				}
				else
				{
					targetStateFromLocomotion = locomotionRoot.Grounded.AttackHeavy;
				}
			}
			else
			{
				if (triggerName == "Light")
				{
					targetStateFromLocomotion = locomotionRoot.Air.AirLight;
				}
				else
				{
					targetStateFromLocomotion = locomotionRoot.Air.AirHeavy;
				}
			}

			HMachine.ChangeState(targetStateFromLocomotion);
		}

		/// <summary>
		/// Enter throw state.
		/// ����Ͷ��״̬��
		/// </summary>
		public void EnterThrowHFSM()
		{
			State.HFSM.OffenseDomainState offenseDomain = HRoot.Offense;
			if (offenseDomain != null)
			{
				HMachine.ChangeState(offenseDomain.Throw);
				return;
			}

			State.HFSM.LocomotionState locomotionRoot = HRoot.Locomotion;
			HMachine.ChangeState(locomotionRoot.Grounded.Throw);
		}

		/// <summary>
		/// Enter hitstun with a specified duration in seconds.
		/// �����ܻ���ֱ״̬�����ó���ʱ�䣨�룩��
		/// </summary>
		public void EnterHitstunHFSM(float seconds)
		{
			State.HFSM.DefenseDomainState defenseDomain = HRoot.Defense;
			if (defenseDomain != null)
			{
				State.HFSM.HitstunState hitstun = defenseDomain.Hitstun;
				hitstun.SetTime(seconds);
				HMachine.ChangeState(hitstun);
				return;
			}

			State.HFSM.LocomotionState locomotionState = HRoot.Locomotion;
			State.HFSM.HitstunState hitstunState = null;
			if (IsGrounded())
			{
				hitstunState = locomotionState.Grounded.Hitstun;
			}
			else
			{
				hitstunState = locomotionState.Air.Hitstun;
			}

			hitstunState.SetTime(seconds);
			HMachine.ChangeState(hitstunState);
		}

		/// <summary>
		/// Enter downed state with parameters whether it is hard and duration.
		/// ���뵹��״̬��ָ���Ƿ�ΪӲ��������ʱ�䡣
		/// </summary>
		public void EnterDownedHFSM(bool hard, float duration)
		{
			State.HFSM.DefenseDomainState defenseDomain = HRoot.Defense;
			if (defenseDomain != null)
			{
				State.HFSM.DownedState downedState = defenseDomain.Downed;
				downedState.Begin(hard, duration);
				HMachine.ChangeState(downedState);
				TryShowWakeupHint();
				return;
			}

			State.HFSM.LocomotionState locomotionState = HRoot.Locomotion;
			State.HFSM.DownedState downedGroundedState = locomotionState.Grounded.Downed;
			downedGroundedState.Begin(hard, duration);
			HMachine.ChangeState(downedGroundedState);
			TryShowWakeupHint();
		}

		private void TryShowWakeupHint()
		{
			// intentionally left blank: UI hint removed
		}

		/// <summary>
		/// Move the actor horizontally. Input range is -1..1.
		/// ����ɫ��ˮƽ�������ƶ������뷶ΧΪ -1..1��
		/// </summary>
		public void Move(float horizontalInput)
		{
			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController != null)
			{
				locomotionController.Move(horizontalInput);
			}
			else
			{
				float speed = 6f;
				if (stats != null)
				{
					speed = stats.walkSpeed;
				}
				rigidbody2D.velocity = new Vector2(horizontalInput * speed, rigidbody2D.velocity.y);
			}
		}

		/// <summary>
		/// Immediately stop horizontal movement.
		/// ����ֹͣˮƽ�����˶���
		/// </summary>
		public void HaltHorizontal()
		{
			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController != null)
			{
				locomotionController.HaltHorizontal();
			}
			else
			{
				rigidbody2D.velocity = new Vector2(0f, rigidbody2D.velocity.y);
			}
		}

		/// <summary>
		/// Control horizontal movement while airborne.
		/// �ڿ��п���ˮƽ�ƶ���
		/// </summary>
		public void AirMove(float horizontalInput)
		{
			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController != null)
			{
				locomotionController.AirMove(horizontalInput);
			}
			else
			{
				float speed = 6f;
				if (stats != null)
				{
					speed = stats.walkSpeed;
				}
				rigidbody2D.velocity = new Vector2(horizontalInput * speed, rigidbody2D.velocity.y);
			}
		}

		/// <summary>
		/// Check whether the actor can perform a jump now.
		/// ����ɫ��ǰ�Ƿ������Ծ��
		/// </summary>
		public bool CanJump()
		{
			if (jumpRule == null)
			{
				jumpRule = GetComponent<Fighter.Core.JumpRule>();
			}

			if (jumpRule != null)
			{
				return jumpRule.CanPerformJump(IsGrounded());
			}

			return IsGrounded();
		}

		/// <summary>
		/// Execute the jump action.
		/// ִ����Ծ������
		/// </summary>
		public void DoJump()
		{
			if (jumpRule != null)
			{
				jumpRule.NotifyJumpExecuted(IsGrounded());
			}

			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController != null)
			{
				locomotionController.Jump();
			}
			else
			{
				float verticalForce = 12f;
				if (stats != null)
				{
					verticalForce = stats.jumpForce;
				}

				rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, verticalForce);

				if (AnimatorReady())
				{
					animator.SetTrigger("Jump");
				}
			}
		}

		/// <summary>
		/// Trigger an attack by name.
		/// �������ƴ�������������
		/// </summary>
		public void TriggerAttack(string triggerName)
		{
#if UNITY_EDITOR
			Debug.Log("[FighterActor] Trigger Attack. triggerName=" + triggerName + " actorName=" + name + " state=" + GetCurrentStateName());
#endif

			Fighter.Core.CriticalAttackExecutor attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor != null)
			{
				attackExecutor.TriggerAttack(triggerName);
				NotifyStateChanged();
				return;
			}

			DebugActionName = triggerName;

			if (actionSet != null)
			{
				CurrentAction = actionSet.Get(triggerName);
			}

			Fighter.Core.FighterResources resources = GetComponent<Fighter.Core.FighterResources>();
			if (CurrentAction != null && CurrentAction.meterCost > 0)
			{
				bool hasSufficientMeter = false;
				if (resources != null)
				{
					hasSufficientMeter = resources.DecreaseMeter(CurrentAction.meterCost);
				}
				else
				{
					if (meter >= CurrentAction.meterCost)
					{
						meter -= CurrentAction.meterCost;
						hasSufficientMeter = true;
					}
					else
					{
						hasSufficientMeter = false;
					}
				}

				if (!hasSufficientMeter)
				{
					CurrentAction = null;
					NotifyStateChanged();
					return;
				}
			}

			if (AnimatorReady())
			{
				animator.SetTrigger(triggerName);
			}

			NotifyStateChanged();
		}

		/// <summary>
		/// Execute a healing action by name.
		/// ִ�����ƶ�����
		/// </summary>
		public void ExecuteHeal(string triggerName)
		{
			Fighter.Core.HealExecutor healExecutor = GetComponent<Fighter.Core.HealExecutor>();
			if (healExecutor != null)
			{
				healExecutor.Execute(triggerName);
				NotifyStateChanged();
				return;
			}

			DebugActionName = triggerName;
			if (actionSet != null)
			{
				CurrentAction = actionSet.Get(triggerName);
			}

			Fighter.Core.FighterResources resources = GetComponent<Fighter.Core.FighterResources>();
			if (CurrentAction != null && CurrentAction.meterCost > 0)
			{
				bool hasSufficientMeter = false;
				if (resources != null)
				{
					hasSufficientMeter = resources.DecreaseMeter(CurrentAction.meterCost);
				}
				else
				{
					if (meter >= CurrentAction.meterCost)
					{
						meter -= CurrentAction.meterCost;
						hasSufficientMeter = true;
					}
					else
					{
						hasSufficientMeter = false;
					}
				}

				if (!hasSufficientMeter)
				{
					CurrentAction = null;
					NotifyStateChanged();
					return;
				}
			}

			if (CurrentAction != null && CurrentAction.healAmount > 0)
			{
				if (resources != null)
				{
					resources.IncreaseHealth(CurrentAction.healAmount);
				}
				else
				{
					int maximumHealth = 100;
					if (stats != null)
					{
						maximumHealth = stats.maxHealth;
					}
					currentHealth = Mathf.Clamp(currentHealth + CurrentAction.healAmount, 0, maximumHealth);
				}
			}

			if (AnimatorReady())
			{
				animator.SetTrigger(triggerName);
			}

			NotifyStateChanged();
		}

		/// <summary>
		/// Clear the current action and debug action name.
		/// �����ǰ��������Զ�������
		/// </summary>
		public void ClearCurrentMove()
		{
			CurrentAction = null;
			DebugActionName = null;
			NotifyStateChanged();
		}

		/// <summary>
		/// Enable or disable attack hitboxes and related effects.
		/// ���û���ù��������估���Ч����
		/// </summary>
		public void SetAttackActive(bool on)
		{
			Fighter.Core.CriticalAttackExecutor attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor != null)
			{
#if UNITY_EDITOR
				Debug.Log("[FighterActor] Set Attack Active. active=" + on + " actorName=" + name + " moveName=" + (CurrentAction != null ? CurrentAction.triggerName : string.Empty));
#endif
				attackExecutor.SetAttackActive(on);
				return;
			}

			DebugHitActive = on;
			if (on)
			{
				hitVictims.Clear();
				hitStopApplied = false;
			}

			if (hitboxes == null)
			{
				return;
			}

			foreach (Hitbox hitbox in hitboxes)
			{
				if (hitbox != null)
				{
					hitbox.active = on;
				}
			}

			if (spriteRendererVisual != null)
			{
				if (on)
				{
					spriteRendererVisual.color = Color.yellow;
				}
				else
				{
					spriteRendererVisual.color = spriteRendererDefaultColor;
				}
			}
		}

		/// <summary>
		/// Request a combo cancel to be consumed later.
		/// ��������ȡ��ָ�
		/// </summary>
		public void RequestComboCancel(string triggerName)
		{
			pendingCancelTrigger = triggerName;
			hasPendingCancel = true;
		}

		/// <summary>
		/// Try to consume a previously requested combo cancel.
		/// ��������֮ǰ���������ȡ����
		/// </summary>
		public bool TryConsumeComboCancel(out string triggerName)
		{
			triggerName = null;
			if (!hasPendingCancel)
			{
				return false;
			}
			hasPendingCancel = false;
			triggerName = pendingCancelTrigger;
			return true;
		}

		/// <summary>
		/// Set the current action directly.
		/// ֱ�����õ�ǰ������
		/// </summary>
		public void SetCurrentMove(Data.CombatActionDefinition actionData)
		{
			CurrentAction = actionData;
		}

		/// <summary>
		/// Clear the set of hit victims.
		/// ��������е�Ŀ�꼯�ϡ�
		/// </summary>
		public void ClearHitVictimsSet()
		{
			hitVictims.Clear();
		}

		/// <summary>
		/// Set visible indicator color when active or not.
		/// ���ü���ʱ�Ŀɼ�ָʾ��ɫ��
		/// </summary>
		public void SetActiveColor(bool on)
		{
			if (hasSpriteRendererVisual)
			{
				if (on)
				{
					spriteRendererVisual.color = Color.yellow;
				}
				else
				{
					spriteRendererVisual.color = spriteRendererDefaultColor;
				}
			}
		}

		/// <summary>
		/// Add external horizontal impulse to be applied by physics step.
		/// �����ⲿˮƽ��������������Ӧ�á�
		/// </summary>
		public void AddExternalImpulse(float deltaX)
		{
			externalImpulseX += deltaX;
		}

		/// <summary>
		/// Determine whether this actor can block given damage information.
		/// �жϽ�ɫ�Ƿ���Զ�ָ�����˺���Ϣ���и񵲡�
		/// </summary>
		public bool CanBlock(DamageInfo info)
		{
			return GuardEvaluator.CanBlock(PendingCommands.block, IsGrounded(), IsCrouching, info.level);
		}

		/// <summary>
		/// Set upper body invulnerability flag.
		/// �����ϰ����޵б�־��
		/// </summary>
		public void SetUpperBodyInvulnerable(bool on)
		{
			UpperBodyInvulnerable = on;
		}

		/// <summary>
		/// Set lower body invulnerability flag.
		/// �����°����޵б�־��
		/// </summary>
		public void SetLowerBodyInvulnerable(bool on)
		{
			LowerBodyInvulnerable = on;
		}

		/// <summary>
		/// Set both upper and lower body invulnerabilities.
		/// ͬʱ�����ϰ������°�����޵�״̬��
		/// </summary>
		public void SetUpperLowerInvulnerable(bool upper, bool lower)
		{
			Fighter.Core.FighterResources resources = GetComponent<Fighter.Core.FighterResources>();
			if (resources != null)
			{
				resources.SetUpperBodyInvulnerability(upper);
				resources.SetLowerBodyInvulnerability(lower);
			}
			else
			{
				UpperBodyInvulnerable = upper;
				LowerBodyInvulnerable = lower;
			}
		}

		/// <summary>
		/// Apply a hit to this actor.
		/// �Ըý�ɫӦ��һ���ܵ������С�
		/// </summary>
		public void TakeHit(DamageInfo info, FighterActor attacker)
		{
			Fighter.Core.DamageReceiver damageReceiver = GetComponent<Fighter.Core.DamageReceiver>();
			if (damageReceiver != null)
			{
#if UNITY_EDITOR
				Debug.Log("[FighterActor] Take Hit. attackerName=" + (attacker != null ? attacker.name : string.Empty) + " targetName=" + name + " damage=" + info.damage + " level=" + info.level + " canBeBlocked=" + info.canBeBlocked);
#endif
				damageReceiver.TakeHit(info, attacker);
				return;
			}
		}

		public event System.Action<float> OnHitConfirm;

		/// <summary>
		/// Local confirmation of hit to apply freeze frames and camera shake.
		/// ����ȷ�����У����ڶ���֡�뾵ͷ������
		/// </summary>
		public void OnHitConfirmedLocal(float seconds)
		{
			Fighter.Core.CriticalAttackExecutor attackExecutor = GetComponent<Fighter.Core.CriticalAttackExecutor>();
			if (attackExecutor != null)
			{
				attackExecutor.OnHitConfirmedLocal(seconds);
				return;
			}

			if (hitStopApplied)
			{
				return;
			}

			hitStopApplied = true;
			int frames = FrameClock.SecondsToFrames(seconds);
			FreezeFrames(frames);
			if (Systems.CameraShaker.Instance != null)
			{
				Systems.CameraShaker.Instance.Shake(0.1f, seconds);
			}

			if (OnHitConfirm != null)
			{
				OnHitConfirm.Invoke(seconds);
			}
		}

		/// <summary>
		/// Notify other systems that this actor was damaged by the attacker.
		/// ֪ͨ����ϵͳ�ý�ɫ��������������˺���
		/// </summary>
		public void NotifyDamagedForReceivers(FighterActor attacker)
		{
			if (OnDamaged != null)
			{
				OnDamaged.Invoke(this);
			}
			if (OnAnyDamage != null)
			{
				OnAnyDamage.Invoke(attacker, this);
			}
		}

		/// <summary>
		/// Mark that a hit confirm recently occurred for UI or logic checks.
		/// ������һ������ȷ�ϣ����� UI ���߼���顣
		/// </summary>
		public void MarkHitConfirmed(float duration)
		{
			float minimumDuration = 0.35f;
			hitConfirmTimer = Mathf.Max(hitConfirmTimer, minimumDuration);
		}

		/// <summary>
		/// Check if there is a recent hit confirm timer.
		/// ����Ƿ�������������ȷ�ϼ�ʱ��
		/// </summary>
		public bool HasRecentHitConfirm()
		{
			return hitConfirmTimer > 0f;
		}

		private void AutoFaceOpponent()
		{
			if (opponent == null)
			{
				return;
			}

			bool shouldFaceRight = transform.position.x <= opponent.position.x;
			if (shouldFaceRight != facingRight)
			{
				facingRight = shouldFaceRight;
				Vector3 localScale = transform.localScale;
				float absoluteX = Mathf.Abs(localScale.x);
				localScale.x = absoluteX * (facingRight ? 1f : -1f);
				transform.localScale = localScale;
			}
		}

		/// <summary>
		/// Determine whether the actor is currently grounded.
		/// �жϽ�ɫ��ǰ�Ƿ��ŵء�
		/// </summary>
		public bool IsGrounded()
		{
			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController != null)
			{
				return locomotionController.IsGrounded(groundMask);
			}

			if (bodyCollider == null)
			{
				return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
			}

			Bounds bound = bodyCollider.bounds;
			Vector2 boxCenter = new Vector2(bound.center.x, bound.min.y - 0.05f);
			Vector2 boxSize = new Vector2(bound.size.x * 0.9f, 0.1f);
			Collider2D overlap = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask);
			if (overlap != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Set an animator boolean parameter by key.
		/// ���� Animator �Ĳ���������
		/// </summary>
		public void SetAnimatorBool(string key, bool value)
		{
			if (AnimatorReady())
			{
				animator.SetBool(key, value);
			}
		}

		public void SetDebugMoveName(string moveName)
		{
			DebugActionName = moveName;
		}

		public void SetDebugHitActive(bool on)
		{
			DebugHitActive = on;
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

		/// <summary>
		/// Apply throw effects on the victim actor.
		/// �Ա�����ɫʩ��Ͷ��Ч����
		/// </summary>
		public void ApplyThrowOn(FighterActor victim)
		{
			if (victim == null)
			{
				return;
			}

			DamageInfo damageInformation = new DamageInfo();
			damageInformation.damage = 6;
			damageInformation.hitstun = 0.2f;
			damageInformation.blockstun = 0f;
			damageInformation.canBeBlocked = false;
			damageInformation.hitstopOnHit = 0.08f;
			damageInformation.pushbackOnHit = 0.2f;
			damageInformation.pushbackOnBlock = 0.0f;
			damageInformation.level = HitLevel.Mid;
			damageInformation.knockdownKind = KnockdownKind.Soft;

			victim.TakeHit(damageInformation, this);
			victim.StartUkemiWindow(0.4f);
		}

		public bool CanHitTarget(FighterActor target)
		{
			if (target == null)
			{
				return false;
			}

			if (target == this)
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

		public bool IsOpponentInThrowRange(float maximumDistance)
		{
			if (opponent == null)
			{
				return false;
			}

			if (!IsGrounded())
			{
				return false;
			}

			FighterActor opponentActor = opponent.GetComponent<FighterActor>();
			if (opponentActor == null)
			{
				return false;
			}

			if (!opponentActor.IsGrounded())
			{
				return false;
			}

			float deltaX = Mathf.Abs(opponent.position.x - transform.position.x);
			float deltaY = Mathf.Abs(opponent.position.y - transform.position.y);
			if (deltaX <= maximumDistance && deltaY <= 1.0f)
			{
				return true;
			}

			return false;
		}

		// Added: tech/ukemi/dodge helpers and UI gating
		public void StartThrowTechWindow(float seconds)
		{
			throwTechWindow = Mathf.Max(throwTechWindow, seconds);
		}

		public bool WasTechTriggeredAndClear()
		{
			bool untriggered = techTriggered;
			techTriggered = false;
			return untriggered;
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
			SetUpperLowerInvulnerable(true, true);
			float duration = 0.2f;
			if (stats != null)
			{
				duration = stats.dodgeInvulnerable;
			}

			if (duration > 0f)
			{
				StartCoroutine(DodgeInvulnerableCoroutine(duration));
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
			if (inputX > 0.1f)
			{
				sign = 1f;
			}
			else if (inputX < -0.1f)
			{
				sign = -1f;
			}

			if (Mathf.Abs(sign) < 0.5f)
			{
				// no horizontal intent -> no teleport
				return;
			}

			Bounds bound = bodyCollider != null ? bodyCollider.bounds : new Bounds(transform.position, new Vector3(1f, 2f, 0f));
			Vector2 size = new Vector2(bound.size.x * 0.98f, bound.size.y * 0.98f);
			Vector2 origin = bound.center;
			Vector2 direction = new Vector2(sign, 0f);
			RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, distance, groundMask);
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

		private System.Collections.IEnumerator DodgeInvulnerableCoroutine(float seconds)
		{
			yield return new WaitForSeconds(seconds);
			SetUpperLowerInvulnerable(false, false);
		}

		public bool AnimatorIsTag(string tag)
		{
			if (animator == null)
			{
				return false;
			}

			if (animator.runtimeAnimatorController == null)
			{
				return false;
			}

			return animator.GetCurrentAnimatorStateInfo(0).IsTag(tag);
		}

		public void UpdateHurtboxEnable()
		{
			if (hurtboxes == null)
			{
				return;
			}

			bool grounded = IsGrounded();
			foreach (Hurtbox hurtbox in hurtboxes)
			{
				if (hurtbox == null)
				{
					continue;
				}

				bool postureActive;
				if (grounded)
				{
					if (IsCrouching)
					{
						postureActive = hurtbox.activeCrouching;
					}
					else
					{
						postureActive = hurtbox.activeStanding;
					}
				}
				else
				{
					postureActive = hurtbox.activeAirborne;
				}

				bool regionInvulnerable;
				if (hurtbox.region == HurtRegion.Legs)
				{
					regionInvulnerable = LowerBodyInvulnerable;
				}
				else
				{
					regionInvulnerable = UpperBodyInvulnerable;
				}

				hurtbox.enabledThisFrame = postureActive && !regionInvulnerable;
			}
		}
	}
}
