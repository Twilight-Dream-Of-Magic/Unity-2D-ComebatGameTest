namespace Fighter.FSM {
    public interface IState {
        void Enter();
        void Tick();
        void Exit();
        string Name { get; }
    }
}