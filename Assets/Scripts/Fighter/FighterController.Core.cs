using System;
using System.Collections.Generic;
using UnityEngine;
using Systems;
using Fighter;

namespace FightingGame.Combat.Actors
{
	/// <summary>
	/// Core runtime behaviour for a fighter actor.
	/// 角色核心运行时行为。
	/// </summary>
	public partial class FighterActor : MonoBehaviour
	{
		[Header("References")]
		/// <summary>
		/// Character statistics and configuration data.
		/// 角色属性与配置数据。
		/// </summary>
		public FighterStats stats;

		/// <summary>
		/// Action set definitions used by this actor.
		/// 本角色使用的动作集合定义。
		/// </summary>
		public Data.CombatActionSet actionSet;

		/// <summary>
		/// Transform of the current opponent.
		/// 当前对手的 Transform。
		/// </summary>
		public Transform opponent;

		/// <summary>
		/// Animator component reference.
		/// Animator 组件引用。
		/// </summary>
		public Animator animator;

		/// <summary>
		/// Physics rigidbody (2D) reference.
		/// 物理刚体（2D）引用。
		/// </summary>
		public new Rigidbody2D rigidbody2D;

		/// <summary>
		/// Collider representing body bounds.
		/// 表示角色身体边界的碰撞体。
		/// </summary>
		public CapsuleCollider2D bodyCollider;

		/// <summary>
		/// Hurtbox components attached to this actor.
		/// 附着在本角色上的受击箱组件。
		/// </summary>
		public Hurtbox[] hurtboxes;

		/// <summary>
		/// Hitbox components attached to this actor.
		/// 附着在本角色上的命中箱组件。
		/// </summary>
		public Hitbox[] hitboxes;

		/// <summary>
		/// Team affiliation for this actor.
		/// 角色的队伍归属。
		/// </summary>
		public FighterTeam team = FighterTeam.Player;

		// Events
		/// <summary>
		/// Event invoked when this actor is damaged.
		/// 当角色受到伤害时触发的事件。
		/// </summary>
		public event Action<FighterActor> OnDamaged;

		/// <summary>
		/// Static event invoked when any actor takes damage.
		/// 全局事件：当任意角色受到伤害时触发。
		/// </summary>
		public static event Action<FighterActor, FighterActor> OnAnyDamage;

		/// <summary>
		/// Event invoked when state machine state changes.
		/// 状态机状态改变时触发的事件。
		/// </summary>
		public event Action<string, string> OnStateChanged;

		[Header("Physics")]
		/// <summary>
		/// Layer mask considered as ground.
		/// 地面判定的图层掩码。
		/// </summary>
		public LayerMask groundMask = ~0;

		[Header("Runtime")]
		/// <summary>
		/// Current health points of this actor.
		/// 当前生命值。
		/// </summary>
		public int currentHealth;

		/// <summary>
		/// Resource meter value.
		/// 能量槽值（计量器）。
		/// </summary>
		public int meter;

		/// <summary>
		/// Faced direction. True means facing right.
		/// 面向方向。True 表示面向右。
		/// </summary>
		public bool facingRight = true;

		/// <summary>
		/// Whether the actor is currently crouching.
		/// 角色是否处于下蹲状态。
		/// </summary>
		public bool IsCrouching
		{
			get;
			set;
		}

		/// <summary>
		/// Upper body invulnerability flag.
		/// 上半身无敌标志（只读由方法控制）。
		/// </summary>
		public bool UpperBodyInvulnerable
		{
			get;
			private set;
		}

		/// <summary>
		/// Lower body invulnerability flag.
		/// 下半身无敌标志（只读由方法控制）。
		/// </summary>
		public bool LowerBodyInvulnerable
		{
			get;
			private set;
		}

		// Block timing state (private)
		private float blockHoldStartTime;
		private float blockLockedUntilTime;

		/// <summary>
		/// Check whether block is currently locked.
		/// 检查格挡是否处于锁定状态。
		/// </summary>
		public bool IsBlockLocked()
		{
			return Time.time < blockLockedUntilTime;
		}

		/// <summary>
		/// Get seconds that the block button has been held.
		/// 获取格挡按键被按住的秒数。
		/// </summary>
		public float GetBlockHeldSeconds()
		{
			if (blockHoldStartTime > 0f)
			{
				return Time.time - blockHoldStartTime;
			}

			return 0f;
		}

		/// <summary>
		/// Current action definition being executed.
		/// 当前正在执行的动作定义。
		/// </summary>
		public Data.CombatActionDefinition CurrentAction
		{
			get;
			private set;
		}

		/// <summary>
		/// Pending input commands for this actor.
		/// 待处理的输入命令。
		/// </summary>
		public FightingGame.Combat.Actors.FighterCommands PendingCommands
		{
			get;
			private set;
		}

		/// <summary>
		/// Hierarchical finite state machine instance.
		/// 层次化有限状态机实例。
		/// </summary>
		public FightingGame.Combat.State.HFSM.HStateMachine HMachine
		{
			get;
			private set;
		}

		/// <summary>
		/// Root state container for HFSM.
		/// HFSM 的根状态容器。
		/// </summary>
		public FightingGame.Combat.State.HFSM.RootState HRoot
		{
			get;
			private set;
		}

		/// <summary>
		/// Set pending input commands. This method also manages block timing.
		/// 设置待处理命令，同时管理格挡时序。
		/// </summary>
		public void SetCommands(in FightingGame.Combat.Actors.FighterCommands commands)
		{
			PendingCommands = commands;

			// Manage block timing: start timer when pressed, reset when released.
			if (commands.block)
			{
				if (blockHoldStartTime <= 0f)
				{
					blockHoldStartTime = Time.time;
				}
			}
			else
			{
				blockHoldStartTime = 0f;
			}
		}

		/// <summary>
		/// Expose stats safely.
		/// 安全地暴露属性数据。
		/// </summary>
		public FighterStats Stats => stats;

		// Internal runtime state fields
		private string pendingCancelTrigger;
		private bool hasPendingCancel;
		private int freezeUntilFrame;
		private Vector2 cachedVelocity;
		private float externalImpulseX;

		public bool DebugHitActive
		{
			get;
			private set;
		}

		public string DebugActionName
		{
			get;
			private set;
		}

		private SpriteRenderer spriteRendererVisual;
		private Color spriteRendererDefaultColor;
		private bool hasSpriteRendererVisual;
		private readonly HashSet<FighterActor> hitVictims = new HashSet<FighterActor>();
		private bool hitStopApplied;
		private float hitConfirmTimer;
		private float throwTechWindow;
		private float ukemiWindow;
		private bool techTriggered;
		private Fighter.Core.JumpRule jumpRule;
		private bool dashRequested;
		private bool dashBack;
		private float nextDodgeAllowedAt;

		/// <summary>
		/// Awake initialization: cache components and initialize HFSM.
		/// Awake 初始化：缓存组件并初始化 HFSM。
		/// </summary>
		private void Awake()
		{
			rigidbody2D = GetComponent<Rigidbody2D>();

			animator = GetComponent<Animator>();
			if (animator == null)
			{
				animator = gameObject.AddComponent<Animator>();
			}

			if (bodyCollider == null)
			{
				bodyCollider = GetComponent<CapsuleCollider2D>();
			}

			if (stats != null)
			{
				currentHealth = Mathf.Clamp(stats.maxHealth, stats.minHealth, stats.maxHealth);
			}
			else
			{
				currentHealth = 5000;
			}

			if (rigidbody2D != null)
			{
				if (stats != null)
				{
					rigidbody2D.gravityScale = stats.gravityScale;
				}
				else
				{
					rigidbody2D.gravityScale = 4f;
				}
			}

			spriteRendererVisual = GetComponentInChildren<SpriteRenderer>();
			if (spriteRendererVisual != null)
			{
				hasSpriteRendererVisual = true;
				spriteRendererDefaultColor = spriteRendererVisual.color;
			}

			if (hurtboxes == null || hurtboxes.Length == 0)
			{
				hurtboxes = GetComponentsInChildren<Hurtbox>(true);
			}

			if (hitboxes == null || hitboxes.Length == 0)
			{
				hitboxes = GetComponentsInChildren<Hitbox>(true);
			}

			if (hurtboxes != null)
			{
				for (int index = 0; index < hurtboxes.Length; index++)
				{
					Hurtbox hurtbox = hurtboxes[index];
					if (hurtbox != null)
					{
						hurtbox.owner = this;
					}
				}
			}

			if (hitboxes != null)
			{
				for (int index = 0; index < hitboxes.Length; index++)
				{
					Hitbox hitbox = hitboxes[index];
					if (hitbox != null)
					{
						hitbox.owner = this;
					}
				}
			}

			jumpRule = GetComponent<Fighter.Core.JumpRule>();
			if (jumpRule == null)
			{
				jumpRule = gameObject.AddComponent<Fighter.Core.JumpRule>();
			}

			HMachine = new FightingGame.Combat.State.HFSM.HStateMachine();
			HRoot = new FightingGame.Combat.State.HFSM.RootState(this);

			if (HMachine != null)
			{
				HMachine.OnStateChanged += (name) =>
				{
					if (OnStateChanged != null)
					{
						OnStateChanged.Invoke(name, DebugActionName ?? string.Empty);
					}
				};
			}

			if (HRoot != null && HRoot.Locomotion != null && HRoot.Locomotion.Machine != null)
			{
				HRoot.Locomotion.Machine.OnStateChanged += (name) =>
				{
					if (OnStateChanged != null)
					{
						OnStateChanged.Invoke(name, DebugActionName ?? string.Empty);
					}
				};
			}
		}

		/// <summary>
		/// Start state machine on Start.
		/// 在 Start 中设置 HFSM 初始状态。
		/// </summary>
		private void Start()
		{
			if (HMachine != null && HRoot != null)
			{
				HMachine.SetInitial(HRoot, HRoot.Locomotion);
			}
		}

		/// <summary>
		/// Per-frame update.
		/// 每帧更新逻辑。
		/// </summary>
		private void Update()
		{
			Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();

			if (locomotionController != null)
			{
				locomotionController.ApplyFreezeVisual(IsFrozen());
			}
			else
			{
				ApplyFreezeVisual();
			}

			if (locomotionController != null)
			{
				locomotionController.AutoFaceOpponent();
			}
			else
			{
				AutoFaceOpponent();
			}

			if (jumpRule != null)
			{
				jumpRule.Tick(IsGrounded(), PendingCommands.jump);
			}

			if (hitConfirmTimer > 0f)
			{
				hitConfirmTimer -= Time.deltaTime;
			}

			if (throwTechWindow > 0f)
			{
				throwTechWindow -= Time.deltaTime;
			}

			if (ukemiWindow > 0f)
			{
				ukemiWindow -= Time.deltaTime;
			}

			if (throwTechWindow > 0f && PendingCommands.light)
			{
				ConsumeTech();
			}

			UpdateHurtboxEnable();

			if (!IsFrozen())
			{
				if (HMachine != null)
				{
					HMachine.Tick();
				}
			}

			if (animator != null)
			{
				if (animator.runtimeAnimatorController != null)
				{
					animator.SetFloat("SpeedX", Mathf.Abs(rigidbody2D.velocity.x));
					animator.SetBool("Grounded", IsGrounded());
					animator.SetBool("Crouch", IsCrouching);
					animator.SetFloat("VelY", rigidbody2D.velocity.y);
					animator.SetInteger("HP", currentHealth);
					animator.SetInteger("Meter", meter);
				}
			}
		}

		/// <summary>
		/// FixedUpdate for physics-step operations.
		/// 物理帧更新逻辑。
		/// </summary>
		private void FixedUpdate()
		{
			if (IsFrozen())
			{
				return;
			}

			if (Mathf.Abs(externalImpulseX) > 0.0001f)
			{
				Fighter.Core.FighterLocomotion locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();

				if (locomotionController != null)
				{
					locomotionController.NudgeHorizontal(externalImpulseX);
				}
				else
				{
					rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x + externalImpulseX, rigidbody2D.velocity.y);
				}

				externalImpulseX = 0f;
			}
		}

		/// <summary>
		/// Check whether animator is ready to receive parameters.
		/// 检查 Animator 是否可用并已加载控制器。
		/// </summary>
		public bool AnimatorReady()
		{
			if (animator == null)
			{
				return false;
			}

			if (animator.runtimeAnimatorController == null)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Check whether the actor is currently frozen (hit stop frames).
		/// 检查角色是否处于冻结帧状态（命中停顿）。
		/// </summary>
		public bool IsFrozen()
		{
			return FrameClock.Now < freezeUntilFrame;
		}

		/// <summary>
		/// Apply freeze frames for a number of frames and cache velocity.
		/// 对指定帧数应用冻结，并缓存速度以便恢复。
		/// </summary>
		public void FreezeFrames(int frames)
		{
			if (frames <= 0)
			{
				return;
			}

			if (!IsFrozen())
			{
				cachedVelocity = rigidbody2D.velocity;
			}

			freezeUntilFrame = Math.Max(freezeUntilFrame, FrameClock.Now + frames);
			rigidbody2D.velocity = Vector2.zero;
		}

		/// <summary>
		/// Apply visual changes when frozen.
		/// 在冻结状态时应用可视化更改。
		/// </summary>
		private void ApplyFreezeVisual()
		{
			if (animator != null)
			{
				if (IsFrozen())
				{
					animator.speed = 0f;
				}
				else
				{
					animator.speed = 1f;
				}
			}

			if (rigidbody2D != null)
			{
				rigidbody2D.simulated = !IsFrozen();
			}
		}

		/// <summary>
		/// Editor-only helper to lock block for seconds via SendMessage.
		/// 编辑器专用：通过 SendMessage 锁定格挡若干秒。
		/// </summary>
		private void __LockBlockForSeconds(float seconds)
		{
			if (seconds <= 0f)
			{
				return;
			}

			blockLockedUntilTime = Time.time + seconds;
			blockHoldStartTime = 0f;
		}

		/// <summary>
		/// Check whether this actor can dodge now, based on cooldown.
		/// 根据冷却检查是否可以闪避。
		/// </summary>
		public bool CanDodge()
		{
			return Time.time >= nextDodgeAllowedAt;
		}

		/// <summary>
		/// Set dodge cooldown to a minimum of the provided seconds.
		/// 设置闪避冷却，取最大值以避免缩短已有冷却。
		/// </summary>
		public void SetDodgeCooldown(float seconds)
		{
			if (seconds <= 0f)
			{
				return;
			}

			nextDodgeAllowedAt = Math.Max(nextDodgeAllowedAt, Time.time + seconds);
		}
	}
}
