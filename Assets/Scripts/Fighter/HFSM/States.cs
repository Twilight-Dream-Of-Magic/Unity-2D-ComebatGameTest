using UnityEngine;

namespace FightingGame.Combat.State.HFSM
{
	public class RootState : HState
	{
		// New domain wrappers (step 1): keep Locomotion mapping for back-compat
		public MovementDomainState Movement { get; private set; }
		public OffenseDomainState Offense { get; private set; }
		public DefenseDomainState Defense { get; private set; }
		public LocomotionState Locomotion => Movement?.Locomotion;

		public RootState(FightingGame.Combat.Actors.FighterActor actor) : base(actor)
		{
			Movement = new MovementDomainState(actor, this);
			Offense = new OffenseDomainState(actor, this);
			Defense = new DefenseDomainState(actor, this);
		}

		public override void OnEnter()
		{
			if (Locomotion != null)
			{
				Locomotion.OnEnter();
			}
		}

		public override void OnTick()
		{
			Actors.FighterCommands pendingCommands = Fighter.PendingCommands; // no abbreviations / 不使用缩写

			// Defense domain takes precedence when block is held
			// 防御域优先：按住格挡时优先进入防御
			if (pendingCommands.block)
			{
				// Route via leaf states instead of instantiating flats
				// 通过叶子态路由，避免直接实例化 Flat
				Fighter.HMachine.ChangeState(pendingCommands.crouch ? (HState)Defense.BlockCrouch : (HState)Defense.BlockStand);
				return;
			}

			// Dodge if requested and allowed
			// 当请求回避且允许时进入回避
			if (pendingCommands.dodge && Fighter.CanDodge())
			{
				Fighter.HMachine.ChangeState(Defense.Dodge);
				return;
			}

			// If Offense is currently active, keep ticking it
			// 若进攻域处于激活态，继续推进
			if (Offense.Flat.Current != null)
			{
				Offense.Flat.Tick();
				return;
			}

			// Air offense trigger: airborne + Light/Heavy (delegate via unified API)
			// 空中攻击触发：在空中且按下轻/重攻击（通过统一入口函数路由）
			if (!Fighter.IsGrounded() && (pendingCommands.light || pendingCommands.heavy))
			{
				Fighter.EnterAttackHFSM(pendingCommands.light ? "Light" : "Heavy");
				return;
			}

			// Fallback to Movement domain
			// 回退到移动域
			Movement.Flat.Tick();
		}
	}
}