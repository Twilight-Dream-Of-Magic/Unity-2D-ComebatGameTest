namespace Fighter.HFSM {
    public abstract class HState {
        public readonly FighterController Fighter;
        public readonly HState Parent;
        public virtual string Name => GetType().Name;

        protected HState(FighterController fighter, HState parent = null) {
            Fighter = fighter;
            Parent = parent;
        }

        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnTick() {}
    }

    public sealed class HStateMachine {
        public HState Root { get; private set; }
        public HState Current { get; private set; }
        public System.Action<string> OnStateChanged;

        public void SetInitial(HState root, HState start) {
            Root = root;
            ChangeState(start);
        }

        public void ChangeState(HState target) {
            if (target == null || target == Current) return;
            var a = Current;
            var b = target;
            // climb stacks to find LCA
            var pa = a; var pb = b;
            int da = Depth(pa), db = Depth(pb);
            while (da > db) { pa.OnExit(); pa = pa.Parent; da--; }
            while (db > da) { pb = pb.Parent; db--; }
            while (pa != pb) { pa?.OnExit(); pb?.OnExit(); pa = pa?.Parent; pb = pb?.Parent; }
            // enter path from LCA to target
            var stack = new System.Collections.Generic.Stack<HState>();
            var cur = target;
            while (cur != pa) { stack.Push(cur); cur = cur.Parent; }
            while (stack.Count > 0) stack.Pop().OnEnter();
            Current = target;
            OnStateChanged?.Invoke(Current?.Name ?? "-");
        }

        static int Depth(HState s) { int d = 0; while (s != null) { d++; s = s.Parent; } return d; }

        public void Tick() { Current?.OnTick(); }
    }
}