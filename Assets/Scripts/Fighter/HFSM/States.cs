using UnityEngine;

namespace FightingGame.Combat.State.HFSM
{
	public class RootState : HState
	{
		// New domain wrappers (step 1): keep Locomotion mapping for back-compat
		public MovementDomainState Movement { get; private set; }
		public OffenseDomainState Offense { get; private set; }
		public DefenseDomainState Defense { get; private set; }
		/// <summary>
		/// Backward‑compatibility mapping: exposes Locomotion the same way as before by forwarding to Movement.Locomotion.
		/// 保持向后兼容：对外继续暴露 Locomotion，内部转发到 Movement.Locomotion。
		/// </summary>
		public LocomotionState Locomotion
		{
			get
			{
				return Movement != null ? Movement.Locomotion : null;
			}
		}

		public RootState(FightingGame.Combat.Actors.FighterActor actor) : base(actor)
		{
			Movement = new MovementDomainState(actor, this);
			Offense = new OffenseDomainState(actor, this);
			Defense = new DefenseDomainState(actor, this);
		}

		public override void OnEnter()
		{
			if (Locomotion != null)
			{
				Locomotion.OnEnter();
			}
		}

		/// <summary>
		/// Ticks the HFSM with strict domain arbitration:
		/// 1) Defense if blocking/dodging; 2) Continue Offense if active; 3) Trigger air offense when airborne + attack button; 4) Otherwise tick Movement.
		/// 以严格优先级驱动状态：1) 防御（格挡/回避） 2) 进攻持续 3) 空中攻击触发 4) 否则移动。
		/// </summary>
		public override void OnTick()
		{
			Actors.FighterCommands pendingCommands = Fighter.PendingCommands; // no abbreviations / 不使用缩写

			// Defense domain takes precedence when block is held
			// 防御域优先：按住格挡时优先进入防御
			if (pendingCommands.block)
			{
				// Route to leaf states for defense instead of direct flat instantiation
				Fighter.HMachine.ChangeState(pendingCommands.crouch ? (HState)Defense.BlockCrouch : (HState)Defense.BlockStand);
				return;
			}

			// Dodge if requested and allowed
			// 当请求回避且允许时进入回避
			if (pendingCommands.dodge && Fighter.CanDodge())
			{
				Fighter.HMachine.ChangeState(Defense.Dodge);
				return;
			}

			// If Offense is currently active, keep ticking it
			// 若进攻域处于激活态，继续推进
			if (Offense.IsActive())
			{
				Offense.TickDomain();
				return;
			}

			// Air offense trigger: airborne + Light/Heavy (delegate via unified API)
			// 空中攻击触发：在空中且按下轻/重攻击（通过统一入口函数路由）
			if (!Fighter.IsGrounded() && (pendingCommands.light || pendingCommands.heavy))
			{
				Fighter.EnterAttackHFSM(pendingCommands.light ? "Light" : "Heavy");
				return;
			}

			// Fallback to Movement domain
			// 回退到移动域
			Movement.TickDomain();
		}
	}

	public class MovementDomainState : HState
	{
		public LocomotionState Locomotion { get; private set; }
		public FightingGame.Combat.State.FSMachine Flat { get; private set; }
		public bool IsActive()
		{
			return Flat != null && Flat.Current != null;
		}
		public void TickDomain()
		{
			if (Flat != null)
			{
				Flat.Tick();
			}
		}
		private const float HorizontalDeadzone = 0.01f;
		private const float DefaultLightStartup = 0.05f;
		private const float DefaultLightActive = 0.04f;
		private const float DefaultLightRecovery = 0.12f;
		private const float DefaultHeavyStartup = 0.12f;
		private const float DefaultHeavyActive = 0.05f;
		private const float DefaultHeavyRecovery = 0.22f;

		public class IdleFlat : FightingGame.Combat.State.FSMState
		{
			public IdleFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Idle";
			public override void Tick()
			{
				Actors.FighterCommands pendingCommands = Fighter.PendingCommands; // no abbreviations
				if (Mathf.Abs(pendingCommands.moveX) > HorizontalDeadzone)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new WalkFlat(Fighter));
					return;
				}
				if (pendingCommands.crouch)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new CrouchFlat(Fighter));
					return;
				}
				if (pendingCommands.light)
				{
					Fighter.EnterAttackHFSM("Light");
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.EnterAttackHFSM("Heavy");
					return;
				}
			}
		}

		public class WalkFlat : FightingGame.Combat.State.FSMState
		{
			public WalkFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Walk";
			public override void Tick()
			{
				Actors.FighterCommands pendingCommands = Fighter.PendingCommands;
				if (Mathf.Abs(pendingCommands.moveX) < HorizontalDeadzone)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
					return;
				}
				Fighter.Move(pendingCommands.moveX);
				if (pendingCommands.light)
				{
					Fighter.EnterAttackHFSM("Light");
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.EnterAttackHFSM("Heavy");
					return;
				}
			}
		}

		public class CrouchFlat : FightingGame.Combat.State.FSMState
		{
			public CrouchFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Crouch";
			public override void OnEnter()
			{
				Fighter.IsCrouching = true;
				Fighter.SetAnimatorBool("Crouch", true);
			}
			public override void OnExit()
			{
				Fighter.IsCrouching = false;
				Fighter.SetAnimatorBool("Crouch", false);
			}
			public override void Tick()
			{
				Actors.FighterCommands pendingCommands = Fighter.PendingCommands;
				if (!pendingCommands.crouch)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
					return;
				}
				if (pendingCommands.light)
				{
					Fighter.EnterAttackHFSM("Light");
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.EnterAttackHFSM("Heavy");
					return;
				}
			}
		}
	}

	public class OffenseDomainState : HState
	{
		public AttackState GroundLight { get; private set; }
		public AttackState GroundHeavy { get; private set; }
		public AttackState AirLight { get; private set; }
		public AttackState AirHeavy { get; private set; }
		public ThrowState Throw { get; private set; }
		public FightingGame.Combat.State.FSMachine Flat { get; private set; }
		public bool IsActive()
		{
			return Flat != null && Flat.Current != null;
		}
		public void TickDomain()
		{
			if (Flat != null)
			{
				Flat.Tick();
			}
		}
		private const float DefaultGroundStartup = 0.10f;
		private const float DefaultGroundActive = 0.05f;
		private const float DefaultGroundRecovery = 0.16f;
		private const float DefaultAirStartup = 0.06f;
		private const float DefaultAirActive = 0.05f;
		private const float DefaultAirRecovery = 0.16f;
		private const float DefaultSuperStartup = 0.12f;
		private const float DefaultSuperActive = 0.08f;
		private const float DefaultSuperRecovery = 0.28f;
		private const float DefaultThrowDelay = 0.15f;
		private const float DefaultAirThrowDelay = 0.12f;
		private const float DefaultThrowRange = 1.0f;
		private const float DefaultThrowTechWindow = 0.25f;
		private const float DefaultAirThrowRange = 1.1f;
		private const float DefaultAirThrowTechWindow = 0.20f;

		public class AttackFlat : FightingGame.Combat.State.FSMState
		{
			private readonly string triggerName;
			private float elapsedSeconds;
			private float startupSeconds, activeSeconds, recoverySeconds;
			public AttackFlat(FightingGame.Combat.Actors.FighterActor actor, string triggerName) : base(actor)
			{
				this.triggerName = triggerName;
			}
			public override string Name => "Offense-" + triggerName;
			public override void OnEnter()
			{
				Data.CombatActionDefinition actionData = Fighter.actionSet != null ? Fighter.actionSet.Get(triggerName) : null;
				startupSeconds = actionData != null ? actionData.startup : DefaultGroundStartup;
				activeSeconds = actionData != null ? actionData.active : DefaultGroundActive;
				recoverySeconds = actionData != null ? actionData.recovery : DefaultGroundRecovery;
				elapsedSeconds = 0f;
				Fighter.TriggerAttack(triggerName);
#if UNITY_EDITOR
				Debug.Log("[Offense] Enter Attack " + triggerName);
#endif
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds < startupSeconds) return;
				if (elapsedSeconds < startupSeconds + activeSeconds)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (elapsedSeconds < startupSeconds + activeSeconds + recoverySeconds)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}

		public class SuperFlat : FightingGame.Combat.State.FSMState
		{
			private float elapsedSeconds;
			private float startupSeconds, activeSeconds, recoverySeconds;
			public SuperFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Offense-Super";
			public override void OnEnter()
			{
				Data.CombatActionDefinition actionData = Fighter.actionSet != null ? Fighter.actionSet.Get("Super") : null;
				startupSeconds = actionData != null ? actionData.startup : DefaultSuperStartup;
				activeSeconds = actionData != null ? actionData.active : DefaultSuperActive;
				recoverySeconds = actionData != null ? actionData.recovery : DefaultSuperRecovery;
				elapsedSeconds = 0f;
				Fighter.TriggerAttack("Super");
#if UNITY_EDITOR
				Debug.Log("[Offense] Enter Super");
#endif
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds < startupSeconds) return;
				if (elapsedSeconds < startupSeconds + activeSeconds)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (elapsedSeconds < startupSeconds + activeSeconds + recoverySeconds)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}

		public class AirAttackFlat : FightingGame.Combat.State.FSMState
		{
			private readonly string triggerName;
			private float elapsedSeconds;
			private float startupSeconds, activeSeconds, recoverySeconds;
			public AirAttackFlat(FightingGame.Combat.Actors.FighterActor actor, string triggerName) : base(actor)
			{
				this.triggerName = triggerName;
			}
			public override string Name => "Offense-Air-" + triggerName;
			public override void OnEnter()
			{
				Data.CombatActionDefinition actionData = Fighter.actionSet != null ? Fighter.actionSet.Get(triggerName) : null;
				startupSeconds = actionData != null ? actionData.startup : DefaultAirStartup;
				activeSeconds = actionData != null ? actionData.active : DefaultAirActive;
				recoverySeconds = actionData != null ? actionData.recovery : DefaultAirRecovery;
				elapsedSeconds = 0f;
				Fighter.TriggerAttack(triggerName);
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				Data.CombatActionDefinition currentMoveData = Fighter.CurrentAction;
				bool didRequestCancel = Fighter.TryConsumeComboCancel(out string toTrigger);
				bool hadContact = Fighter.HasRecentHitConfirm();
				bool allowCancel = false;
				if (currentMoveData != null)
				{
					if (!hadContact && currentMoveData.canCancelOnWhiff && elapsedSeconds >= currentMoveData.onWhiffCancelWindow.x && elapsedSeconds <= currentMoveData.onWhiffCancelWindow.y)
					{
						allowCancel = true;
					}
					if (hadContact)
					{
						if (currentMoveData.canCancelOnHit && elapsedSeconds >= currentMoveData.onHitCancelWindow.x && elapsedSeconds <= currentMoveData.onHitCancelWindow.y)
						{
							allowCancel = true;
						}
						if (currentMoveData.canCancelOnBlock && elapsedSeconds >= currentMoveData.onBlockCancelWindow.x && elapsedSeconds <= currentMoveData.onBlockCancelWindow.y)
						{
							allowCancel = true;
						}
					}
					if (allowCancel && currentMoveData.cancelIntoTriggers != null && currentMoveData.cancelIntoTriggers.Length > 0)
					{
						bool listed = false;
						for (int i = 0; i < currentMoveData.cancelIntoTriggers.Length; i++)
						{
							if (currentMoveData.cancelIntoTriggers[i] == toTrigger)
							{
								listed = true;
								break;
							}
						}
						allowCancel = listed;
					}
				}
				if (didRequestCancel && allowCancel && !string.IsNullOrEmpty(toTrigger))
				{
					Fighter.TriggerAttack(toTrigger);
					return;
				}
				if (elapsedSeconds < startupSeconds) return;
				if (elapsedSeconds < startupSeconds + activeSeconds)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (elapsedSeconds < startupSeconds + activeSeconds + recoverySeconds)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}

		public class ThrowFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public ThrowFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Offense-Throw";
			public override void OnEnter()
			{
				countdownSeconds = DefaultThrowDelay;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds > 0f) return;
				Actors.FighterActor opponent = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (opponent != null && Fighter.IsOpponentInThrowRange(DefaultThrowRange))
				{
					opponent.StartThrowTechWindow(DefaultThrowTechWindow);
					if (!opponent.WasTechTriggeredAndClear())
					{
						Fighter.ApplyThrowOn(opponent);
					}
				}
				Fighter.SetAnimatorBool("Throw", false);
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}

		public class AirThrowFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public AirThrowFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Offense-AirThrow";
			public override void OnEnter()
			{
				countdownSeconds = DefaultAirThrowDelay;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds > 0f) return;
				Actors.FighterActor opponent = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (opponent != null && !opponent.IsGrounded() && Vector2.Distance(opponent.transform.position, Fighter.transform.position) < DefaultAirThrowRange)
				{
					opponent.StartThrowTechWindow(DefaultAirThrowTechWindow);
					if (!opponent.WasTechTriggeredAndClear())
					{
						Fighter.ApplyThrowOn(opponent);
					}
				}
				Fighter.SetAnimatorBool("Throw", false);
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}

		public class GuardBreakThrowFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public GuardBreakThrowFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Offense-GuardBreakThrow";
			public override void OnEnter()
			{
				countdownSeconds = DefaultThrowDelay;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds > 0f) return;
				Actors.FighterActor opponent = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				bool opponentBlocking = opponent != null && opponent.PendingCommands.block;
				if (opponent != null && opponentBlocking && Fighter.IsOpponentInThrowRange(DefaultThrowRange))
				{
					opponent.StartThrowTechWindow(DefaultThrowTechWindow);
					if (!opponent.WasTechTriggeredAndClear())
					{
						Fighter.ApplyThrowOn(opponent);
					}
				}
				Fighter.SetAnimatorBool("Throw", false);
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
}