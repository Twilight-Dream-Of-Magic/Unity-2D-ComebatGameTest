using UnityEngine;
using Combat;
using Fighter.FSM;
using Fighter.States;
using Data;
using Systems;
using System;
using System.Collections.Generic;

namespace Fighter {
    public enum FighterTeam { Player, AI }

    public struct FighterCommands {
        public float moveX;
        public bool jump, crouch, light, heavy, block, dodge;
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterController : MonoBehaviour {
        [Header("Refs")]
        public FighterStats stats;
        public MoveSet moveSet;
        public Transform opponent;
        public Animator animator;
        public Rigidbody2D rb;
        public CapsuleCollider2D bodyCollider;
        public Hurtbox[] hurtboxes;
        public Hitbox[] hitboxes;
        public FighterTeam team = FighterTeam.Player;

        // Damage events
        public event Action<FighterController> OnDamaged; // invoked on victim when real damage dealt
        public static event Action<FighterController, FighterController> OnAnyDamage; // (victim, attacker)

        [Header("Physics")]
        public LayerMask groundMask = ~0; // default: everything

        [Header("Runtime")]
        public int currentHealth;
        public int meter;
        public int maxMeter = 1000;
        public bool facingRight = true;
        public bool IsCrouching { get; set; }
        public bool UpperBodyInvuln { get; private set; }
        public bool LowerBodyInvuln { get; private set; }

        public MoveData CurrentMove { get; private set; }

        public FighterCommands PendingCommands { get; private set; }
        public StateMachine StateMachine { get; private set; }

        public IdleState Idle { get; private set; }
        public WalkState Walk { get; private set; }
        public CrouchState Crouch { get; private set; }
        public JumpAirState JumpState { get; private set; }
        public BlockState Block { get; private set; }
        public DodgeState Dodge { get; private set; }
        public AttackState AttackLight { get; private set; }
        public AttackState AttackHeavy { get; private set; }
        public HitstunState Hitstun { get; private set; }
        public KnockdownState KO { get; private set; }

        public FighterStats Stats => stats;

        string pendingCancelTrigger;
        bool hasPendingCancel;

        int freezeUntilFrame;
        Vector2 cachedVelocity;
        float externalImpulseX;

        public bool debugHitActive { get; private set; }
        public string debugMoveName { get; private set; }
        SpriteRenderer srVisual; Color srDefaultColor; bool srHasVisual;

        // per-attack gating
        int attackInstanceId;
        readonly HashSet<FighterController> hitVictims = new HashSet<FighterController>();
        bool hitStopApplied;

        private void Awake() {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider2D>();
            currentHealth = stats != null ? stats.maxHealth : 100;
            rb.gravityScale = stats != null ? stats.gravityScale : 4f;

            srVisual = GetComponentInChildren<SpriteRenderer>();
            if (srVisual != null) { srHasVisual = true; srDefaultColor = srVisual.color; }

            if (hurtboxes != null) foreach (var h in hurtboxes) if (h != null) h.owner = this;
            if (hitboxes != null) foreach (var h in hitboxes) if (h != null) h.owner = this;

            StateMachine = new StateMachine();
            Idle = new IdleState(this);
            Walk = new WalkState(this);
            Crouch = new CrouchState(this);
            JumpState = new JumpAirState(this);
            Block = new BlockState(this);
            Dodge = new DodgeState(this);
            AttackLight = new AttackState(this, "Light");
            AttackHeavy = new AttackState(this, "Heavy");
            Hitstun = new HitstunState(this);
            KO = new KnockdownState(this);
        }

        private bool AnimatorReady() {
            return animator != null && animator.runtimeAnimatorController != null;
        }

        private void Start() { StateMachine.SetState(Idle); }

        private void Update() {
            ApplyFreezeVisual();
            AutoFaceOpponent();
            UpdateHurtboxEnable();
            if (!IsFrozen() && StateMachine != null) StateMachine.Tick();
            if (AnimatorReady()) {
                animator.SetFloat("SpeedX", Mathf.Abs(rb.velocity.x));
                animator.SetBool("Grounded", IsGrounded());
                animator.SetBool("Crouch", IsCrouching);
                animator.SetFloat("VelY", rb.velocity.y);
                animator.SetInteger("HP", currentHealth);
                animator.SetInteger("Meter", meter);
            }
        }

        private void FixedUpdate() {
            if (IsFrozen()) return;
            if (Mathf.Abs(externalImpulseX) > 0.0001f) {
                var pos = rb.position;
                float targetX = pos.x + externalImpulseX;
                rb.MovePosition(new Vector2(targetX, pos.y));
                externalImpulseX = 0f;
            }
        }

        bool IsFrozen() => FrameClock.Now < freezeUntilFrame;
        void ApplyFreezeVisual() {
            bool freeze = IsFrozen();
            if (animator) animator.speed = freeze ? 0f : 1f;
            rb.simulated = !freeze;
        }

        public void FreezeFrames(int frames) {
            if (frames <= 0) return;
            if (!IsFrozen()) cachedVelocity = rb.velocity;
            freezeUntilFrame = Mathf.Max(freezeUntilFrame, FrameClock.Now + frames);
        }

        public void SetCommands(in FighterCommands cmd) { PendingCommands = cmd; }

        public void Move(float x) {
            rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y);
            if (Mathf.Abs(x) > 0.01f && StateMachine.Current != Walk) StateMachine.SetState(Walk);
            if (Mathf.Abs(x) <= 0.01f && StateMachine.Current == Walk) StateMachine.SetState(Idle);
        }
        public void HaltHorizontal() { rb.velocity = new Vector2(0, rb.velocity.y); }
        public void AirMove(float x) { rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y); }
        public void Jump() { rb.velocity = new Vector2(rb.velocity.x, stats != null ? stats.jumpForce : 12f); if (AnimatorReady()) animator.SetTrigger("Jump"); }

        public void TriggerAttack(string trigger) {
            debugMoveName = trigger;
            if (moveSet != null) CurrentMove = moveSet.Get(trigger);
            // meter gate
            if (CurrentMove != null && CurrentMove.meterCost > 0) {
                if (!ConsumeMeter(CurrentMove.meterCost)) { CurrentMove = null; return; }
            }
            // apply heal-on-use
            if (CurrentMove != null && CurrentMove.healAmount > 0) {
                AddHealth(CurrentMove.healAmount);
            }
            if (AnimatorReady()) animator.SetTrigger(trigger);
        }
        public void ClearCurrentMove() { CurrentMove = null; debugMoveName = null; }
        public void SetAttackActive(bool on) {
            debugHitActive = on;
            if (on) { attackInstanceId++; hitVictims.Clear(); hitStopApplied = false; }
            if (hitboxes == null) return;
            foreach (var h in hitboxes) if (h != null) h.SetActive(on);
            if (srHasVisual) srVisual.color = on ? Color.yellow : srDefaultColor;
        }

        public bool CanHitTarget(FighterController target) {
            if (target == null) return false;
            if (hitVictims.Contains(target)) return false;
            hitVictims.Add(target); return true;
        }

        public void RequestComboCancel(string trigger) { pendingCancelTrigger = trigger; hasPendingCancel = true; }
        public bool TryConsumeComboCancel(out string trigger) { trigger = null; if (!hasPendingCancel) return false; hasPendingCancel = false; trigger = pendingCancelTrigger; return true; }

        public bool AnimatorIsTag(string tag) { return animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag(tag); }

        public void AddMeter(int value) { meter = Mathf.Clamp(meter + value, 0, maxMeter); }
        public bool ConsumeMeter(int value) { if (meter < value) return false; meter -= value; return true; }
        public void AddHealth(int value) { int maxHp = stats != null ? stats.maxHealth : 100; currentHealth = Mathf.Clamp(currentHealth + value, 0, maxHp); }

        public bool CanBlock(DamageInfo info) {
            return GuardEvaluator.CanBlock(PendingCommands.block, IsGrounded(), IsCrouching, info.level);
        }

        public void SetUpperBodyInvuln(bool on) { UpperBodyInvuln = on; }
        public void SetLowerBodyInvuln(bool on) { LowerBodyInvuln = on; }

        void UpdateHurtboxEnable() {
            if (hurtboxes == null) return;
            foreach (var hb in hurtboxes) if (hb != null) {
                bool enabled = true;
                if (hb.region == Combat.HurtRegion.Head || hb.region == Combat.HurtRegion.Torso) enabled &= !UpperBodyInvuln;
                if (hb.region == Combat.HurtRegion.Legs) enabled &= !LowerBodyInvuln;
                hb.enabledThisFrame = enabled;
            }
        }

        public void TakeHit(DamageInfo info, FighterController attacker) {
            if ((info.level == HitLevel.High || info.level == HitLevel.Overhead) && UpperBodyInvuln) return;
            if (info.level == HitLevel.Low && LowerBodyInvuln) return;

            var res = HitResolver.Resolve(info, CanBlock(info), stats != null ? stats.blockDamageRatio : 0.2f);
            int maxHp = stats != null ? stats.maxHealth : 100;
            int before = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - res.finalDamage, 0, maxHp);

            if (currentHealth == 0) { StateMachine.SetState(KO); if (AnimatorReady()) animator.SetTrigger("KO"); return; }

            float dir = Mathf.Sign(transform.position.x - attacker.transform.position.x);
            if (!res.wasBlocked) {
                rb.velocity = new Vector2(dir * info.knockback.x, info.knockback.y);
                if (AnimatorReady()) animator.SetTrigger("Hit");
                Hitstun.Begin(res.appliedStun);
                StateMachine.SetState(Hitstun);

                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnHit : 20);
                attacker.externalImpulseX += -dir * res.appliedPushback;

                if (currentHealth < before) { OnDamaged?.Invoke(this); OnAnyDamage?.Invoke(this, attacker); }
            } else {
                if (AnimatorReady()) animator.SetTrigger("BlockHit");
                Hitstun.Begin(res.appliedStun);
                StateMachine.SetState(Hitstun);

                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnBlock : 10);
                attacker.externalImpulseX += -dir * res.appliedPushback;
            }
        }

        public void OnHitConfirmedLocal(float seconds) {
            if (hitStopApplied) return;
            hitStopApplied = true;
            int frames = FrameClock.SecondsToFrames(seconds);
            FreezeFrames(frames);
            Systems.CameraShaker.Instance?.Shake(0.1f, seconds);
        }

        public void StartDodge() {
            if (AnimatorReady()) animator.SetTrigger("Dodge");
            float dir = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(dir * (stats != null ? stats.walkSpeed * 1.5f : 9f), rb.velocity.y);
            float inv = stats != null ? stats.dodgeInvuln : 0.2f;
            StartCoroutine(DodgeInvuln(inv));
        }

        System.Collections.IEnumerator DodgeInvuln(float seconds) {
            SetUpperBodyInvuln(true); SetLowerBodyInvuln(true);
            int frames = FrameClock.SecondsToFrames(seconds);
            int until = FrameClock.Now + frames;
            while (FrameClock.Now < until) { yield return new WaitForFixedUpdate(); }
            SetUpperBodyInvuln(false); SetLowerBodyInvuln(false);
        }

        void AutoFaceOpponent() {
            if (!opponent) return;
            bool shouldFaceRight = transform.position.x <= opponent.position.x;
            if (shouldFaceRight != facingRight) { facingRight = shouldFaceRight; var s = transform.localScale; s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1); transform.localScale = s; }
        }

        public bool IsGrounded() {
            if (bodyCollider == null) return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
            var b = bodyCollider.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
        }

        public void SetAnimatorBool(string key, bool v) { if (AnimatorReady()) animator.SetBool(key, v); }
    }
}