using UnityEngine;
using Combat;
// using Fighter.FSM;
using Data;
using Systems;
using System;
using System.Collections.Generic;
using Fighter.Core;

namespace Fighter {
    /// <summary>
    /// Central gameplay component for a fighter.
    /// Owns runtime state (HP/Meter/Facing), the hierarchical finite state machine (HFSM) controlling behavior,
    /// references to colliders and animator, and exposes utility methods used by hit/guard/combo systems.
    /// </summary>
    public enum FighterTeam { Player, AI }

    /// <summary>
    /// Normalized command snapshot provided each frame by an input source (player, AI, or feeder).
    /// </summary>
    public struct FighterCommands {
        public float moveX;
        public bool jump, crouch, light, heavy, block, dodge;
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(50)]
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
        public event Action<FighterController> OnDamaged;
        public static event Action<FighterController, FighterController> OnAnyDamage;
        public event Action<string,string> OnStateChanged; // (stateName, moveName)

        [Header("Physics")]
        public LayerMask groundMask = ~0;

        [Header("Runtime")]
        public int currentHealth;
        public int meter;
        public int maxMeter = 1000;
        public bool facingRight = true;
        public bool IsCrouching { get; set; }
        public bool UpperBodyInvuln { get; private set; }
        public bool LowerBodyInvuln { get; private set; }

        public Data.MoveData CurrentMove { get; private set; }
        public FighterCommands PendingCommands { get; private set; }

        // HFSM (authoritative)
        public Fighter.HFSM.HStateMachine HMachine { get; private set; }
        public Fighter.HFSM.RootState HRoot { get; private set; }

        public void SetCommands(in FighterCommands cmd) { PendingCommands = cmd; }

        public FighterStats Stats => stats;

        string pendingCancelTrigger;
        bool hasPendingCancel;

        int freezeUntilFrame;
        Vector2 cachedVelocity;
        float externalImpulseX;

        public bool debugHitActive { get; private set; }
        public string debugMoveName { get; private set; }
        SpriteRenderer srVisual; Color srDefaultColor; bool srHasVisual;

        int attackInstanceId;
        readonly HashSet<FighterController> hitVictims = new HashSet<FighterController>();
        bool hitStopApplied;

        float hitConfirmTimer;

        float throwTechWindow;
        float ukemiWindow;
        bool techTriggered;

        JumpRule jumpRule;
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

            // auto-discover hitboxes/hurtboxes if not assigned in inspector
            if (hurtboxes == null || hurtboxes.Length == 0) hurtboxes = GetComponentsInChildren<Hurtbox>(true);
            if (hitboxes == null || hitboxes.Length == 0) hitboxes = GetComponentsInChildren<Hitbox>(true);
            if (hurtboxes != null) foreach (var h in hurtboxes) if (h != null) h.owner = this;
            if (hitboxes != null) foreach (var h in hitboxes) if (h != null) h.owner = this;

            jumpRule = GetComponent<JumpRule>();
            if (!jumpRule) jumpRule = gameObject.AddComponent<JumpRule>();

            // HFSM init
            HMachine = new Fighter.HFSM.HStateMachine();
            HRoot = new Fighter.HFSM.RootState(this);
            HMachine.OnStateChanged += (name) => { OnStateChanged?.Invoke(name, debugMoveName ?? ""); };
        }

        private bool AnimatorReady() { return animator != null && animator.runtimeAnimatorController != null; }

        private void Start() { HMachine.SetInitial(HRoot, HRoot.Locomotion); }

        public void NotifyStateChanged() { OnStateChanged?.Invoke(GetCurrentStateName(), debugMoveName ?? ""); }

        // Animator Event hooks (to be called from throw animations)
        public void AE_ThrowStart() { }
        public void AE_ThrowConnect() { if (opponent) { var vic = opponent.GetComponent<FighterController>(); ApplyThrowOn(vic); StartThrowTechWindow(0.25f); } }
        public void AE_ThrowEnd() { }

        public void StartThrowTechWindow(float seconds) { throwTechWindow = Mathf.Max(throwTechWindow, seconds); }
        public bool CanTechThrow() { return throwTechWindow > 0f; }
        public void ConsumeTech() { throwTechWindow = 0f; techTriggered = true; }
        public bool WasTechTriggeredAndClear() { bool t = techTriggered; techTriggered = false; return t; }

        public void StartUkemiWindow(float seconds) { ukemiWindow = Mathf.Max(ukemiWindow, seconds); }
        public bool CanUkemi() { return ukemiWindow > 0f; }
        public void ConsumeUkemi() { ukemiWindow = 0f; }

        private void Update() {
            var loco = GetComponent<FighterLocomotion>();
            if (loco) loco.ApplyFreezeVisual(IsFrozen()); else ApplyFreezeVisual();
            if (loco) loco.AutoFaceOpponent(); else AutoFaceOpponent();

            if (jumpRule != null) jumpRule.Tick(IsGrounded(), PendingCommands.jump);

            if (hitConfirmTimer > 0f) hitConfirmTimer -= Time.deltaTime;
            if (throwTechWindow > 0f) throwTechWindow -= Time.deltaTime;
            if (ukemiWindow > 0f) ukemiWindow -= Time.deltaTime;
            if (throwTechWindow > 0f && PendingCommands.light) ConsumeTech();

            UpdateHurtboxEnable();
            if (!IsFrozen()) { if (HMachine != null) HMachine.Tick(); }
            if (AnimatorReady()) {
                animator.SetFloat("SpeedX", Mathf.Abs(rb.velocity.x));
                animator.SetBool("Grounded", IsGrounded());
                animator.SetBool("Crouch", IsCrouching);
                animator.SetFloat("VelY", rb.velocity.y);
                animator.SetInteger("HP", currentHealth);
                animator.SetInteger("Meter", meter);
            }
        }

        // HFSM helpers
        public string GetCurrentStateName() { return HMachine != null && HMachine.Current != null ? HMachine.Current.Name : string.Empty; }
        public void EnterAttackHFSM(string trigger) {
            var loc = HRoot.Locomotion;
            Fighter.HFSM.AttackState target = null;
            if (IsGrounded()) target = (trigger == "Light") ? loc.Grounded.AttackLight : loc.Grounded.AttackHeavy;
            else target = (trigger == "Light") ? loc.Air.AirLight : loc.Air.AirHeavy;
            HMachine.ChangeState(target);
        }
        public void EnterThrowHFSM() { HMachine.ChangeState(HRoot.Locomotion.Grounded.Throw); }
        public void EnterBlockHFSM() { /* HFSM reads PendingCommands.block each frame to switch BlockStand/BlockCrouch */ }
        public void EnterCrouchHFSM() { /* HFSM reads PendingCommands.crouch and will enter Crouch/BlockCrouch accordingly */ }
        public void EnterDodgeHFSM() { HMachine.ChangeState(HRoot.Locomotion.Grounded.Dodge); }
        public void EnterHitstunHFSM(float seconds) {
            var loc = HRoot.Locomotion;
            var hs = IsGrounded() ? loc.Grounded.Hitstun : loc.Air.Hitstun;
            hs.Begin(seconds);
            HMachine.ChangeState(hs);
        }
        public void EnterDownedHFSM(bool hard, float duration) {
            var loc = HRoot.Locomotion;
            var dn = loc.Grounded.Downed;
            dn.Begin(hard, duration);
            HMachine.ChangeState(dn);
            TryShowWakeupHint();
        }
        void TryShowWakeupHint() {
            var hint = UnityEngine.Object.FindObjectOfType<UI.ControlsHintBinder>();
            if (hint) { hint.showWakeupTip = true; if (hint.text) { hint.text.text += "\n" + hint.wakeupTip; } }
        }

        private void FixedUpdate() {
            if (IsFrozen()) return;
            if (Mathf.Abs(externalImpulseX) > 0.0001f) {
                var loco = GetComponent<FighterLocomotion>();
                if (loco) loco.NudgeHorizontal(externalImpulseX);
                else rb.velocity = new Vector2(rb.velocity.x + externalImpulseX, rb.velocity.y);
                externalImpulseX = 0f;
            }
        }

        public bool IsFrozen() { return FrameClock.Now < freezeUntilFrame; }
        public void FreezeFrames(int frames) {
            if (frames <= 0) return;
            if (!IsFrozen()) cachedVelocity = rb.velocity;
            freezeUntilFrame = Mathf.Max(freezeUntilFrame, FrameClock.Now + frames);
            rb.velocity = Vector2.zero;
        }
        void ApplyFreezeVisual() {
            if (animator) animator.speed = IsFrozen() ? 0f : 1f;
            if (rb) rb.simulated = !IsFrozen();
        }

        public void Move(float x) {
            var loco = GetComponent<FighterLocomotion>();
            if (loco) loco.Move(x); else rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y);
        }
        public void HaltHorizontal() { var loco = GetComponent<FighterLocomotion>(); if (loco) loco.HaltHorizontal(); else rb.velocity = new Vector2(0, rb.velocity.y); }
        public void AirMove(float x) { var loco = GetComponent<FighterLocomotion>(); if (loco) loco.AirMove(x); else rb.velocity = new Vector2(x * (stats != null ? stats.walkSpeed : 6f), rb.velocity.y); }
        public bool CanJump() { if (!jumpRule) jumpRule = GetComponent<JumpRule>(); return jumpRule ? jumpRule.CanPerformJump(IsGrounded()) : IsGrounded(); }
        public void DoJump() { if (jumpRule) jumpRule.NotifyJumpExecuted(IsGrounded()); var loco = GetComponent<FighterLocomotion>(); if (loco) loco.Jump(); else { rb.velocity = new Vector2(rb.velocity.x, stats != null ? stats.jumpForce : 12f); if (AnimatorReady()) animator.SetTrigger("Jump"); } }

        public void TriggerAttack(string trigger) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.TriggerAttack(trigger); return; }
            debugMoveName = trigger;
            if (moveSet != null) CurrentMove = moveSet.Get(trigger);
            if (CurrentMove != null && CurrentMove.meterCost > 0) { if (!ConsumeMeter(CurrentMove.meterCost)) { CurrentMove = null; return; } }
            if (CurrentMove != null && CurrentMove.healAmount > 0) { AddHealth(CurrentMove.healAmount); }
            if (AnimatorReady()) animator.SetTrigger(trigger);
        }
        public void ClearCurrentMove() { CurrentMove = null; debugMoveName = null; }
        public void SetAttackActive(bool on) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.SetAttackActive(on); return; }
            debugHitActive = on;
            if (on) { attackInstanceId++; hitVictims.Clear(); hitStopApplied = false; }
            if (hitboxes == null) return;
            foreach (var h in hitboxes) if (h != null) h.active = on;
            if (srHasVisual) srVisual.color = on ? Color.yellow : srDefaultColor;
        }

        public void RequestComboCancel(string trigger) { pendingCancelTrigger = trigger; hasPendingCancel = true; }
        public bool TryConsumeComboCancel(out string trigger) { trigger = null; if (!hasPendingCancel) return false; hasPendingCancel = false; trigger = pendingCancelTrigger; return true; }

        public bool AnimatorIsTag(string tag) { return animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag(tag); }
        public void SetCurrentMove(Data.MoveData md) { CurrentMove = md; }
        public void IncrementAttackInstanceId() { attackInstanceId++; }
        public void ClearHitVictimsSet() { hitVictims.Clear(); }
        public void SetActiveColor(bool on) { if (srHasVisual) srVisual.color = on ? Color.yellow : srDefaultColor; }
        public void AddExternalImpulse(float dx) { externalImpulseX += dx; }
        public void RaiseDamaged(FighterController attacker) { OnDamaged?.Invoke(this); OnAnyDamage?.Invoke(this, attacker); }

        public void AddMeter(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) res.AddMeter(value); else meter = Mathf.Clamp(meter + value, 0, maxMeter); }
        public bool ConsumeMeter(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) return res.ConsumeMeter(value); if (meter < value) return false; meter -= value; return true; }
        public void AddHealth(int value) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) res.AddHealth(value); else { int maxHp = stats != null ? stats.maxHealth : 100; currentHealth = Mathf.Clamp(currentHealth + value, 0, maxHp); } }

        public bool CanBlock(DamageInfo info) { return GuardEvaluator.CanBlock(PendingCommands.block, IsGrounded(), IsCrouching, info.level); }

        public void SetUpperBodyInvuln(bool on) { UpperBodyInvuln = on; }
        public void SetLowerBodyInvuln(bool on) { LowerBodyInvuln = on; }
        public void SetUpperLowerInvuln(bool up, bool low) { var res = GetComponent<Fighter.Core.FighterResources>(); if (res) { res.SetUpperBodyInvuln(up); res.SetLowerBodyInvuln(low);} else { UpperBodyInvuln = up; LowerBodyInvuln = low; } }

        void UpdateHurtboxEnable() {
            if (hurtboxes == null) return;
            foreach (var hb in hurtboxes) if (hb != null) {
                bool enabled = true;
                if (hb.region == Combat.HurtRegion.Head || hb.region == Combat.HurtRegion.Torso) enabled &= !UpperBodyInvuln;
                if (hb.region == Combat.HurtRegion.Legs) enabled &= !LowerBodyInvuln;
                bool grounded = IsGrounded();
                if (grounded && !IsCrouching) enabled &= hb.activeStanding;
                else if (grounded && IsCrouching) enabled &= hb.activeCrouching;
                else enabled &= hb.activeAirborne;
                hb.enabledThisFrame = enabled;
            }
        }

        public void TakeHit(DamageInfo info, FighterController attacker) {
            var recv = GetComponent<Fighter.Core.DamageReceiver>();
            if (recv) { recv.TakeHit(info, attacker); return; }
            if ((info.level == HitLevel.High || info.level == HitLevel.Overhead) && UpperBodyInvuln) return;
            if (info.level == HitLevel.Low && LowerBodyInvuln) return;

            var res = HitResolver.Resolve(info, CanBlock(info), stats != null ? stats.blockDamageRatio : 0.2f);
            int maxHp = stats != null ? stats.maxHealth : 100;
            int before = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - res.finalDamage, 0, maxHp);
            var fr = GetComponent<Fighter.Core.FighterResources>(); if (fr != null) fr.OnHealthChanged?.Invoke(currentHealth, maxHp);

            if (currentHealth == 0) { if (AnimatorReady()) animator.SetTrigger("KO"); return; }

            float dir = Mathf.Sign(transform.position.x - attacker.transform.position.x);
            if (!res.wasBlocked) {
                rb.velocity = new Vector2(dir * info.knockback.x, info.knockback.y);
                if (AnimatorReady()) animator.SetTrigger("Hit");
                bool requestKD = info.knockdownKind == Combat.KnockdownKind.Soft || info.knockdownKind == Combat.KnockdownKind.Hard;
                if (requestKD && team == FighterTeam.Player && stats != null && stats.preventKnockdownIfMeter && meter >= stats.preventKnockdownMeterCost) { meter -= stats.preventKnockdownMeterCost; requestKD = false; }
                if (requestKD) { float downDur = info.knockdownKind == Combat.KnockdownKind.Hard ? (stats != null ? stats.hardKnockdownTime : 1.0f) : (stats != null ? stats.softKnockdownTime : 0.6f); EnterDownedHFSM(info.knockdownKind == Combat.KnockdownKind.Hard, downDur); }
                else { EnterHitstunHFSM(res.appliedStun); }

                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.MarkHitConfirmed();
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnHit : 20);
                attacker.externalImpulseX += -dir * res.appliedPushback;

                if (currentHealth < before) { OnDamaged?.Invoke(this); OnAnyDamage?.Invoke(this, attacker); }
            } else {
                if (AnimatorReady()) animator.SetTrigger("BlockHit");
                EnterHitstunHFSM(res.appliedStun);
                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnBlock : 10);
                attacker.externalImpulseX += -dir * res.appliedPushback;
            }
        }

        public event Action<float> OnHitConfirm;
        public void OnHitConfirmedLocal(float seconds) {
            var atk = GetComponent<Fighter.Core.AttackExecutor>();
            if (atk) { atk.OnHitConfirmedLocal(seconds); return; }
            if (hitStopApplied) return; hitStopApplied = true;
            int frames = FrameClock.SecondsToFrames(seconds);
            FreezeFrames(frames);
            Systems.CameraShaker.Instance?.Shake(0.1f, seconds);
            OnHitConfirm?.Invoke(seconds);
        }

        public void MarkHitConfirmed(float duration = 0.35f) { hitConfirmTimer = Mathf.Max(hitConfirmTimer, duration); }
        public bool HasRecentHitConfirm() => hitConfirmTimer > 0f;

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

        void AutoFaceOpponent() {
            if (!opponent) return;
            bool shouldFaceRight = transform.position.x <= opponent.position.x;
            if (shouldFaceRight != facingRight) { facingRight = shouldFaceRight; var s = transform.localScale; s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1); transform.localScale = s; }
        }

        public bool IsGrounded() {
            var loco = GetComponent<FighterLocomotion>();
            if (loco) return loco.IsGrounded(groundMask);
            if (bodyCollider == null) return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
            var b = bodyCollider.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
        }

        public void SetAnimatorBool(string key, bool v) { if (AnimatorReady()) animator.SetBool(key, v); }
        public void SetDebugMoveName(string name) { debugMoveName = name; }
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
                knockdownKind = Combat.KnockdownKind.Soft,
            };
            victim.TakeHit(info, this);
            victim.StartUkemiWindow(0.4f);
        }

        public bool CanHitTarget(FighterController target) {
            if (target == null || target == this) return false;
            if (hitVictims.Contains(target)) return false;
            hitVictims.Add(target);
            return true;
        }

        public bool IsOpponentInThrowRange(float maxDist) {
            if (!opponent) return false;
            if (!IsGrounded()) return false;
            var opp = opponent.GetComponent<FighterController>();
            if (!opp || !opp.IsGrounded()) return false;
            float dx = Mathf.Abs(opponent.position.x - transform.position.x);
            float dy = Mathf.Abs(opponent.position.y - transform.position.y);
            return dx <= maxDist && dy <= 1.0f;
        }
    }
}