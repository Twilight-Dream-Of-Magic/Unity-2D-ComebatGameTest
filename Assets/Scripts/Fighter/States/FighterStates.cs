using UnityEngine;

namespace Fighter.States {
    // Legacy single-layer FSM shim kept only for compile compatibility.
    // The runtime now uses HFSM (see Fighter/HFSM). These classes are no-ops.
    public abstract class FighterStateBase {
        protected FightingGame.Combat.Actors.FighterActor fighter;
        public FighterStateBase(FightingGame.Combat.Actors.FighterActor f) { fighter = f; }
        public abstract string Name { get; }
        public virtual void Enter() {}
        public virtual void Tick() {}
        public virtual void Exit() {}
        protected bool HasMoveInput(out float x) { x = 0f; return false; }
        public void OwnerNotifyStateChanged() { }
    }

    public class IdleState : FighterStateBase { public IdleState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Idle"; }
    public class WalkState : FighterStateBase { public WalkState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Walk"; }
    public class CrouchState : FighterStateBase { public CrouchState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Crouch"; }
    public class JumpAirState : FighterStateBase { public JumpAirState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Jump"; }
    public class BlockState : FighterStateBase { public BlockState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Block"; }
    public class DodgeState : FighterStateBase { public DodgeState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Dodge"; }
    public class AttackState : FighterStateBase { public AttackState(FightingGame.Combat.Actors.FighterActor f, string trig) : base(f) {} public override string Name => "Attack"; }
    public class HitstunState : FighterStateBase { public HitstunState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Hitstun"; public void Begin(float t) {} }
    public class KnockdownState : FighterStateBase { public KnockdownState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "KO"; }
    public class ThrowState : FighterStateBase { public ThrowState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Throw"; }
    public class WakeupState : FighterStateBase { public WakeupState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Wakeup"; }
    public class PreJumpState : FighterStateBase { public PreJumpState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "PreJump"; }
    public class LandingState : FighterStateBase { public LandingState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Landing"; }
    public class DashState : FighterStateBase { public DashState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Dash"; }
    public class BackdashState : FighterStateBase { public BackdashState(FightingGame.Combat.Actors.FighterActor f) : base(f) {} public override string Name => "Backdash"; }
}