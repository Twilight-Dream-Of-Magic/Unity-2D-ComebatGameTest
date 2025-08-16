using UnityEngine;

namespace FightingGame.Combat.State
{
	public abstract class FSMState
	{
		public readonly FightingGame.Combat.Actors.FighterActor Fighter;
		protected FSMState(FightingGame.Combat.Actors.FighterActor f)
		{
			Fighter = f;
		}
		public virtual string Name => GetType().Name;
		public virtual void OnEnter() { }
		public virtual void OnExit() { }
		public virtual void Tick() { }
	}

	public sealed class FSMachine
	{
		public FSMState Current
		{
			get;
			private set;
		}
		public System.Action<string> OnStateChanged;
		public void SetInitial(FSMState s)
		{
			Current = s;
			Current?.OnEnter();
			OnStateChanged?.Invoke(Current?.Name ?? "-");
		}
		public void ChangeState(FSMState s)
		{
			if (s == null || s == Current) return;
			Current?.OnExit();
			Current = s;
			Current?.OnEnter();
			OnStateChanged?.Invoke(Current?.Name ?? "-");
		}
		public void Tick()
		{
			Current?.Tick();
		}
	}
}