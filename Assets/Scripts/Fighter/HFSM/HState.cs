namespace FightingGame.Combat.State.HFSM
{
	/// <summary>
	/// Base class for hierarchical states. Provides references to Fighter and Parent, and virtual hooks.
	/// 分层状态基类：持有 Fighter 与父状态引用，并提供进入/退出/逐帧钩子。
	/// </summary>
	public abstract class HState
	{
		/// <summary>Owning fighter. 所属角色。</summary>
		public readonly FightingGame.Combat.Actors.FighterActor Fighter;
		/// <summary>Parent state in the hierarchy. 层级父状态。</summary>
		public readonly HState Parent;
		/// <summary>Depth from Root (Root=0). 自 Root 起的深度（Root=0）。</summary>
		public readonly int Depth;
		/// <summary>Human-readable state name. 可读状态名。</summary>
		public virtual string Name => GetType().Name;

		protected HState(FightingGame.Combat.Actors.FighterActor fighter, HState parent = null)
		{
			Fighter = fighter;
			Parent = parent;
			Depth = parent == null ? 0 : parent.Depth + 1;
		}

		/// <summary>Called once when entering the state. 进入状态时调用。</summary>
		public virtual void OnEnter() { }
		/// <summary>Called once when exiting the state. 退出状态时调用。</summary>
		public virtual void OnExit() { }
		/// <summary>Called every frame while active. 活跃期间每帧调用。</summary>
		public virtual void OnTick() { }
	}

	/// <summary>
	/// Minimal hierarchical state machine with Root scoping; ensures exit-all-then-enter-all order
	/// when changing states.
	/// 分层状态机：限定 Root 作用域；状态切换遵循“先退后进”。
	/// </summary>
	public sealed class HStateMachine
	{
		/// <summary>Root state for clamping traversal. 根状态（限定遍历范围）。</summary>
		public HState Root
		{
			get;
			private set;
		}
		/// <summary>Current active state. 当前活跃状态。</summary>
		public HState Current
		{
			get;
			private set;
		}
		/// <summary>Raised on state name changes (for UI). 状态变化事件（名称，用于 UI）。</summary>
		public System.Action<string> OnStateChanged;

		// Reusable buffers to avoid per-transition allocations
		readonly System.Collections.Generic.List<HState> exitList = new System.Collections.Generic.List<HState>(8);
		readonly System.Collections.Generic.Stack<HState> enterStack = new System.Collections.Generic.Stack<HState>(8);

		// Non-reentrant transition scheduling
		bool isTransitioning;
		readonly System.Collections.Generic.Queue<HState> pending = new System.Collections.Generic.Queue<HState>(4);

		/// <summary>Sets Root and initial state. 设定根与初始状态。</summary>
		public void SetInitial(HState root, HState start)
		{
			Root = root;
			Request(start);
			ProcessPending();
		}

		/// <summary>Request a transition (scheduled). 请求切换（排队）。</summary>
		public void Request(HState target)
		{
			if (target != null) pending.Enqueue(target);
		}

		/// <summary>Back-compat immediate API; schedule then process. 兼容旧 API（排队后立刻处理）。</summary>
		public void ChangeState(HState target)
		{
			Request(target);
			ProcessPending();
		}

		/// <summary>Ticks current state and processes any pending transitions. 驱动当前状态并处理排队转换。</summary>
		public void Tick()
		{
			ProcessPending();
			Current?.OnTick();
		}

		void ProcessPending()
		{
			if (isTransitioning) return;
			int guard = 8; // avoid infinite loops
			while (pending.Count > 0 && guard-- > 0)
			{
				var target = pending.Dequeue();
				if (target == null || target == Current) continue;
				isTransitioning = true;
#if UNITY_EDITOR
				UnityEngine.Debug.Log($"[HFSM] ChangeState {Current?.Name ?? "-"} -> {target.Name}");
#endif
				ComputeAndApplyTransition(Current, target);
				isTransitioning = false;
#if UNITY_EDITOR
				UnityEngine.Debug.Log($"[HFSM] Now Current = {Current?.Name ?? "-"}");
#endif
				OnStateChanged?.Invoke(Current?.Name ?? "-");
				// Immediately tick the new current once to allow entry-driven effects to settle
				Current?.OnTick();
			}
		}

		void ComputeAndApplyTransition(HState from, HState to)
		{
			// Simple strategy: exit to Root completely, then enter down to target
			exitList.Clear();
			var cur = from;
			while (cur != null && cur != Root)
			{
				exitList.Add(cur);
				cur = ParentClamped(cur);
			}
			for (int i = 0; i < exitList.Count; i++) exitList[i].OnExit();
			enterStack.Clear();
			cur = to;
			while (cur != null && cur != Root)
			{
				enterStack.Push(cur);
				cur = ParentClamped(cur);
			}
			while (enterStack.Count > 0) enterStack.Pop().OnEnter();
			Current = to;
		}

		HState ParentClamped(HState s)
		{
			return (s == null || s == Root) ? null : s.Parent;
		}
	}
}