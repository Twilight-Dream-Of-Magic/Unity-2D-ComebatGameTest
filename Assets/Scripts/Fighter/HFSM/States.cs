using UnityEngine;

namespace Fighter.HFSM {
    public class RootState : HState {
        public LocomotionState Locomotion { get; private set; }
        public RootState(FighterController f) : base(f) {
            Locomotion = new LocomotionState(f, this);
        }
        public override void OnTick() { Locomotion.Machine.Tick(); }
    }

    public class LocomotionState : HState {
        public readonly HStateMachine Machine = new HStateMachine();
        public GroundedState Grounded { get; private set; }
        public AirState Air { get; private set; }
        public LocomotionState(FighterController f, HState parent) : base(f, parent) {
            Grounded = new GroundedState(f, this);
            Air = new AirState(f, this);
        }
        public override void OnEnter() {
            Machine.SetInitial(this, Fighter.IsGrounded() ? (HState)Grounded.Idle : (HState)Air.Jump);
        }
        public override void OnTick() {
            if (Fighter.IsGrounded()) {
                if (Machine.Current == null || Machine.Current.Parent != Grounded) Machine.ChangeState(Grounded.Idle);
            } else {
                if (Machine.Current == null || Machine.Current.Parent != Air) Machine.ChangeState(Air.Jump);
            }
            Machine.Tick();
        }
    }

    public class GroundedState : HState {
        public HStateMachine Machine => (Parent as LocomotionState).Machine;
        public IdleState Idle { get; private set; }
        public WalkState Walk { get; private set; }
        public CrouchState Crouch { get; private set; }
        public BlockState Block { get; private set; }
        public AttackState AttackLight { get; private set; }
        public AttackState AttackHeavy { get; private set; }
        public HitstunState Hitstun { get; private set; }
        public DownedState Downed { get; private set; }
        public DodgeState Dodge { get; private set; }
        public ThrowState Throw { get; private set; }
        public GroundedState(FighterController f, HState parent) : base(f, parent) {
            Idle = new IdleState(f, this);
            Walk = new WalkState(f, this);
            Crouch = new CrouchState(f, this);
            Block = new BlockState(f, this);
            AttackLight = new AttackState(f, this, "Light");
            AttackHeavy = new AttackState(f, this, "Heavy");
            Hitstun = new HitstunState(f, this);
            Downed = new DownedState(f, this);
            Dodge = new DodgeState(f, this);
            Throw = new ThrowState(f, this);
        }
    }

    public class AirState : HState {
        public HStateMachine Machine => (Parent as LocomotionState).Machine;
        public JumpAirState Jump { get; private set; }
        public AttackState AirLight { get; private set; }
        public AttackState AirHeavy { get; private set; }
        public HitstunState Hitstun { get; private set; }
        public AirState(FighterController f, HState parent) : base(f, parent) {
            Jump = new JumpAirState(f, this);
            AirLight = new AttackState(f, this, "Light");
            AirHeavy = new AttackState(f, this, "Heavy");
            Hitstun = new HitstunState(f, this);
        }
    }

    public class IdleState : HState {
        public IdleState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Idle";
        public override void OnTick() {
            var g = Parent as GroundedState;
            var loco = Parent.Parent as LocomotionState;
            var c = Fighter.PendingCommands;
            if (c.block) { g.Machine.ChangeState(g.Block); return; }
            if (c.dodge) { g.Machine.ChangeState(g.Dodge); return; }
            if (c.crouch) { g.Machine.ChangeState(g.Crouch); return; }
            if (c.jump && Fighter.CanJump()) { Fighter.DoJump(); loco.Machine.ChangeState(loco.Air.Jump); return; }
            if (c.light) { g.Machine.ChangeState(g.AttackLight); return; }
            if (c.heavy) { g.Machine.ChangeState(g.AttackHeavy); return; }
            if (Mathf.Abs(c.moveX) > 0.01f) { g.Machine.ChangeState(g.Walk); return; }
            Fighter.HaltHorizontal();
        }
    }

    public class WalkState : HState {
        public WalkState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Walk";
        public override void OnTick() {
            var g = Parent as GroundedState;
            var loco = Parent.Parent as LocomotionState;
            var c = Fighter.PendingCommands;
            if (Mathf.Abs(c.moveX) < 0.01f) { g.Machine.ChangeState(g.Idle); return; }
            if (c.block) { g.Machine.ChangeState(g.Block); return; }
            if (c.crouch) { g.Machine.ChangeState(g.Crouch); return; }
            if (c.jump && Fighter.CanJump()) { Fighter.DoJump(); loco.Machine.ChangeState(loco.Air.Jump); return; }
            if (c.light) { g.Machine.ChangeState(g.AttackLight); return; }
            if (c.heavy) { g.Machine.ChangeState(g.AttackHeavy); return; }
            Fighter.Move(c.moveX);
        }
    }

    public class CrouchState : HState {
        public CrouchState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Crouch";
        public override void OnEnter() { Fighter.IsCrouching = true; Fighter.SetAnimatorBool("Crouch", true); }
        public override void OnExit() { Fighter.IsCrouching = false; Fighter.SetAnimatorBool("Crouch", false); }
        public override void OnTick() {
            var g = Parent as GroundedState;
            var c = Fighter.PendingCommands;
            if (!c.crouch) { g.Machine.ChangeState(g.Idle); return; }
            if (c.light) { g.Machine.ChangeState(g.AttackLight); return; }
            if (c.heavy) { g.Machine.ChangeState(g.AttackHeavy); return; }
        }
    }

    public class BlockState : HState {
        public BlockState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Block";
        public override void OnEnter() { Fighter.SetAnimatorBool("Block", true); }
        public override void OnExit() { Fighter.SetAnimatorBool("Block", false); }
        public override void OnTick() {
            var g = Parent as GroundedState;
            if (!Fighter.PendingCommands.block) g.Machine.ChangeState(g.Idle);
        }
    }

    public class AttackState : HState {
        readonly string trigger;
        float startup, active, recovery;
        float t;
        enum Phase { Startup, Active, Recovery }
        Phase phase;
        public AttackState(FighterController f, HState p, string trig) : base(f, p) { trigger = trig; }
        public override string Name => "Attack-" + trigger;
        public override void OnEnter() {
            var md = Fighter.moveSet ? Fighter.moveSet.Get(trigger) : null;
            startup = md ? md.startup : 0.08f;
            active = md ? md.active : 0.06f;
            recovery = md ? md.recovery : 0.18f;
            t = 0; phase = Phase.Startup; Fighter.TriggerAttack(trigger);
        }
        public override void OnTick() {
            var g = Parent as GroundedState;
            t += Time.deltaTime;
            switch (phase) {
                case Phase.Startup:
                    if (t >= startup) { phase = Phase.Active; t = 0; Fighter.SetAttackActive(true); }
                    break;
                case Phase.Active:
                    if (t >= active) { phase = Phase.Recovery; t = 0; Fighter.SetAttackActive(false); }
                    break;
                case Phase.Recovery:
                    if (t >= recovery) { g.Machine.ChangeState(g.Idle); Fighter.ClearCurrentMove(); }
                    break;
            }
        }
        public override void OnExit() { Fighter.SetAttackActive(false); }
    }

    public class HitstunState : HState {
        float timer;
        public HitstunState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Hitstun";
        public void Begin(float d) { timer = d; }
        public override void OnTick() {
            timer -= Time.deltaTime;
            if (timer <= 0) {
                var g = Parent as GroundedState;
                g.Machine.ChangeState(g.Idle);
            }
        }
    }

    public class DownedState : HState { public DownedState(FighterController f, HState p) : base(f, p) { } public override string Name => "Downed"; }

    public class DodgeState : HState {
        float timer;
        public DodgeState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Dodge";
        public override void OnEnter() { timer = Fighter.Stats.dodgeDuration; Fighter.StartDodge(); }
        public override void OnTick() {
            var g = Parent as GroundedState;
            timer -= Time.deltaTime; if (timer <= 0) g.Machine.ChangeState(g.Idle);
        }
    }

    public class ThrowState : HState {
        float t;
        public ThrowState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Throw";
        public override void OnEnter() { t = 0.15f; Fighter.SetAnimatorBool("Throw", true); }
        public override void OnTick() {
            var g = Parent as GroundedState;
            t -= Time.deltaTime;
            if (t <= 0) {
                var opp = Fighter.opponent ? Fighter.opponent.GetComponent<FighterController>() : null;
                if (opp && Fighter.IsOpponentInThrowRange(1.0f)) {
                    opp.StartThrowTechWindow(0.25f);
                    if (!opp.WasTechTriggeredAndClear()) { Fighter.ApplyThrowOn(opp); }
                }
                Fighter.SetAnimatorBool("Throw", false);
                g.Machine.ChangeState(g.Idle);
            }
        }
    }

    public class JumpAirState : HState {
        public JumpAirState(FighterController f, HState p) : base(f, p) { }
        public override string Name => "Jump";
        public override void OnTick() {
            var loco = Parent.Parent as LocomotionState;
            var air = Parent as AirState;
            if (Fighter.IsGrounded()) { loco.Machine.ChangeState(loco.Grounded.Idle); return; }
            var c = Fighter.PendingCommands;
            if (Mathf.Abs(c.moveX) > 0.01f) Fighter.AirMove(c.moveX);
            if (c.jump && Fighter.CanJump()) { Fighter.DoJump(); return; }
            if (c.light) { loco.Machine.ChangeState(air.AirLight); return; }
            if (c.heavy) { loco.Machine.ChangeState(air.AirHeavy); return; }
        }
    }
}