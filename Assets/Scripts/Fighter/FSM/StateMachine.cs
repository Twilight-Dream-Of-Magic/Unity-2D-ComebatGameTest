namespace Fighter.FSM {
    public class StateMachine {
        public IState Current { get; private set; }
        public void SetState(IState next) {
            if (Current == next) return;
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }
        public void Tick() => Current?.Tick();
    }
}