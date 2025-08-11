using UnityEngine;
using Fighter.FSM;
using Data;

namespace Fighter.States {
    public abstract class FighterStateBase : IState {
        protected FighterController fighter;
        public FighterStateBase(FighterController f) { fighter = f; }
        public abstract string Name { get; }
        public virtual void Enter() {}
        public virtual void Tick() {}
        public virtual void Exit() {}
        protected bool HasMoveInput(out float x) { x = fighter.PendingCommands.moveX; return Mathf.Abs(x) > 0.01f; }
    }

    public class IdleState : FighterStateBase {
        public IdleState(FighterController f) : base(f) {}
        public override string Name => "Idle";
        public override void Enter() { fighter.SetAnimatorBool("Block", false); }
        public override void Tick() {
            var c = fighter.PendingCommands;
            if (fighter.TryConsumeDashRequest(out bool back)) { fighter.StateMachine.SetState(back ? (FighterStateBase)fighter.Backdash : fighter.Dash); return; }
            if (c.block && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Block); return; }
            if (c.dodge && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Dodge); return; }
            if (c.crouch && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Crouch); return; }
            if ((c.jump && fighter.CanJump()) || (fighter.GetComponent<JumpRule>()?.ShouldConsumeBufferedJump(fighter.IsGrounded()) ?? false)) { fighter.DoJump(); fighter.StateMachine.SetState(fighter.PreJump); return; }
            if (c.light) { fighter.StateMachine.SetState(fighter.AttackLight); return; }
            if (c.heavy) { fighter.StateMachine.SetState(fighter.AttackHeavy); return; }
            if (HasMoveInput(out float x)) fighter.Move(x); else fighter.HaltHorizontal();
        }
    }

    public class WalkState : FighterStateBase {
        public WalkState(FighterController f) : base(f) {}
        public override string Name => "Walk";
        public override void Tick() {
            var c = fighter.PendingCommands;
            if (!HasMoveInput(out float x)) { fighter.StateMachine.SetState(fighter.Idle); return; }
            if (fighter.TryConsumeDashRequest(out bool back)) { fighter.StateMachine.SetState(back ? (FighterStateBase)fighter.Backdash : fighter.Dash); return; }
            if (c.block && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Block); return; }
            if (c.dodge && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Dodge); return; }
            if (c.crouch && fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Crouch); return; }
            if ((c.jump && fighter.CanJump()) || (fighter.GetComponent<JumpRule>()?.ShouldConsumeBufferedJump(fighter.IsGrounded()) ?? false)) { fighter.DoJump(); fighter.StateMachine.SetState(fighter.PreJump); return; }
            if (c.light) { fighter.StateMachine.SetState(fighter.AttackLight); return; }
            if (c.heavy) { fighter.StateMachine.SetState(fighter.AttackHeavy); return; }
            fighter.Move(x);
        }
    }

    public class CrouchState : FighterStateBase {
        public CrouchState(FighterController f) : base(f) {}
        public override string Name => "Crouch";
        public override void Enter() { fighter.IsCrouching = true; fighter.SetAnimatorBool("Crouch", true); }
        public override void Tick() {
            var c = fighter.PendingCommands;
            if (!c.crouch) { fighter.StateMachine.SetState(fighter.Idle); return; }
            if (c.light) { fighter.StateMachine.SetState(fighter.AttackLight); return; }
            if (c.heavy) { fighter.StateMachine.SetState(fighter.AttackHeavy); return; }
        }
        public override void Exit() { fighter.IsCrouching = false; fighter.SetAnimatorBool("Crouch", false); }
    }

    public class PreJumpState : FighterStateBase {
        float t; public PreJumpState(FighterController f) : base(f) {}
        public override string Name => "PreJump";
        public override void Enter() { t = 0.05f; }
        public override void Tick() { t -= Time.deltaTime; if (t <= 0) fighter.StateMachine.SetState(fighter.JumpState); }
    }

    public class LandingState : FighterStateBase {
        float t; public LandingState(FighterController f) : base(f) {}
        public override string Name => "Landing";
        public override void Enter() { t = 0.06f; }
        public override void Tick() { t -= Time.deltaTime; if (t <= 0) fighter.StateMachine.SetState(fighter.Idle); }
    }

    public class JumpAirState : FighterStateBase {
        public JumpAirState(FighterController f) : base(f) {}
        public override string Name => "Jump";
        public override void Tick() {
            if (fighter.IsGrounded()) { fighter.StateMachine.SetState(fighter.Landing); return; }
            var c = fighter.PendingCommands;
            if (Mathf.Abs(c.moveX) > 0.01f) fighter.AirMove(c.moveX);
            var rule = fighter.GetComponent<JumpRule>();
            if ((c.jump && fighter.CanJump()) || (rule != null && rule.ShouldConsumeBufferedJump(false))) { fighter.DoJump(); return; }
            if (c.light) { fighter.StateMachine.SetState(fighter.AttackLight); return; }
            if (c.heavy) { fighter.StateMachine.SetState(fighter.AttackHeavy); return; }
        }
    }

    public class BlockState : FighterStateBase {
        public BlockState(FighterController f) : base(f) {}
        public override string Name => "Block";
        public override void Enter() { fighter.SetAnimatorBool("Block", true); }
        public override void Tick() { if (!fighter.PendingCommands.block) fighter.StateMachine.SetState(fighter.Idle); }
        public override void Exit() { fighter.SetAnimatorBool("Block", false); }
    }

    public class DodgeState : FighterStateBase {
        float timer; public DodgeState(FighterController f) : base(f) {}
        public override string Name => "Dodge";
        public override void Enter() { timer = fighter.Stats.dodgeDuration; fighter.StartDodge(); }
        public override void Tick() { timer -= Time.deltaTime; if (timer <= 0) fighter.StateMachine.SetState(fighter.Idle); }
    }

    public class DashState : FighterStateBase {
        float t; float dir; public DashState(FighterController f) : base(f) {}
        public override string Name => "Dash";
        public override void Enter() { t = 0.15f; dir = fighter.facingRight ? 1f : -1f; fighter.SetAnimatorBool("Dash", true); }
        public override void Tick() {
            t -= Time.deltaTime;
            fighter.AirMove(dir * 1.6f);
            if (t <= 0) fighter.StateMachine.SetState(fighter.Idle);
        }
        public override void Exit() { fighter.SetAnimatorBool("Dash", false); }
    }

    public class BackdashState : FighterStateBase {
        float t; float dir; public BackdashState(FighterController f) : base(f) {}
        public override string Name => "Backdash";
        public override void Enter() { t = 0.18f; dir = fighter.facingRight ? -1f : 1f; fighter.SetAnimatorBool("Backdash", true); }
        public override void Tick() {
            t -= Time.deltaTime;
            fighter.AirMove(dir * 1.4f);
            if (t <= 0) fighter.StateMachine.SetState(fighter.Idle);
        }
        public override void Exit() { fighter.SetAnimatorBool("Backdash", false); }
    }

    public class AttackState : FighterStateBase {
        readonly string trigger;
        float startup, active, recovery; float t; enum Phase { Startup, Active, Recovery } Phase phase;
        public AttackState(FighterController f, string trig) : base(f) { trigger = trig; }
        public override string Name => "Attack-" + trigger;
        public override void Enter() {
            MoveData md = fighter.moveSet ? fighter.moveSet.Get(trigger) : null;
            startup = md ? md.startup : 0.08f;
            active = md ? md.active : 0.06f;
            recovery = md ? md.recovery : 0.18f;
            t = 0; phase = Phase.Startup; fighter.TriggerAttack(trigger);
        }
        public override void Tick() {
            t += Time.deltaTime;
            switch (phase) {
                case Phase.Startup:
                    if (t >= startup) { phase = Phase.Active; t = 0; fighter.SetAttackActive(true);} break;
                case Phase.Active:
                    if (fighter.TryConsumeComboCancel(out string to)) { fighter.TriggerAttack(to); phase = Phase.Startup; t = 0; }
                    else if (t >= active) { phase = Phase.Recovery; t = 0; fighter.SetAttackActive(false);} break;
                case Phase.Recovery:
                    if (fighter.TryConsumeComboCancel(out string to2)) { fighter.TriggerAttack(to2); phase = Phase.Startup; t = 0; }
                    else if (t >= recovery) { fighter.ClearCurrentMove(); fighter.StateMachine.SetState(fighter.Idle);} break;
            }
        }
        public override void Exit() { fighter.SetAttackActive(false); fighter.ClearCurrentMove(); }
    }

    public class HitstunState : FighterStateBase {
        float timer; public HitstunState(FighterController f) : base(f){}
        public override string Name => "Hitstun";
        public void Begin(float t) { timer = t; }
        public override void Enter() {}
        public override void Tick() { timer -= Time.deltaTime; if (timer <= 0) fighter.StateMachine.SetState(fighter.Idle); }
    }

    public class KnockdownState : FighterStateBase {
        public KnockdownState(FighterController f) : base(f){}
        public override string Name => "KO";
    }

    public class ThrowState : FighterStateBase {
        float t; public ThrowState(FighterController f) : base(f) {}
        public override string Name => "Throw";
        public override void Enter() { t = 0.2f; }
        public override void Tick() { t -= Time.deltaTime; if (t <= 0) fighter.StateMachine.SetState(fighter.Idle); }
    }

    public class WakeupState : FighterStateBase {
        float t; public WakeupState(FighterController f) : base(f) {}
        public override string Name => "Wakeup";
        public override void Enter() { t = 0.3f; }
        public override void Tick() { t -= Time.deltaTime; if (t <= 0) fighter.StateMachine.SetState(fighter.Idle); }
    }
}