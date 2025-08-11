using UnityEngine;
using Combat;
using Fighter.FSM;
using Fighter.States;
using Data;
using Systems;
using System;
using System.Collections.Generic;
using Fighter.Core;

namespace Fighter {
    /// <summary>
    /// Central gameplay component for a fighter.
    /// Owns runtime state (HP/Meter/Facing), the finite state machine (idle/walk/crouch/jump/block/dodge/attack/hitstun/KO),
    /// references to colliders and animator, and exposes utility methods used by hit/guard/combo systems.
    /// High-level responsibilities:
    /// - Movement and facing control
    /// - Attack lifecycle and hitbox toggling (driven by FSM and/or animation events)
    /// - Damage intake, block evaluation, hitstun/knockdown transitions, and hit-stop freeze
    /// - Resource changes (meter gain/spend, optional heal on special)
    /// - Basic visual debug (attack active color, auto-face)
    /// </summary>
    public enum FighterTeam { Player, AI }

    /// <summary>
    /// Normalized command snapshot provided each frame by an input source (player, AI, or feeder).
    /// </summary>
    public struct FighterCommands {
        /// <summary>Horizontal movement input in range [-1, 1].</summary>
        public float moveX;
        /// <summary>Pressed this frame to attempt a jump (consumed if grounded).</summary>
        public bool jump, crouch, light, heavy, block, dodge;
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterController : MonoBehaviour {
        [Header("Refs")]
        /// <summary>Core character stat block (HP, speeds, gravity, chip ratio, dodge durations).</summary>
        public FighterStats stats;
        /// <summary>Move database mapped by trigger name (e.g., "Light", "Heavy", "Super").</summary>
        public MoveSet moveSet;
        /// <summary>Transform of the current opponent; used for auto-facing and distance logic.</summary>
        public Transform opponent;
        /// <summary>Animator driving visual state and animation triggers.</summary>
        public Animator animator;
        /// <summary>RigidBody2D used for motion/physics queries.</summary>
        public Rigidbody2D rb;
        /// <summary>Main body collider used for ground checks. Optional but recommended.</summary>
        public CapsuleCollider2D bodyCollider;
        /// <summary>All hurtboxes that can be struck. These are toggled per-region via invulnerability flags.</summary>
        public Hurtbox[] hurtboxes;
        /// <summary>All hitboxes toggled on during the active frames of an attack.</summary>
        public Hitbox[] hitboxes;
        public FighterTeam team = FighterTeam.Player;

        // Damage events
        /// <summary>Raised on the victim when real damage (after block) was applied.</summary>
        public event Action<FighterController> OnDamaged; // invoked on victim when real damage dealt
        /// <summary>Raised for any damage event (victim, attacker), useful for global UI counters.</summary>
        public static event Action<FighterController, FighterController> OnAnyDamage; // (victim, attacker)

        [Header("Physics")]
        /// <summary>Layer mask considered ground for grounded checks.</summary>
        public LayerMask groundMask = ~0; // default: everything

        [Header("Runtime")]
        /// <summary>Current hit points (HP).</summary>
        public int currentHealth;
        /// <summary>Current super/ability meter.</summary>
        public int meter;
        /// <summary>Maximum meter value.</summary>
        public int maxMeter = 1000;
        /// <summary>True if the fighter currently faces right; transforms scale X by +/-1 accordingly.</summary>
        public bool facingRight = true;
        /// <summary>True if the fighter is in crouch state.</summary>
        public bool IsCrouching { get; set; }
        /// <summary>Upper-body invulnerability flag (e.g., during certain dodges/animations).</summary>
        public bool UpperBodyInvuln { get; private set; }
        /// <summary>Lower-body invulnerability flag.</summary>
        public bool LowerBodyInvuln { get; private set; }

        /// <summary>The data of the move currently being executed, if any.</summary>
        public MoveData CurrentMove { get; private set; }

        /// <summary>The latest input snapshot provided by an input source.</summary>
        public FighterCommands PendingCommands { get; private set; }
        /// <summary>Finite state machine controlling fighter behavior.</summary>
        public StateMachine StateMachine { get; private set; }

        /// <summary>State singleton instances created at Awake.</summary>
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
        public States.PreJumpState PreJump { get; private set; }
        public States.LandingState Landing { get; private set; }
        public States.DashState Dash { get; private set; }
        public States.BackdashState Backdash { get; private set; }
        public States.ThrowState Throw { get; private set; }
        public States.WakeupState Wakeup { get; private set; }

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

        int jumpsUsed;
        bool dashRequested; bool dashBack;

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
            PreJump = new States.PreJumpState(this);
            Landing = new States.LandingState(this);
            Dash = new States.DashState(this);
            Backdash = new States.BackdashState(this);
            Throw = new States.ThrowState(this);
            Wakeup = new States.WakeupState(this);
        }

        /// <summary>
        /// Fast check to ensure animator is present and has a runtime controller.
        /// </summary>
        private bool AnimatorReady() {
            return animator != null && animator.runtimeAnimatorController != null;
        }

        private void Start() { StateMachine.SetState(Idle); }

        private void Update() {
            // delegate visuals and locomotion helpers
            var loco = GetComponent<FighterLocomotion>();
            if (loco) loco.ApplyFreezeVisual(IsFrozen()); else ApplyFreezeVisual();
            if (loco) loco.AutoFaceOpponent(); else AutoFaceOpponent();

            // reset air jump counter when grounded
            if (IsGrounded()) jumpsUsed = 0;

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
                var loco = GetComponent<FighterLocomotion>();
                if (loco) loco.NudgeHorizontal(externalImpulseX);
                else {
                    var pos = rb.position;
                    float targetX = pos.x + externalImpulseX;
                    rb.MovePosition(new Vector2(targetX, pos.y));
                }
                externalImpulseX = 0f;
            }
        }

        /// <summary>
        /// True while the fighter is in hit-stop/freeze frames.
        /// </summary>
        bool IsFrozen() => FrameClock.Now < freezeUntilFrame;

        /// <summary>
        /// Apply freeze-time visual and physics simulation pause.
        /// </summary>
        void ApplyFreezeVisual() {
            bool freeze = IsFrozen();
            if (animator) animator.speed = freeze ? 0f : 1f;
            rb.simulated = !freeze;
        }

        /// <summary>
        /// Enter a temporary freeze measured in frames. Used for hit-stop feedback.
        /// </summary>
        public void FreezeFrames(int frames) {
            if (frames <= 0) return;
            if (!IsFrozen()) cachedVelocity = rb.velocity;
            freezeUntilFrame = Mathf.Max(freezeUntilFrame, FrameClock.Now + frames);
        }

        /// <summary>
        /// Called by inputs (player/AI) to provide the command snapshot for this frame.
        /// </summary>
        public void SetCommands(in FighterCommands cmd) { PendingCommands = cmd; }

        /// <summary>
        /// Grounded horizontal movement; also transitions Idle/Walk as needed.
        /// </summary>
        public void Move(float x) {
            var loco = GetComponent<FighterLocomotion>();
            if (loco) loco.Move(x); else rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y);
            // State transitions are controlled in Idle/Walk states to avoid thrashing here.
        }
        /// <summary>Immediate halt of horizontal velocity.</summary>
        public void HaltHorizontal() { var loco = GetComponent<FighterLocomotion>(); if (loco) loco.HaltHorizontal(); else rb.velocity = new Vector2(0, rb.velocity.y); }
        /// <summary>Airborne horizontal control.</summary>
        public void AirMove(float x) { var loco = GetComponent<FighterLocomotion>(); if (loco) loco.AirMove(x); else rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y); }
        /// <summary>Attempt to jump if grounded and update animator.</summary>
        public bool CanJump() {
            if (IsGrounded()) return true;
            return jumpsUsed < 1; // allow one additional air jump (double-jump total = 2)
        }
        public void DoJump() {
            // increment air jump counter only when not grounded
            if (!IsGrounded()) jumpsUsed++;
            var loco = GetComponent<FighterLocomotion>();
            if (loco) loco.Jump();
            else { rb.velocity = new Vector2(rb.velocity.x, stats != null ? stats.jumpForce : 12f); if (AnimatorReady()) animator.SetTrigger("Jump"); }
        }
        public void Jump() { if (CanJump()) DoJump(); }

        /// <summary>
        /// Entry point for starting an attack by trigger name (e.g., "Light"). Applies meter gates and optional heal.
        /// </summary>
        public void TriggerAttack(string trigger) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.TriggerAttack(trigger); return; }
            debugMoveName = trigger;
            if (moveSet != null) CurrentMove = moveSet.Get(trigger);
            if (CurrentMove != null && CurrentMove.meterCost > 0) { if (!ConsumeMeter(CurrentMove.meterCost)) { CurrentMove = null; return; } }
            if (CurrentMove != null && CurrentMove.healAmount > 0) { AddHealth(CurrentMove.healAmount); }
            if (AnimatorReady()) animator.SetTrigger(trigger);
        }
        /// <summary>Clear the current move reference (typically on exit/recovery).</summary>
        public void ClearCurrentMove() { CurrentMove = null; debugMoveName = null; }
        /// <summary>Toggle all hitboxes on/off for the active frames.</summary>
        public void SetAttackActive(bool on) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.SetAttackActive(on); return; }
            debugHitActive = on;
            if (on) { attackInstanceId++; hitVictims.Clear(); hitStopApplied = false; }
            if (hitboxes == null) return;
            foreach (var h in hitboxes) if (h != null) h.SetActive(on);
            if (srHasVisual) srVisual.color = on ? Color.yellow : srDefaultColor;
        }

        /// <summary>
        /// Checks per-attack victim gating to avoid multi-hit registering the same target once per activation.
        /// </summary>
        public bool CanHitTarget(FighterController target) {
            if (target == null) return false;
            if (hitVictims.Contains(target)) return false;
            hitVictims.Add(target); return true;
        }

        /// <summary>Queue a pending combo cancel into another trigger.</summary>
        public void RequestComboCancel(string trigger) { pendingCancelTrigger = trigger; hasPendingCancel = true; }
        /// <summary>Consume pending combo cancel if present.</summary>
        public bool TryConsumeComboCancel(out string trigger) { trigger = null; if (!hasPendingCancel) return false; hasPendingCancel = false; trigger = pendingCancelTrigger; return true; }

        /// <summary>Utility check for animator tag on layer 0.</summary>
        public bool AnimatorIsTag(string tag) { return animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag(tag); }

        /// <summary>Adapter for new core components: set current move data.</summary>
        public void SetCurrentMove(Data.MoveData md) { CurrentMove = md; }
        /// <summary>Adapter for new core components: increment attack instance id for per-activation gating.</summary>
        public void IncrementAttackInstanceId() { attackInstanceId++; }
        /// <summary>Adapter for new core components: clear per-attack victim set.</summary>
        public void ClearHitVictimsSet() { hitVictims.Clear(); }
        /// <summary>Adapter for new core components: set active color on visual sprite.</summary>
        public void SetActiveColor(bool on) { if (srHasVisual) srVisual.color = on ? Color.yellow : srDefaultColor; }
        /// <summary>Adapter for new core components: add horizontal impulse to be applied in FixedUpdate.</summary>
        public void AddExternalImpulse(float dx) { externalImpulseX += dx; }
        /// <summary>Adapter for new core components: raise damage events.</summary>
        public void RaiseDamaged(FighterController attacker) { OnDamaged?.Invoke(this); OnAnyDamage?.Invoke(this, attacker); }

        /// <summary>Add to the meter (clamped to [0, maxMeter]).</summary>
        public void AddMeter(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) res.AddMeter(value); else meter = Mathf.Clamp(meter + value, 0, maxMeter); }
        /// <summary>Consume meter if available; returns true on success.</summary>
        public bool ConsumeMeter(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) return res.ConsumeMeter(value); if (meter < value) return false; meter -= value; return true; }
        /// <summary>Restore HP by value, clamped to max HP.</summary>
        public void AddHealth(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) res.AddHealth(value); else { int maxHp = stats != null ? stats.maxHealth : 100; currentHealth = Mathf.Clamp(currentHealth + value, 0, maxHp); } }

        /// <summary>
        /// Evaluate if a given incoming hit can be blocked given current command/grounding/crouch.
        /// </summary>
        public bool CanBlock(DamageInfo info) {
            return GuardEvaluator.CanBlock(PendingCommands.block, IsGrounded(), IsCrouching, info.level);
        }

        /// <summary>Set upper-body invulnerability for this frame series.</summary>
        public void SetUpperBodyInvuln(bool on) { UpperBodyInvuln = on; }
        /// <summary>Set lower-body invulnerability for this frame series.</summary>
        public void SetLowerBodyInvuln(bool on) { LowerBodyInvuln = on; }
        /// <summary>Adapter for new core components: invuln toggles via FighterResources.</summary>
        public void SetUpperLowerInvuln(bool up, bool low) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) { res.SetUpperBodyInvuln(up); res.SetLowerBodyInvuln(low);} else { UpperBodyInvuln = up; LowerBodyInvuln = low; } }

        /// <summary>
        /// Per-frame toggling of hurtboxes according to invulnerability flags and regions.
        /// </summary>
        void UpdateHurtboxEnable() {
            if (hurtboxes == null) return;
            foreach (var hb in hurtboxes) if (hb != null) {
                bool enabled = true;
                if (hb.region == Combat.HurtRegion.Head || hb.region == Combat.HurtRegion.Torso) enabled &= !UpperBodyInvuln;
                if (hb.region == Combat.HurtRegion.Legs) enabled &= !LowerBodyInvuln;
                hb.enabledThisFrame = enabled;
            }
        }

        /// <summary>
        /// Main damage intake flow: applies invulnerability, resolves block vs hit, updates HP/meter/pushback, transitions to Hitstun/KO as needed.
        /// </summary>
        public void TakeHit(DamageInfo info, FighterController attacker) {
            var recv = GetComponent<Fighter.Core.DamageReceiver>();
            if (recv) { recv.TakeHit(info, attacker); return; }
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

        /// <summary>
        /// Local feedback on successful contact: apply hit-stop and shake.
        /// </summary>
        public void OnHitConfirmedLocal(float seconds) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.OnHitConfirmedLocal(seconds); return; }
            if (hitStopApplied) return;
            hitStopApplied = true;
            int frames = FrameClock.SecondsToFrames(seconds);
            FreezeFrames(frames);
            Systems.CameraShaker.Instance?.Shake(0.1f, seconds);
        }

        /// <summary>
        /// Start a short dodge: trigger animation, grant brief invulnerability, and step in facing direction.
        /// </summary>
        public void StartDodge() {
            if (AnimatorReady()) animator.SetTrigger("Dodge");
            float dir = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(dir * (stats != null ? stats.walkSpeed * 1.5f : 9f), rb.velocity.y);
            float inv = stats != null ? stats.dodgeInvuln : 0.2f;
            StartCoroutine(DodgeInvuln(inv));
        }

        System.Collections.IEnumerator DodgeInvuln(float seconds) {
            SetUpperLowerInvuln(true, true);
            int frames = FrameClock.SecondsToFrames(seconds);
            int until = FrameClock.Now + frames;
            while (FrameClock.Now < until) { yield return new WaitForFixedUpdate(); }
            SetUpperLowerInvuln(false, false);
        }

        /// <summary>
        /// Auto-face the opponent by flipping local scale X around center.
        /// </summary>
        void AutoFaceOpponent() {
            if (!opponent) return;
            bool shouldFaceRight = transform.position.x <= opponent.position.x;
            if (shouldFaceRight != facingRight) { facingRight = shouldFaceRight; var s = transform.localScale; s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1); transform.localScale = s; }
        }

        /// <summary>
        /// Checks for ground using either body collider bounds or a short ray/box below the character.
        /// </summary>
        public bool IsGrounded() {
            var loco = GetComponent<FighterLocomotion>();
            if (loco) return loco.IsGrounded(groundMask);
            if (bodyCollider == null) return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
            var b = bodyCollider.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
        }

        /// <summary>Convenience wrapper to set animator booleans safely.</summary>
        public void SetAnimatorBool(string key, bool v) { if (AnimatorReady()) animator.SetBool(key, v); }
        /// <summary>Adapter for new core components: set debug move name for UI/telemetry.</summary>
        public void SetDebugMoveName(string name) { debugMoveName = name; }
        /// <summary>Adapter for new core components: set debug hit active flag.</summary>
        public void SetDebugHitActive(bool on) { debugHitActive = on; }

        public bool TryConsumeDashRequest(out bool isBack) { if (!dashRequested) { isBack = false; return false; } isBack = dashBack; dashRequested = false; return true; }
        public void RequestDash(bool back) { dashRequested = true; dashBack = back; }

        public void ApplyThrowOn(FighterController victim) {
            if (victim == null) return;
            var info = new Combat.DamageInfo {
                damage = 6,
                hitstun = 0.2f,
                blockstun = 0f,
                canBeBlocked = false,
                hitstopOnHit = 0.08f,
                pushbackOnHit = 0.2f,
            };
            victim.TakeHit(info, this);
        }
    }
}