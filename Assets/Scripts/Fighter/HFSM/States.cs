using UnityEngine;

namespace FightingGame.Combat.State.HFSM
{
	/// <summary>
	/// Root of the hierarchical finite state machine (HFSM) for a fighter.
	/// Orchestrates domain arbitration with strict priority: Defense > Offense > Movement.
	/// 角色的分层有限状态机（HFSM）根节点；按优先级仲裁：防御 > 进攻 > 移动。
	/// </summary>
	public class RootState : HState
	{
		/// <summary>
		/// Movement domain state holder. Created in constructor; not null after construction.
		/// 移动域状态容器，构造时创建，构造完成后非空。
		/// </summary>
		public MovementDomainState Movement { get; private set; }

		/// <summary>
		/// Offense domain state holder. Created in constructor; not null after construction.
		/// 进攻域状态容器，构造时创建，构造完成后非空。
		/// </summary>
		public OffenseDomainState Offense { get; private set; }

		/// <summary>
		/// Defense domain state holder. Created in constructor; not null after construction.
		/// 防御域状态容器，构造时创建，构造完成后非空。
		/// </summary>
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

		/// <summary>
		/// Constructs the root HFSM and wires three domain states. Parameter name kept as <paramref name="actor"/> for safety (callers may use named arguments).
		/// 构造根 HFSM 并连接三大域；为避免命名实参潜在破坏，保留参数名 <paramref name="actor"/>。
		/// </summary>
		/// <param name="actor">Fighter actor instance / 角色实例</param>
		public RootState(FightingGame.Combat.Actors.FighterActor actor) : base(actor)
		{
			Movement = new MovementDomainState(actor, this);
			Offense = new OffenseDomainState(actor, this);
			Defense = new DefenseDomainState(actor, this);
		}

		/// <summary>
		/// Enters the initial locomotion state if available.
		/// 若可用则进入初始的移动子状态。
		/// </summary>
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
				Defense.Flat.ChangeState(new DefenseDomainState.BlockFlat(Fighter, pendingCommands.crouch));
				Defense.Flat.Tick();
				return;
			}

			// Dodge if requested and allowed
			// 当请求回避且允许时进入回避
			if (pendingCommands.dodge && Fighter.CanDodge())
			{
				Defense.Flat.ChangeState(new DefenseDomainState.DodgeFlat(Fighter));
				Defense.Flat.Tick();
				return;
			}

			// If Offense is currently active, keep ticking it
			// 若进攻域处于激活态，继续推进
			if (Offense.Flat.Current != null)
			{
				Offense.Flat.Tick();
				return;
			}

			// Air offense trigger: airborne + Light/Heavy
			// 空中攻击触发：在空中且按下轻/重攻击
			if (!Fighter.IsGrounded() && (pendingCommands.light || pendingCommands.heavy))
			{
				Offense.BeginAirAttackFlat(pendingCommands.light ? "Light" : "Heavy");
				Offense.Flat.Tick();
				return;
			}

			// Fallback to Movement domain
			// 回退到移动域
			Movement.Flat.Tick();
		}
	}

	/// <summary>
	/// Movement domain wrapper which hosts the Locomotion HFSM and an embedded flat FSM (movement + simple attacks).
	/// 整合移动域：包含 Locomotion 的 HFSM 与一个嵌入式平面 FSM（用于移动与轻/重攻击）。
	/// </summary>
	public class MovementDomainState : HState
	{
		/// <summary>
		/// Locomotion state instance (forwarded to callers for backward compatibility).
		/// Locomotion 实例（向后兼容，对外转发）。
		/// </summary>
		public LocomotionState Locomotion { get; private set; }

		/// <summary>
		/// Embedded flat finite state machine for movement-related flats (Idle/Walk/Crouch/Attack).
		/// 嵌入式平面有限状态机，承载移动相关子态（静止/行走/下蹲/攻击）。
		/// </summary>
		public FightingGame.Combat.State.FSMachine Flat { get; private set; }

		// Local tuning constants (private and scoped to this class)
		private const float HorizontalDeadzone = 0.01f;
		private const float DefaultLightStartup = 0.05f;
		private const float DefaultLightActive = 0.04f;
		private const float DefaultLightRecovery = 0.12f;
		private const float DefaultHeavyStartup = 0.12f;
		private const float DefaultHeavyActive = 0.05f;
		private const float DefaultHeavyRecovery = 0.22f;

		/// <summary>
		/// Idle flat state: monitors input to transition to Walk/Crouch/Attack flats.
		/// 静止子态：监听输入并在需要时切换到行走/下蹲/攻击子态。
		/// </summary>
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
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
					return;
				}
			}
		}

		/// <summary>
		/// Walk flat state: moves the fighter while walk input persists, otherwise returns to Idle.
		/// 行走子态：当行走输入持续时移动角色，否则回到静止态。
		/// </summary>
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
				// perform movement if still holding input
				Fighter.Move(pendingCommands.moveX);
				// check attack transitions after movement handling
				if (pendingCommands.light)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
					return;
				}
			}
		}

		/// <summary>
		/// Crouch flat: sets animator and crouch flag on enter/exit; while crouching can perform attacks.
		/// 下蹲子态：进入/离开时设置动画与下蹲标志；下蹲期间可发起攻击。
		/// </summary>
		public class CrouchFlat : FightingGame.Combat.State.FSMState
		{
			public CrouchFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Crouch";
			public override void OnEnter()
			{
				Fighter.IsCrouching = true;
				Fighter.SetAnimatorBool("Crouch", true); // keep animator string unchanged
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
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
					return;
				}
				if (pendingCommands.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
					return;
				}
			}
		}

		/// <summary>
		/// Light attack flat: handles startup/active/recovery using move data when available.
		/// 轻攻击子态：根据 action data（若存在）处理起始/有效/恢复阶段。
		/// </summary>
		public class AttackLightFlat : FightingGame.Combat.State.FSMState
		{
			private float elapsedSeconds;
			private float startupSeconds, activeSeconds, recoverySeconds;

			public AttackLightFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Attack-Light";
			public override void OnEnter()
			{
				Data.CombatActionDefinition actionData = Fighter.actionSet != null ? Fighter.actionSet.Get("Light") : null;
				startupSeconds = actionData != null ? actionData.startup : DefaultLightStartup;
				activeSeconds = actionData != null ? actionData.active : DefaultLightActive;
				recoverySeconds = actionData != null ? actionData.recovery : DefaultLightRecovery;
				elapsedSeconds = 0f;
				Fighter.TriggerAttack("Light"); // keep animator/trigger string unchanged
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds < startupSeconds)
				{
					return;
				}
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
				Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Heavy attack flat: similar to light but with different default timings.
		/// 重攻击子态：与轻攻击类似，但默认时间参数不同。
		/// </summary>
		public class AttackHeavyFlat : FightingGame.Combat.State.FSMState
		{
			private float elapsedSeconds;
			private float startupSeconds, activeSeconds, recoverySeconds;

			public AttackHeavyFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Attack-Heavy";
			public override void OnEnter()
			{
				Data.CombatActionDefinition actionData = Fighter.actionSet != null ? Fighter.actionSet.Get("Heavy") : null;
				startupSeconds = actionData != null ? actionData.startup : DefaultHeavyStartup;
				activeSeconds = actionData != null ? actionData.active : DefaultHeavyActive;
				recoverySeconds = actionData != null ? actionData.recovery : DefaultHeavyRecovery;
				elapsedSeconds = 0f;
				Fighter.TriggerAttack("Heavy");
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds < startupSeconds)
				{
					return;
				}
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
				Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Constructs the MovementDomainState with a Locomotion substate and initializes the embedded flat FSM.
		/// 构造 MovementDomainState，创建 Locomotion 子态并初始化嵌入式平面 FSM。
		/// </summary>
		public MovementDomainState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			Locomotion = new LocomotionState(actor, this);
			Flat = new FightingGame.Combat.State.FSMachine();
			Flat.SetInitial(new IdleFlat(actor));
		}

		public override string Name => "Movement";

		public override void OnEnter()
		{
			if (Locomotion != null)
			{
				Locomotion.OnEnter();
			}
		}

		public override void OnTick()
		{
			Flat.Tick();
		}
	}

	/// <summary>
	/// Offense domain wrapper: holds concrete Attack/Throw states and an embedded flat FSM for offense sequencing.
	/// 进攻域封装：包含具体的攻击/投掷状态以及用于进攻序列的嵌入式平面 FSM。
	/// </summary>
	public class OffenseDomainState : HState
	{
		/// <summary>
		/// Ground light attack state instance (forwarded for potential HFSM migration).
		/// 地面轻攻击状态实例（为后续 HFSM 迁移保留）。
		/// </summary>
		public AttackState GroundLight { get; private set; }
		public AttackState GroundHeavy { get; private set; }
		public AttackState AirLight { get; private set; }
		public AttackState AirHeavy { get; private set; }
		public ThrowState Throw { get; private set; }

		/// <summary>
		/// Embedded flat FSM for offense-related flats (attacks, throws, super, heal).
		/// 嵌入式平面 FSM，承载进攻相关的子态（攻击、投掷、大招、回复）。
		/// </summary>
		public FightingGame.Combat.State.FSMachine Flat { get; private set; }

		// Default timing constants (scoped to this class)
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

		/// <summary>
		/// General attack flat used for ground/air attacks. Uses triggerName to lookup move data.
		/// 通用攻击子态：基于 triggerName 来读取 move data（可用于地面与空中攻击）。
		/// </summary>
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
				Debug.Log($"[Offense] Enter Attack {triggerName}");
#endif
			}

			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds < startupSeconds)
					return;
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
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Super attack flat (uses different default timings).
		/// 大招子态（使用不同的默认时间参数）。
		/// </summary>
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
				if (elapsedSeconds < startupSeconds)
					return;
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
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Air attack flat with cancel-window and cancel-list checks.
		/// 空中攻击子态，包含取消窗口与取消目标列表检查。
		/// </summary>
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
					// check whiff cancel window
					if (!hadContact && currentMoveData.canCancelOnWhiff && elapsedSeconds >= currentMoveData.onWhiffCancelWindow.x && elapsedSeconds <= currentMoveData.onWhiffCancelWindow.y)
					{
						allowCancel = true;
					}
					// check hit/block cancel windows
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
					// if allowCancel, ensure the requested trigger is in the cancel list (if list exists)
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
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Throw flat: timed throw attempt with tech window for opponent.
		/// 投掷子态：定时尝试投掷，对手拥有投掷化解时间窗口。
		/// </summary>
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
				if (countdownSeconds > 0f)
					return;
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
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Air throw flat: similar to ThrowFlat but checks airborne condition and uses separate timing/range.
		/// 空中投掷子态：与地面投掷类似，但需检查目标是否在空中并使用独立的时间/范围参数。
		/// </summary>
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
				if (countdownSeconds > 0f)
					return;
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
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Guard break throw: only executes if opponent is blocking and in range.
		/// 破防投掷：仅在对手处于格挡并在范围内时生效。
		/// </summary>
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
				if (countdownSeconds > 0f)
					return;
				Actors.FighterActor opponent = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				bool opponentBlocking = opponent != null && opponent.PendingCommands.block;
				if (opponent != null && opponentBlocking && Fighter.IsOpponentInThrowRange(DefaultThrowRange))
				{
					opponent.StartThrowTechWindow(DefaultThrowTechWindow);
					if (!opponent.WasTechTriggeredAndClear())
						Fighter.ApplyThrowOn(opponent);
				}
				Fighter.SetAnimatorBool("Throw", false);
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}

		/// <summary>
		/// Heal flat: execute heal then wait for recovery time before returning to movement.
		/// 回复子态：执行回复并等待恢复时间然后回到移动态。
		/// </summary>
		public class HealFlat : FightingGame.Combat.State.FSMState
		{
			private readonly string triggerName;
			private float elapsedSeconds;
			private float recoverySeconds;
			public HealFlat(FightingGame.Combat.Actors.FighterActor actor, string triggerName) : base(actor) { this.triggerName = triggerName; }
			public override string Name => "Heal";
			public override void OnEnter()
			{
				Fighter.ExecuteHeal(triggerName);
				recoverySeconds = 0.22f; elapsedSeconds = 0f;
#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Heal");
#endif
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds >= recoverySeconds)
				{
					Fighter.ClearCurrentMove();
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		public OffenseDomainState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			RootState root = parent as RootState;
			LocomotionState locomotion = root != null ? root.Locomotion : null;
			GroundLight = new AttackState(actor, this, "Light", locomotion);
			GroundHeavy = new AttackState(actor, this, "Heavy", locomotion);
			AirLight = new AttackState(actor, this, "Light", locomotion);
			AirHeavy = new AttackState(actor, this, "Heavy", locomotion);
			Throw = new ThrowState(actor, this, locomotion);
			Flat = new FightingGame.Combat.State.FSMachine();
		}

		public override string Name => "Offense";
		public override void OnTick()
		{
			Flat.Tick();
		}

		// Begin helpers that create flats while preserving public API
		public void BeginAttackFlat(string trigger)
		{
			Flat.ChangeState(new AttackFlat(Fighter, trigger));
		}
		public void BeginAirAttackFlat(string trigger)
		{
			Flat.ChangeState(new AirAttackFlat(Fighter, trigger));
		}
		public void BeginThrowFlat()
		{
			Flat.ChangeState(new ThrowFlat(Fighter));
		}
		public void BeginHealFlat(string trigger)
		{
			Flat.ChangeState(new HealFlat(Fighter, trigger));
		}
		public void BeginSuperFlat()
		{
			Flat.ChangeState(new SuperFlat(Fighter));
		}
		public void BeginAirThrowFlat()
		{
			Flat.ChangeState(new AirThrowFlat(Fighter));
		}
		public void BeginGuardBreakThrowFlat()
		{
			Flat.ChangeState(new GuardBreakThrowFlat(Fighter));
		}
	}

	/// <summary>
	/// Defense domain wrapper: contains block/dodge/hitstun/downed/wakeup behaviors and an embedded flat FSM for defensive sequences.
	/// 防御域封装：包含格挡/回避/受击硬直/倒地/起身行为空间，并提供一个嵌入式平面 FSM 用于防御序列。
	/// </summary>
	public class DefenseDomainState : HState
	{
		public BlockStandState BlockStand { get; private set; }
		public BlockCrouchState BlockCrouch { get; private set; }
		public DodgeState Dodge { get; private set; }
		public HitstunState Hitstun { get; private set; }
		public DownedState Downed { get; private set; }
		public WakeupState Wakeup { get; private set; }

		/// <summary>
		/// Embedded flat FSM for defense-related flats.
		/// 防御相关的嵌入式平面 FSM。
		/// </summary>
		public FightingGame.Combat.State.FSMachine Flat { get; private set; }

		// Class-scoped tuning constants
		private const float DefaultWakeupInvulnerability = 0.25f;
		private const float WakeupRollImpulse = 0.12f;

		/// <summary>
		/// Blocking flat: supports standing/crouch variants and enforces hold limits using runtime configuration.
		/// 格挡子态：支持站立/下蹲变体，并使用运行时配置限制格挡持续时间。
		/// </summary>
		public class BlockFlat : FightingGame.Combat.State.FSMState
		{
			private readonly bool isCrouch;
			public BlockFlat(FightingGame.Combat.Actors.FighterActor actor, bool crouch) : base(actor)
			{
				isCrouch = crouch;
			}

			public override string Name => isCrouch ? "Block(Crouch)" : "Block";

			public override void OnEnter()
			{
				Fighter.SetAnimatorBool("Block", true);
				if (isCrouch)
				{
					Fighter.IsCrouching = true;
					Fighter.SetAnimatorBool("Crouch", true);
				}
			}

			public override void OnExit()
			{
				Fighter.SetAnimatorBool("Block", false);
			}

			public override void Tick()
			{
				var pendingCommands = Fighter.PendingCommands;
				var runtimeConfig = Systems.RuntimeConfig.Instance;
				// If block held beyond configured maximum, trigger cooldown and return to movement
				if (runtimeConfig != null && pendingCommands.block && Fighter.GetBlockHeldSeconds() > runtimeConfig.blockMaxHoldSeconds)
				{
					Fighter.gameObject.SendMessage("__LockBlockForSeconds", runtimeConfig.blockCooldownSeconds, SendMessageOptions.DontRequireReceiver);
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
					return;
				}

				if (!pendingCommands.block)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		/// <summary>
		/// Dodge flat: performs dodge teleport and grants short invulnerability.
		/// 回避子态：执行回避传送并赋予短暂无敌。
		/// </summary>
		public class DodgeFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public DodgeFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Dodge";
			public override void OnEnter()
			{
				Fighter.TryPerformDodgeTeleport(Fighter.PendingCommands.moveX);
				Fighter.StartDodge();
				countdownSeconds = Fighter.Stats.dodgeInvulnerable;
#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Dodge (invulnerability frames active)");
#endif
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds <= 0f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		/// <summary>
		/// Hitstun flat: simple countdown before returning to movement.
		/// 受击硬直子态：倒计时后回到移动态。
		/// </summary>
		public class HitstunFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public HitstunFlat(FightingGame.Combat.Actors.FighterActor actor, float durationSeconds) : base(actor)
			{
				countdownSeconds = durationSeconds;
			}
			public override string Name => "Hitstun";
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds <= 0f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		/// <summary>
		/// Heal flat: execute heal then wait for recovery before returning.
		/// 回复子态：执行回复并等待恢复时间然后返回。
		/// </summary>
		public class HealFlat : FightingGame.Combat.State.FSMState
		{
			private readonly string triggerName;
			private float elapsedSeconds;
			private float recoverySeconds;
			public HealFlat(FightingGame.Combat.Actors.FighterActor actor, string triggerName) : base(actor) { this.triggerName = triggerName; }
			public override string Name => "Heal";
			public override void OnEnter()
			{
				Fighter.ExecuteHeal(triggerName);
				recoverySeconds = 0.22f;
				elapsedSeconds = 0f;
#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Heal");
#endif
			}
			public override void Tick()
			{
				elapsedSeconds += Time.deltaTime;
				if (elapsedSeconds >= recoverySeconds)
				{
					Fighter.ClearCurrentMove();
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		/// <summary>
		/// Downed flat: plays downed animation and transitions to wakeup when timer expires.
		/// 倒地子态：播放倒地动画，计时结束后进入起身态。
		/// </summary>
		public class DownedFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			private readonly bool isHard;
			public DownedFlat(FightingGame.Combat.Actors.FighterActor actor, bool hard, float durationSeconds) : base(actor)
			{
				isHard = hard;
				countdownSeconds = durationSeconds;
			}
			public override string Name => isHard ? "Downed(Hard)" : "Downed(Soft)";
			public override void OnEnter()
			{
				Fighter.SetAnimatorBool("Downed", true);
			}
			public override void OnExit()
			{
				Fighter.SetAnimatorBool("Downed", false);
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				if (countdownSeconds <= 0f)
				{
					Fighter.HRoot.Defense.Flat.ChangeState(new WakeupFlat(Fighter));
				}
			}
		}

		/// <summary>
		/// Wakeup flat: grants temporary invulnerability, allows directional roll/backrise input early in the window.
		/// 起身子态：提供短暂无敌，并允许在早期窗口内通过方向输入进行前滚/后起。
		/// </summary>
		public class WakeupFlat : FightingGame.Combat.State.FSMState
		{
			private float countdownSeconds;
			public WakeupFlat(FightingGame.Combat.Actors.FighterActor actor) : base(actor) { }
			public override string Name => "Wakeup";
			public override void OnEnter()
			{
				countdownSeconds = Fighter.stats != null ? Fighter.stats.wakeupInvuln : DefaultWakeupInvulnerability;
				Fighter.SetUpperLowerInvulnerable(true, true);
				if (Fighter.animator != null && Fighter.animator.runtimeAnimatorController != null)
				{
					Fighter.animator.SetTrigger("Wakeup");
				}
			}
			public override void OnExit()
			{
				Fighter.SetUpperLowerInvulnerable(false, false);
			}
			public override void Tick()
			{
				countdownSeconds -= Time.deltaTime;
				var pendingCommands = Fighter.PendingCommands;
				float totalInvuln = Fighter.stats != null ? Fighter.stats.wakeupInvuln : DefaultWakeupInvulnerability;
				float halfWindow = totalInvuln * 0.5f;

				// Early window: allow directional roll/backrise impulse
				if (countdownSeconds > 0f && countdownSeconds > totalInvuln - halfWindow)
				{
					float direction = 0f;
					if (pendingCommands.moveX > 0.4f)
					{
						direction = Fighter.facingRight ? 1f : -1f; // forward roll
					}
					else if (pendingCommands.moveX < -0.4f)
					{
						direction = Fighter.facingRight ? -1f : 1f; // backrise
					}
					if (Mathf.Abs(direction) > 0.1f)
					{
						Fighter.AddExternalImpulse(direction * WakeupRollImpulse);
					}
				}

				if (countdownSeconds <= 0f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}

		public DefenseDomainState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			BlockStand = new BlockStandState(actor, this);
			BlockCrouch = new BlockCrouchState(actor, this);
			Dodge = new DodgeState(actor, this);
			RootState root = parent as RootState;
			LocomotionState locomotion = root != null ? root.Locomotion : null;
			Hitstun = new HitstunState(actor, this, locomotion);
			Downed = new DownedState(actor, this, locomotion);
			Wakeup = new WakeupState(actor, this, locomotion);
			Flat = new FightingGame.Combat.State.FSMachine();
		}

		public override string Name => "Defense";
		public override void OnTick()
		{
			Flat.Tick();
		}

		// Public helpers to trigger flats
		public void BeginHitstunFlat(float seconds)
		{
			Flat.ChangeState(new HitstunFlat(Fighter, seconds));
		}
		public void BeginDownedFlat(bool hard, float duration)
		{
			Flat.ChangeState(new DownedFlat(Fighter, hard, duration));
		}
		public void BeginWakeupFlat()
		{
			Flat.ChangeState(new WakeupFlat(Fighter));
		}
		public void BeginHealFlatDefense(string trigger)
		{
			Flat.ChangeState(new HealFlat(Fighter, trigger));
		}
	}

	/// <summary>
	/// Locomotion-wide constants for thresholds and default timings.
	/// 运动相关的全局常量（阈值与默认时序）
	/// </summary>
	internal static class LocomotionTuning
	{
		public const float MovementDeadZone = 0.01f;                 // Small stick dead-zone / 移动死区
		public const float DefaultAttackStartupSeconds = 0.08f;      // 默认起手帧时长
		public const float DefaultAttackActiveSeconds = 0.06f;       // 默认生效帧时长
		public const float DefaultAttackRecoverySeconds = 0.18f;     // 默认硬直帧时长

		public const float ThrowResolveDelaySeconds = 0.15f;         // 抓取结算延迟
		public const float ThrowTechWindowSeconds = 0.25f;           // 抓取拆招判定窗口
		public const float DefaultThrowRange = 1.0f;                 // 默认抓取距离

		public const float WakeupDirectionThreshold = 0.4f;          // 起身阶段方向判断阈值
		public const float WakeupImpulseMagnitude = 0.12f;           // 起身轻微位移脉冲
		public const float DefaultWakeupInvulnerability = 0.25f;     // 默认起身无敌时长
	}

	/// <summary>
	/// Character locomotion super-state that routes between grounded and aerial substates.
	/// 角色移动总状态：在地面与空中子状态之间路由
	/// </summary>
	public class LocomotionState : HState
	{
		public readonly HStateMachine Machine = new HStateMachine();

		public GroundedState Grounded { get; private set; }

		public AirState Air { get; private set; }

		public LocomotionState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			Grounded = new GroundedState(actor, this);
			Air = new AirState(actor, this);
		}

		public override void OnEnter()
		{
			// Expanded control flow (no ternary) / 展开控制流（避免三元）
			if (Fighter.IsGrounded())
			{
				Machine.SetInitial(this, Grounded.Idle);
				return;
			}
			Machine.SetInitial(this, Air.Jump);
		}

		public override void OnTick()
		{
			if (Fighter.IsGrounded())
			{
				if (Machine.Current == null || Machine.Current.Parent != Grounded)
				{
					Machine.ChangeState(Grounded.Idle);
				}
			}
			else
			{
				if (Machine.Current == null || Machine.Current.Parent != Air)
				{
					Machine.ChangeState(Air.Jump);
				}
			}

			Machine.Tick();
		}
	}

	/// <summary>
	/// Grounded locomotion domain (idle/walk/crouch/defense/offense/etc.).
	/// 地面移动域（站立/行走/蹲/防御/进攻 等）
	/// </summary>
	public class GroundedState : HState
	{
		public HStateMachine Machine
		{
			get
			{
				LocomotionState locomotionState = Parent as LocomotionState;
				return locomotionState != null ? locomotionState.Machine : null;
			}
		}

		public IdleState Idle { get; private set; }
		public WalkState Walk { get; private set; }
		public CrouchState Crouch { get; private set; }
		public BlockStandState BlockStand { get; private set; }
		public BlockCrouchState BlockCrouch { get; private set; }
		public AttackState AttackLight { get; private set; }
		public AttackState AttackHeavy { get; private set; }
		public HitstunState Hitstun { get; private set; }
		public DownedState Downed { get; private set; }
		public WakeupState Wakeup { get; private set; }
		public DodgeState Dodge { get; private set; }
		public ThrowState Throw { get; private set; }

		public GroundedState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			Idle = new IdleState(actor, this);
			Walk = new WalkState(actor, this);
			Crouch = new CrouchState(actor, this);
			BlockStand = new BlockStandState(actor, this);
			BlockCrouch = new BlockCrouchState(actor, this);

			LocomotionState locomotionState = Parent as LocomotionState;
			AttackLight = new AttackState(actor, this, "Light", locomotionState);
			AttackHeavy = new AttackState(actor, this, "Heavy", locomotionState);
			Hitstun = new HitstunState(actor, this, locomotionState);
			Downed = new DownedState(actor, this, locomotionState);
			Wakeup = new WakeupState(actor, this, locomotionState);
			Dodge = new DodgeState(actor, this);
			Throw = new ThrowState(actor, this, locomotionState);
		}
	}

	/// <summary>
	/// Aerial locomotion domain (jumping, aerial attacks).
	/// 空中移动域（跳跃、空中攻击）
	/// </summary>
	public class AirState : HState
	{
		public HStateMachine Machine
		{
			get
			{
				LocomotionState locomotionState = Parent as LocomotionState;
				return locomotionState != null ? locomotionState.Machine : null;
			}
		}

		public JumpAirState Jump { get; private set; }
		public AttackState AirLight { get; private set; }
		public AttackState AirHeavy { get; private set; }
		public HitstunState Hitstun { get; private set; }

		public AirState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent)
		{
			Jump = new JumpAirState(actor, this);
			LocomotionState locomotionState = Parent as LocomotionState;
			AirLight = new AttackState(actor, this, "Light", locomotionState);
			AirHeavy = new AttackState(actor, this, "Heavy", locomotionState);
			Hitstun = new HitstunState(actor, this, locomotionState);
		}
	}

	/// <summary>
	/// Standing neutral. Routes to defense, movement, jump, or offense.
	/// 站立中立：路由至防御、移动、起跳或进攻
	/// </summary>
	public class IdleState : HState
	{
		public IdleState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Idle"; }
		}

		public override void OnTick()
		{
			GroundedState groundedState = Parent as GroundedState;
			LocomotionState locomotionState = Parent.Parent as LocomotionState;
			var pendingCommands = Fighter.PendingCommands;

			if (pendingCommands.block)
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					if (pendingCommands.crouch)
					{
						Fighter.HMachine.ChangeState(defense.BlockCrouch);
					}
					else
					{
						Fighter.HMachine.ChangeState(defense.BlockStand);
					}
					return;
				}

				if (pendingCommands.crouch)
				{
					groundedState.Machine.ChangeState(groundedState.BlockCrouch);
				}
				else
				{
					groundedState.Machine.ChangeState(groundedState.BlockStand);
				}
				return;
			}

			if (pendingCommands.dodge && Fighter.CanDodge())
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					Fighter.HMachine.ChangeState(defense.Dodge);
					return;
				}
				groundedState.Machine.ChangeState(groundedState.Dodge);
				return;
			}

			if (pendingCommands.crouch)
			{
				groundedState.Machine.ChangeState(groundedState.Crouch);
				return;
			}

			if (pendingCommands.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				locomotionState.Machine.ChangeState(locomotionState.Air.Jump);
				return;
			}

			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (pendingCommands.light)
			{
				groundedState.Machine.ChangeState(groundedState.AttackLight);
				return;
			}
			if (pendingCommands.heavy)
			{
				groundedState.Machine.ChangeState(groundedState.AttackHeavy);
				return;
			}

			if (Mathf.Abs(pendingCommands.moveX) > LocomotionTuning.MovementDeadZone)
			{
				groundedState.Machine.ChangeState(groundedState.Walk);
				return;
			}

			Fighter.HaltHorizontal();
		}
	}

	/// <summary>
	/// Grounded walking with lateral input.
	/// 地面行走（左右方向输入）
	/// </summary>
	public class WalkState : HState
	{
		public WalkState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Walk"; }
		}

		public override void OnTick()
		{
			GroundedState groundedState = Parent as GroundedState;
			LocomotionState locomotionState = Parent.Parent as LocomotionState;
			var pendingCommands = Fighter.PendingCommands;

			if (Mathf.Abs(pendingCommands.moveX) < LocomotionTuning.MovementDeadZone)
			{
				groundedState.Machine.ChangeState(groundedState.Idle);
				return;
			}

			if (pendingCommands.block)
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					if (pendingCommands.crouch)
					{
						Fighter.HMachine.ChangeState(defense.BlockCrouch);
					}
					else
					{
						Fighter.HMachine.ChangeState(defense.BlockStand);
					}
					return;
				}

				if (pendingCommands.crouch)
				{
					groundedState.Machine.ChangeState(groundedState.BlockCrouch);
				}
				else
				{
					groundedState.Machine.ChangeState(groundedState.BlockStand);
				}
				return;
			}

			if (pendingCommands.crouch)
			{
				groundedState.Machine.ChangeState(groundedState.Crouch);
				return;
			}

			if (pendingCommands.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				locomotionState.Machine.ChangeState(locomotionState.Air.Jump);
				return;
			}

			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (pendingCommands.light)
			{
				groundedState.Machine.ChangeState(groundedState.AttackLight);
				return;
			}
			if (pendingCommands.heavy)
			{
				groundedState.Machine.ChangeState(groundedState.AttackHeavy);
				return;
			}

			Fighter.Move(pendingCommands.moveX);
		}
	}

	/// <summary>
	/// Crouching neutral with defense/attack transitions.
	/// 蹲姿中立，可转向防御或攻击
	/// </summary>
	public class CrouchState : HState
	{
		public CrouchState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Crouch"; }
		}

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

		public override void OnTick()
		{
			GroundedState groundedState = Parent as GroundedState;
			var pendingCommands = Fighter.PendingCommands;

			if (!pendingCommands.crouch)
			{
				groundedState.Machine.ChangeState(groundedState.Idle);
				return;
			}

			if (pendingCommands.block)
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					Fighter.HMachine.ChangeState(defense.BlockCrouch);
					return;
				}
				groundedState.Machine.ChangeState(groundedState.BlockCrouch);
				return;
			}

			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (pendingCommands.light)
			{
				groundedState.Machine.ChangeState(groundedState.AttackLight);
				return;
			}
			if (pendingCommands.heavy)
			{
				groundedState.Machine.ChangeState(groundedState.AttackHeavy);
				return;
			}
		}
	}

	/// <summary>
	/// Standing guard state with hold limit and stance transitions.
	/// 站立防御：包含长按限制与姿态切换
	/// </summary>
	public class BlockStandState : HState
	{
		public BlockStandState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Block"; }
		}

		public override void OnEnter()
		{
			Fighter.SetAnimatorBool("Block", true);
			Fighter.IsCrouching = false;
			Fighter.SetAnimatorBool("Crouch", false);
		}

		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Block", false);
		}

		public override void OnTick()
		{
			var pendingCommands = Fighter.PendingCommands;
			var runtimeConfiguration = Systems.RuntimeConfig.Instance;

			if (runtimeConfiguration != null &&
				pendingCommands.block &&
				Fighter.GetBlockHeldSeconds() > runtimeConfiguration.blockMaxHoldSeconds)
			{
				Fighter.gameObject.SendMessage("__LockBlockForSeconds", runtimeConfiguration.blockCooldownSeconds, SendMessageOptions.DontRequireReceiver);

				var movementLocomotion = Fighter.HRoot?.Movement?.Locomotion;
				if (movementLocomotion != null)
				{
					Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Idle);
					return;
				}
			}

			if (!pendingCommands.block)
			{
				var movementLocomotion = Fighter.HRoot?.Movement?.Locomotion;
				if (movementLocomotion != null)
				{
					Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Idle);
					return;
				}
			}

			if (pendingCommands.crouch)
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					Fighter.HMachine.ChangeState(defense.BlockCrouch);
					return;
				}
			}
		}
	}

	/// <summary>
	/// Crouching guard state with hold limit and stance transitions.
	/// 蹲姿防御：包含长按限制与姿态切换
	/// </summary>
	public class BlockCrouchState : HState
	{
		public BlockCrouchState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Block(Crouch)"; }
		}

		public override void OnEnter()
		{
			Fighter.IsCrouching = true;
			Fighter.SetAnimatorBool("Crouch", true);
			Fighter.SetAnimatorBool("Block", true);
		}

		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Block", false);
		}

		public override void OnTick()
		{
			var pendingCommands = Fighter.PendingCommands;
			var runtimeConfiguration = Systems.RuntimeConfig.Instance;

			if (runtimeConfiguration != null &&
				pendingCommands.block &&
				Fighter.GetBlockHeldSeconds() > runtimeConfiguration.blockMaxHoldSeconds)
			{
				Fighter.gameObject.SendMessage("__LockBlockForSeconds", runtimeConfiguration.blockCooldownSeconds, SendMessageOptions.DontRequireReceiver);

				var movementLocomotionX = Fighter.HRoot?.Movement?.Locomotion;
				if (movementLocomotionX != null)
				{
					if (pendingCommands.crouch)
					{
						Fighter.HMachine.ChangeState(movementLocomotionX.Grounded.Crouch);
					}
					else
					{
						Fighter.HMachine.ChangeState(movementLocomotionX.Grounded.Idle);
					}
					return;
				}
			}

			if (!pendingCommands.block)
			{
				var movementLocomotion = Fighter.HRoot?.Movement?.Locomotion;
				if (movementLocomotion != null)
				{
					if (pendingCommands.crouch)
					{
						Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Crouch);
					}
					else
					{
						Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Idle);
					}
					return;
				}
			}

			if (!pendingCommands.crouch)
			{
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					Fighter.HMachine.ChangeState(defense.BlockStand);
					return;
				}
			}
		}
	}

	/// <summary>
	/// Generic attack state with startup/active/recovery phases and cancel windows.
	/// 通用攻击状态：包含 起手/生效/硬直 三阶段与可取消窗口
	/// </summary>
	public class AttackState : HState
	{
		private readonly string attackTrigger;
		private readonly LocomotionState locomotionState;

		private float startupSeconds;
		private float activeSeconds;
		private float recoverySeconds;
		private float elapsedSeconds;

		private enum Phase
		{
			Startup,
			Active,
			Recovery
		}

		private Phase currentPhase;

		public AttackState(FightingGame.Combat.Actors.FighterActor actor, HState parent, string attackTriggerName, LocomotionState locomotionReference) : base(actor, parent)
		{
			attackTrigger = attackTriggerName;
			locomotionState = locomotionReference;
		}

		public override string Name
		{
			get { return "Attack-" + attackTrigger; }
		}

		public override void OnEnter()
		{
			Data.CombatActionDefinition actionData = (Fighter.actionSet != null) ? Fighter.actionSet.Get(attackTrigger) : null;

			if (actionData != null)
			{
				startupSeconds = actionData.startup;
				activeSeconds = actionData.active;
				recoverySeconds = actionData.recovery;
			}
			else
			{
				startupSeconds = LocomotionTuning.DefaultAttackStartupSeconds;
				activeSeconds = LocomotionTuning.DefaultAttackActiveSeconds;
				recoverySeconds = LocomotionTuning.DefaultAttackRecoverySeconds;
			}

			elapsedSeconds = 0f;
			currentPhase = Phase.Startup;
			Fighter.TriggerAttack(attackTrigger);
		}

		public override void OnTick()
		{
			elapsedSeconds += Time.deltaTime;

			Data.CombatActionDefinition actionData = Fighter.CurrentAction;
			bool isCancelRequested = Fighter.TryConsumeComboCancel(out string targetAttackTrigger);
			bool hasHitConfirm = Fighter.HasRecentHitConfirm();
			bool isCancelAllowed = false;

			if (actionData != null)
			{
				if (!hasHitConfirm && actionData.canCancelOnWhiff &&
					elapsedSeconds >= actionData.onWhiffCancelWindow.x &&
					elapsedSeconds <= actionData.onWhiffCancelWindow.y)
				{
					isCancelAllowed = true;
				}

				if (hasHitConfirm)
				{
					if (actionData.canCancelOnHit &&
						elapsedSeconds >= actionData.onHitCancelWindow.x &&
						elapsedSeconds <= actionData.onHitCancelWindow.y)
					{
						isCancelAllowed = true;
					}
					if (actionData.canCancelOnBlock &&
						elapsedSeconds >= actionData.onBlockCancelWindow.x &&
						elapsedSeconds <= actionData.onBlockCancelWindow.y)
					{
						isCancelAllowed = true;
					}
				}
			}

			switch (currentPhase)
			{
				case Phase.Startup:
					{
						if (elapsedSeconds >= startupSeconds)
						{
							currentPhase = Phase.Active;
							elapsedSeconds = 0f;
							Fighter.SetAttackActive(true);
						}
						break;
					}
				case Phase.Active:
					{
						if (isCancelRequested && isCancelAllowed && !string.IsNullOrEmpty(targetAttackTrigger))
						{
							Fighter.TriggerAttack(targetAttackTrigger);
							currentPhase = Phase.Startup;
							elapsedSeconds = 0f;
							break;
						}

						if (elapsedSeconds >= activeSeconds)
						{
							currentPhase = Phase.Recovery;
							elapsedSeconds = 0f;
							Fighter.SetAttackActive(false);
						}
						break;
					}
				case Phase.Recovery:
					{
						if (isCancelRequested && isCancelAllowed && !string.IsNullOrEmpty(targetAttackTrigger))
						{
							Fighter.TriggerAttack(targetAttackTrigger);
							currentPhase = Phase.Startup;
							elapsedSeconds = 0f;
							break;
						}

						if (elapsedSeconds >= recoverySeconds)
						{
							if (locomotionState != null)
							{
								// Return to locomotion neutral via root machine.
								// 经由根状态机返回移动中立
								Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
							}
							Fighter.ClearCurrentMove();
						}
						break;
					}
			}
		}

		public override void OnExit()
		{
			Fighter.SetAttackActive(false);
		}
	}

	/// <summary>
	/// Hitstun (cannot act). Returns to locomotion after timer.
	/// 受击硬直：计时结束后回到移动域
	/// </summary>
	public class HitstunState : HState
	{
		private readonly LocomotionState locomotionState;
		private float remainingSeconds;

		public HitstunState(FightingGame.Combat.Actors.FighterActor actor, HState parent, LocomotionState locomotionReference = null) : base(actor, parent)
		{
			locomotionState = locomotionReference;
		}

		public override string Name
		{
			get { return "Hitstun"; }
		}

		public void SetTime(float durationSeconds)
		{
			remainingSeconds = durationSeconds;
		}

		public override void OnTick()
		{
			remainingSeconds -= Time.deltaTime;
			if (remainingSeconds <= 0f)
			{
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	/// <summary>
	/// Downed (soft/hard knockdown). May route to wakeup.
	/// 倒地（软/重击倒），可路由到起身
	/// </summary>
	public class DownedState : HState
	{
		private readonly LocomotionState locomotionState;
		private float remainingSeconds;
		private bool isHardKnockdown;

		public DownedState(FightingGame.Combat.Actors.FighterActor actor, HState parent, LocomotionState locomotionReference = null) : base(actor, parent)
		{
			locomotionState = locomotionReference;
		}

		public override string Name
		{
			get { return isHardKnockdown ? "Downed(Hard)" : "Downed(Soft)"; }
		}

		public void Begin(bool hardKnockdown, float durationSeconds)
		{
			isHardKnockdown = hardKnockdown;
			remainingSeconds = durationSeconds;
		}

		public override void OnEnter()
		{
			Fighter.SetAnimatorBool("Downed", true);
		}

		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Downed", false);
		}

		public override void OnTick()
		{
			remainingSeconds -= Time.deltaTime;

			if (remainingSeconds <= 0f)
			{
				// Prefer Defense domain Wakeup if available / 优先使用防御域的起身
				var defense = Fighter.HRoot?.Defense;
				if (defense != null)
				{
					Fighter.HMachine.ChangeState(defense.Wakeup);
					return;
				}

				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	/// <summary>
	/// Wakeup with brief invulnerability and directional micro-adjust.
	/// 起身：短暂无敌并允许微小方向位移
	/// </summary>
	public class WakeupState : HState
	{
		private readonly LocomotionState locomotionState;
		private float remainingSeconds;

		public WakeupState(FightingGame.Combat.Actors.FighterActor actor, HState parent, LocomotionState locomotionReference = null) : base(actor, parent)
		{
			locomotionState = locomotionReference;
		}

		public override string Name
		{
			get { return "Wakeup"; }
		}

		public override void OnEnter()
		{
			float configured = (Fighter.stats != null) ? Fighter.stats.wakeupInvuln : LocomotionTuning.DefaultWakeupInvulnerability;
			remainingSeconds = configured;

			Fighter.SetUpperLowerInvulnerable(true, true);

			if (Fighter.animator && Fighter.animator.runtimeAnimatorController)
			{
				Fighter.animator.SetTrigger("Wakeup");
			}
		}

		public override void OnExit()
		{
			Fighter.SetUpperLowerInvulnerable(false, false);
		}

		public override void OnTick()
		{
			remainingSeconds -= Time.deltaTime;

			// Allow quick direction adjustment during first half / 起身前半段允许方向微调
			var pendingCommands = Fighter.PendingCommands;
			float totalInvuln = (Fighter.stats != null) ? Fighter.stats.wakeupInvuln : LocomotionTuning.DefaultWakeupInvulnerability;
			float halfDuration = totalInvuln * 0.5f;

			if (remainingSeconds > 0f && remainingSeconds > (totalInvuln - halfDuration))
			{
				float direction = 0f;

				if (pendingCommands.moveX > LocomotionTuning.WakeupDirectionThreshold)
				{
					// Forward roll / 前滚
					direction = Fighter.facingRight ? 1f : -1f;
				}
				else if (pendingCommands.moveX < -LocomotionTuning.WakeupDirectionThreshold)
				{
					// Backrise / 后撤起身
					direction = Fighter.facingRight ? -1f : 1f;
				}

				if (Mathf.Abs(direction) > 0.1f)
				{
					Fighter.AddExternalImpulse(direction * LocomotionTuning.WakeupImpulseMagnitude);
				}
			}

			if (remainingSeconds <= 0f)
			{
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	/// <summary>
	/// Short invulnerable dodge (teleport or displacement).
	/// 短暂无敌的闪避（位移或瞬移）
	/// </summary>
	public class DodgeState : HState
	{
		private float remainingSeconds;

		public DodgeState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Dodge"; }
		}

		public override void OnEnter()
		{
			remainingSeconds = Fighter.Stats.dodgeDuration;
			Fighter.TryPerformDodgeTeleport(Fighter.PendingCommands.moveX);
			Fighter.StartDodge();
		}

		public override void OnTick()
		{
			remainingSeconds -= Time.deltaTime;

			if (remainingSeconds <= 0f)
			{
				var movementLocomotion = Fighter.HRoot?.Movement?.Locomotion;
				if (movementLocomotion != null)
				{
					Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Idle);
				}
			}
		}
	}

	/// <summary>
	/// Simple throw attempt with opponent tech window.
	/// 简单投技流程，包含对手拆招窗口
	/// </summary>
	public class ThrowState : HState
	{
		private readonly LocomotionState locomotionState;
		private float remainingDelaySeconds;

		public ThrowState(FightingGame.Combat.Actors.FighterActor actor, HState parent, LocomotionState locomotionReference = null) : base(actor, parent)
		{
			locomotionState = locomotionReference;
		}

		public override string Name
		{
			get { return "Throw"; }
		}

		public override void OnEnter()
		{
			remainingDelaySeconds = LocomotionTuning.ThrowResolveDelaySeconds;
			Fighter.SetAnimatorBool("Throw", true);
		}

		public override void OnTick()
		{
			remainingDelaySeconds -= Time.deltaTime;

			if (remainingDelaySeconds <= 0f)
			{
				var opponentActor = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;

				if (opponentActor != null && Fighter.IsOpponentInThrowRange(LocomotionTuning.DefaultThrowRange))
				{
					opponentActor.StartThrowTechWindow(LocomotionTuning.ThrowTechWindowSeconds);

					if (!opponentActor.WasTechTriggeredAndClear())
					{
						Fighter.ApplyThrowOn(opponentActor);
					}
				}

				Fighter.SetAnimatorBool("Throw", false);

				if (locomotionState != null)
				{
					// Return via locomotion sub-machine / 通过移动子状态机返回
					locomotionState.Machine.ChangeState(locomotionState.Grounded.Idle);
				}
				else
				{
					// Fallback routing via root machine / 通过根状态机的兜底路由
					var movementLocomotion = Fighter.HRoot?.Movement?.Locomotion;
					if (movementLocomotion != null)
					{
						Fighter.HMachine.ChangeState(movementLocomotion.Grounded.Idle);
					}
				}
			}
		}
	}

	/// <summary>
	/// In-air jump control with offense hooks.
	/// 空中跳跃控制，并可转入空中攻击
	/// </summary>
	public class JumpAirState : HState
	{
		public JumpAirState(FightingGame.Combat.Actors.FighterActor actor, HState parent) : base(actor, parent) { }

		public override string Name
		{
			get { return "Jump"; }
		}

		public override void OnTick()
		{
			LocomotionState locomotionState = Parent.Parent as LocomotionState;
			AirState airState = Parent as AirState;

			if (Fighter.IsGrounded())
			{
				locomotionState.Machine.ChangeState(locomotionState.Grounded.Idle);
				return;
			}

			var pendingCommands = Fighter.PendingCommands;

			if (Mathf.Abs(pendingCommands.moveX) > LocomotionTuning.MovementDeadZone)
			{
				Fighter.AirMove(pendingCommands.moveX);
			}

			if (pendingCommands.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				return;
			}

			if (pendingCommands.light)
			{
				locomotionState.Machine.ChangeState(airState.AirLight);
				return;
			}

			if (pendingCommands.heavy)
			{
				locomotionState.Machine.ChangeState(airState.AirHeavy);
				return;
			}
		}
	}
}