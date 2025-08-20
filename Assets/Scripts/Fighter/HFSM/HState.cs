using UnityEngine;

namespace FightingGame.Combat.State.HFSM
{
	/// <summary>
	/// Base class for hierarchical states. Provides references to the owning fighter and parent state,
	/// and offers virtual hooks for entering, exiting, and ticking.
	/// 分層狀態基類：持有角色與父狀態引用，並提供進入 / 退出 / 逐幀鉤子。
	/// </summary>
	public abstract class HState
	{
		/// <summary>
		/// Owning fighter.
		/// 所屬角色。
		/// </summary>
		public readonly FightingGame.Combat.Actors.FighterActor Fighter;

		/// <summary>
		/// Parent state in the hierarchy.
		/// 層級中的父狀態。
		/// </summary>
		public readonly HState Parent;

		/// <summary>
		/// Depth counted from the root (root = 0).
		/// 自根節點起的深度（根 = 0）。
		/// </summary>
		public readonly int Depth;

		/// <summary>
		/// Human-readable state name.
		/// 可讀的狀態名稱。
		/// </summary>
		public virtual string Name
		{
			get
			{
				return GetType().Name;
			}
		}

		protected HState(FightingGame.Combat.Actors.FighterActor fighter, HState parent = null)
		{
			Fighter = fighter;
			Parent = parent;
			Depth = parent == null ? 0 : parent.Depth + 1;
		}

		/// <summary>
		/// Called once when entering the state.
		/// 進入狀態時調用一次。
		/// </summary>
		public virtual void OnEnter()
		{
		}

		/// <summary>
		/// Called once when exiting the state.
		/// 退出狀態時調用一次。
		/// </summary>
		public virtual void OnExit()
		{
		}

		/// <summary>
		/// Called every frame while the state is active.
		/// 狀態活躍期間每幀調用。
		/// </summary>
		public virtual void OnTick()
		{
		}
	}

	/// <summary>
	/// Minimal hierarchical state machine with a root scope. When changing states, it guarantees
	/// the order of "exit all from current to root" followed by "enter all from root to target".
	/// 分層狀態機（限定根作用域）：保證狀態切換遵循「自當前向上全部退出」再「自根向下全部進入」的順序。
	/// </summary>
	public sealed class HStateMachine
	{
		/// <summary>
		/// Root state that clamps traversal.
		/// 根狀態（限定遍歷範圍）。
		/// </summary>
		public HState Root
		{
			get;
			private set;
		}

		/// <summary>
		/// Current active state.
		/// 當前活躍狀態。
		/// </summary>
		public HState Current
		{
			get;
			private set;
		}

		/// <summary>
		/// Raised when the current state's human-readable name changes (for UI binding).
		/// 狀態名變化事件（供 UI 綁定）。
		/// </summary>
		public System.Action<string> OnStateChanged;

		// Reusable buffers to avoid per-transition allocations
		// 可重用緩衝，避免每次切換都分配
		private readonly System.Collections.Generic.List<HState> exitStateList = new System.Collections.Generic.List<HState>(8);
		private readonly System.Collections.Generic.Stack<HState> enterStateStack = new System.Collections.Generic.Stack<HState>(8);

		// Non-reentrant transition scheduling
		// 非可重入的切換排程
		private bool isTransitionInProgress;
		private readonly System.Collections.Generic.Queue<HState> pendingRequestQueue = new System.Collections.Generic.Queue<HState>(4);

		/// <summary>
		/// Sets the root and the initial state.
		/// 設定根與初始狀態。
		/// </summary>
		public void SetInitial(HState root, HState start)
		{
			Root = root;
			Request(start);
			ProcessPendingRequests();
		}

		/// <summary>
		/// Request a transition (it will be scheduled and processed safely).
		/// 請求狀態切換（排隊並安全處理）。
		/// </summary>
		public void Request(HState target)
		{
			if (target != null)
			{
				pendingRequestQueue.Enqueue(target);
			}
		}

		/// <summary>
		/// Immediate compatibility wrapper: schedule then process.
		/// 與舊介面相容：先排隊，隨後立刻處理。
		/// </summary>
		public void ChangeState(HState target)
		{
			Request(target);
			ProcessPendingRequests();
		}

		/// <summary>
		/// Drive the current state and process any pending transitions.
		/// 駕駛當前狀態並處理所有排隊中的請求。
		/// </summary>
		public void Tick()
		{
			ProcessPendingRequests();

			if (Current != null)
			{
				Current.OnTick();
			}
		}

		private void ProcessPendingRequests()
		{
			if (isTransitionInProgress)
			{
				return;
			}

			int safetyCounter = 8; // avoid infinite loops / 避免死循環

			while (pendingRequestQueue.Count > 0 && safetyCounter > 0)
			{
				safetyCounter--;

				HState target = pendingRequestQueue.Dequeue();

				if (target == null)
				{
					continue;
				}

				if (target == Current)
				{
					continue;
				}

				isTransitionInProgress = true;

#if UNITY_EDITOR
				string previousName = Current != null ? Current.Name : "-";
				string targetName = target != null ? target.Name : "-";
				Debug.Log("[HierarchicalStateMachine] Change state from " + previousName + " to " + targetName);
#endif

				ComputeAndApplyTransition(Current, target);

				isTransitionInProgress = false;

#if UNITY_EDITOR
				string nowName = Current != null ? Current.Name : "-";
				Debug.Log("[HierarchicalStateMachine] Current state is now " + nowName);
#endif
				if (OnStateChanged != null)
				{
					OnStateChanged.Invoke(Current != null ? Current.Name : "-");
				}

				// Immediately tick the new current once to allow entry-driven effects to settle
				// 立即對新狀態調用一次 OnTick，讓入場效果先行穩定
				if (Current != null)
				{
					Current.OnTick();
				}
			}
		}

		private void ComputeAndApplyTransition(HState from, HState to)
		{
			// Strategy: exit upward to the root completely, then enter downward to the target
			// 策略：先自當前一路退出到根，再自根一路進入到目標
			exitStateList.Clear();

			HState current = from;
			while (current != null && current != Root)
			{
				exitStateList.Add(current);
				current = GetParentClamped(current);
			}

			for (int index = 0; index < exitStateList.Count; index++)
			{
				exitStateList[index].OnExit();
			}

			enterStateStack.Clear();
			current = to;
			while (current != null && current != Root)
			{
				enterStateStack.Push(current);
				current = GetParentClamped(current);
			}

			while (enterStateStack.Count > 0)
			{
				HState next = enterStateStack.Pop();
				next.OnEnter();
			}

			Current = to;
		}

		private HState GetParentClamped(HState state)
		{
			if (state == null)
			{
				return null;
			}

			if (state == Root)
			{
				return null;
			}

			return state.Parent;
		}
	}
}
