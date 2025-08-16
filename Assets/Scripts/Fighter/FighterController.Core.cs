using UnityEngine;
using FightingGame.Combat;
using Data;
using Systems;
using System;
using System.Collections.Generic;
using Fighter;

namespace FightingGame.Combat.Actors
{
	public partial class FighterActor : MonoBehaviour
	{
		[Header("Refs")] public FighterStats stats;
		public Data.CombatActionSet actionSet;
		public Transform opponent;
		public Animator animator;
		public new Rigidbody2D rigidbody2D;
		public CapsuleCollider2D bodyCollider;
		public Hurtbox[] hurtboxes;
		public Hitbox[] hitboxes;
		public FighterTeam team = FighterTeam.Player;
		// Events
		public event Action<FighterActor> OnDamaged;
		public static event Action<FighterActor, FighterActor> OnAnyDamage;
		public event Action<string, string> OnStateChanged;
		[Header("Physics")] public LayerMask groundMask = ~0;
		[Header("Runtime")] public int currentHealth;
		public int meter;
		public int maxMeter = 1000;
		public bool facingRight = true;
		public bool IsCrouching
		{
			get;
			set;
		}
		public bool UpperBodyInvuln
		{
			get;
			private set;
		}
		public bool LowerBodyInvuln
		{
			get;
			private set;
		}
		// Block timing state
		float _blockHoldStartAt;
		float _blockLockedUntil;
		public bool IsBlockLocked() { return Time.time < _blockLockedUntil; }
		public float GetBlockHeldSeconds() { return _blockHoldStartAt > 0f ? Time.time - _blockHoldStartAt : 0f; }
		public Data.CombatActionDefinition CurrentMove
		{
			get;
			private set;
		}
		public FightingGame.Combat.Actors.FighterCommands PendingCommands
		{
			get;
			private set;
		}
		public FightingGame.Combat.State.HFSM.HStateMachine HMachine
		{
			get;
			private set;
		}
		public FightingGame.Combat.State.HFSM.RootState HRoot
		{
			get;
			private set;
		}
		public void SetCommands(in FightingGame.Combat.Actors.FighterCommands cmd)
		{
			PendingCommands = cmd;
			// 管理格擋時序：按下開始計時；鬆開重置
			if (cmd.block)
			{
				if (_blockHoldStartAt <= 0f)
				{
					_blockHoldStartAt = Time.time;
				}
			}
			else
			{
				_blockHoldStartAt = 0f;
			}
		}
		public FighterStats Stats => stats;
		string pendingCancelTrigger;
		bool hasPendingCancel;
		int freezeUntilFrame;
		Vector2 cachedVelocity;
		float externalImpulseX;
		public bool debugHitActive
		{
			get;
			private set;
		}
		public string debugMoveName
		{
			get;
			private set;
		}
		SpriteRenderer spriteRendererVisual;
		Color spriteRendererDefaultColor;
		bool hasSpriteRendererVisual;
		int attackInstanceId;
		readonly HashSet<FighterActor> hitVictims = new HashSet<FighterActor>();
		bool hitStopApplied;
		float hitConfirmTimer;
		float throwTechWindow;
		float ukemiWindow;
		bool techTriggered;
		Fighter.Core.JumpRule jumpRule;
		bool dashRequested;
		bool dashBack;

		void Awake()
		{
			rigidbody2D = GetComponent<Rigidbody2D>();
			animator = GetComponent<Animator>();
			if (animator == null) animator = gameObject.AddComponent<Animator>();
			if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider2D>();
			currentHealth = stats != null ? stats.maxHealth : 20000;
			rigidbody2D.gravityScale = stats != null ? stats.gravityScale : 4f;
			spriteRendererVisual = GetComponentInChildren<SpriteRenderer>();
			if (spriteRendererVisual != null)
			{
				hasSpriteRendererVisual = true;
				spriteRendererDefaultColor = spriteRendererVisual.color;
			}
			if (hurtboxes == null || hurtboxes.Length == 0) hurtboxes = GetComponentsInChildren<Hurtbox>(true);
			if (hitboxes == null || hitboxes.Length == 0) hitboxes = GetComponentsInChildren<Hitbox>(true);
			if (hurtboxes != null) foreach (var h in hurtboxes) if (h != null) h.owner = this;
			if (hitboxes != null) foreach (var h in hitboxes) if (h != null) h.owner = this;
			jumpRule = GetComponent<Fighter.Core.JumpRule>();
			if (!jumpRule) jumpRule = gameObject.AddComponent<Fighter.Core.JumpRule>();
			HMachine = new FightingGame.Combat.State.HFSM.HStateMachine();
			HRoot = new FightingGame.Combat.State.HFSM.RootState(this);
			HMachine.OnStateChanged += (name) => {
				OnStateChanged?.Invoke(name, debugMoveName ?? "");
			};
			if (HRoot != null && HRoot.Locomotion != null && HRoot.Locomotion.Machine != null)
			{
				HRoot.Locomotion.Machine.OnStateChanged += (name) => {
					OnStateChanged?.Invoke(name, debugMoveName ?? "");
				};
			}
		}
		void Start()
		{
			HMachine.SetInitial(HRoot, HRoot.Locomotion);
		}
		void Update()
		{
			var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
			if (locomotionController) locomotionController.ApplyFreezeVisual(IsFrozen());
			else ApplyFreezeVisual();
			if (locomotionController) locomotionController.AutoFaceOpponent();
			else AutoFaceOpponent();
			if (jumpRule != null) jumpRule.Tick(IsGrounded(), PendingCommands.jump);
			if (hitConfirmTimer > 0f) hitConfirmTimer -= Time.deltaTime;
			if (throwTechWindow > 0f) throwTechWindow -= Time.deltaTime;
			if (ukemiWindow > 0f) ukemiWindow -= Time.deltaTime;
			if (throwTechWindow > 0f && PendingCommands.light) ConsumeTech();
			UpdateHurtboxEnable();
			if (!IsFrozen())
			{
				if (HMachine != null) HMachine.Tick();
			}
			if (animator != null && animator.runtimeAnimatorController != null)
			{
				animator.SetFloat("SpeedX", Mathf.Abs(rigidbody2D.velocity.x));
				animator.SetBool("Grounded", IsGrounded());
				animator.SetBool("Crouch", IsCrouching);
				animator.SetFloat("VelY", rigidbody2D.velocity.y);
				animator.SetInteger("HP", currentHealth);
				animator.SetInteger("Meter", meter);
			}
		}
		void FixedUpdate()
		{
			if (IsFrozen()) return;
			if (Mathf.Abs(externalImpulseX) > 0.0001f) {
				var locomotionController = GetComponent<Fighter.Core.FighterLocomotion>();
				if (locomotionController) locomotionController.NudgeHorizontal(externalImpulseX);
				else rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x + externalImpulseX, rigidbody2D.velocity.y);
				externalImpulseX = 0f;
			}
		}
		bool AnimatorReady()
		{
			return animator != null && animator.runtimeAnimatorController != null;
		}
		public bool IsFrozen()
		{
			return FrameClock.Now < freezeUntilFrame;
		}
		public void FreezeFrames(int frames)
		{
			if (frames <= 0) return;
			if (!IsFrozen()) cachedVelocity = rigidbody2D.velocity;
			freezeUntilFrame = Mathf.Max(freezeUntilFrame, FrameClock.Now + frames);
			rigidbody2D.velocity = Vector2.zero;
		}
		void ApplyFreezeVisual()
		{
			if (animator) animator.speed = IsFrozen() ? 0f : 1f;
			if (rigidbody2D) rigidbody2D.simulated = !IsFrozen();
		}
		// Editor-only SendMessage target to lock block
		void __LockBlockForSeconds(float seconds)
		{
			if (seconds <= 0f)
			{
				return;
			}
			_blockLockedUntil = Time.time + seconds;
			_blockHoldStartAt = 0f;
		}
	}
}