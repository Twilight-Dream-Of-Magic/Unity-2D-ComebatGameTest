namespace Fighter.FSM {
    public class StateMachine {
        public IState Current { get; private set; }
        public void SetState(IState next) {
            if (Current == next) return;
            Current?.Exit();
            Current = next;
            Current?.Enter();
            // notify owner if available
            if (Current is Fighter.States.FighterStateBase fsb && fsb != null) fsb.OwnerNotifyStateChanged();
        }
        public void Tick() => Current?.Tick();
    }
}