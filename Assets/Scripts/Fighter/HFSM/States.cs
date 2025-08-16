using UnityEngine;

namespace FightingGame.Combat.State.HFSM
{
	public class RootState : HState
	{
		// New domain wrappers (step 1): keep Locomotion mapping for back-compat
		public MovementDomainState Movement
		{
			get;
			private set;
		}
		public OffenseDomainState Offense
		{
			get;
			private set;
		}
		public DefenseDomainState Defense
		{
			get;
			private set;
		}
		// Back-compat: expose Locomotion as before
		public LocomotionState Locomotion => Movement?.Locomotion;
		public RootState(FightingGame.Combat.Actors.FighterActor fa) : base(fa)
		{
			Movement = new MovementDomainState(fa, this);
			Offense = new OffenseDomainState(fa, this);
			Defense = new DefenseDomainState(fa, this);
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
			// Strict arbitration: Defense > Offense > Movement
			var c = Fighter.PendingCommands;
			// Defense domain takes precedence when held or active
			if (c.block)
			{
				Defense.Flat.ChangeState(new DefenseDomainState.BlockFlat(Fighter, c.crouch));
				Defense.Flat.Tick();
				return;
			}
			if (c.dodge)
			{
				Defense.Flat.ChangeState(new DefenseDomainState.DodgeFlat(Fighter));
				Defense.Flat.Tick();
				return;
			}
			// If Offense is active, keep ticking it
			if (Offense.Flat.Current != null)
			{
				Offense.Flat.Tick();
				return;
			}
			// Air offense trigger: airborne + J/K
			if (!Fighter.IsGrounded() && (c.light || c.heavy))
			{
				Offense.BeginAirAttackFlat(c.light ? "Light" : "Heavy");
				Offense.Flat.Tick();
				return;
			}
			// Fallback to Movement domain
			Movement.Flat.Tick();
		}
	}

	// Domain: Movement (wraps existing Locomotion + its internal machine)
	public class MovementDomainState : HState
	{
		public LocomotionState Locomotion
		{
			get;
			private set;
		}
		// Hybrid: embed a flat FSM for movement+attacks
		public FightingGame.Combat.State.FSMachine Flat
		{
			get;
			private set;
		}
		public class IdleFlat : FightingGame.Combat.State.FSMState
		{
			public IdleFlat(FightingGame.Combat.Actors.FighterActor f) : base(f) { }
			public override string Name => "Idle";
			public override void Tick()
			{
				var c = Fighter.PendingCommands;
				if (Mathf.Abs(c.moveX) > 0.01f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new WalkFlat(Fighter));
				}
				if (c.crouch)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new CrouchFlat(Fighter));
				}
				if (c.light)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
				}
				if (c.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
				}
			}
		}
		public class WalkFlat : FightingGame.Combat.State.FSMState
		{
			public WalkFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Walk";
			public override void Tick()
			{
				var c = Fighter.PendingCommands;
				if (Mathf.Abs(c.moveX) < 0.01f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
				}
				else
				{
					Fighter.Move(c.moveX);
				}
				if (c.light)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
				}
				if (c.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
				}
			}
		}
		public class CrouchFlat : FightingGame.Combat.State.FSMState
		{
			public CrouchFlat(FightingGame.Combat.Actors.FighterActor f) : base(f) { }
			public override string Name => "Crouch";
			public override void OnEnter()
			{
				Fighter.IsCrouching = true;
				Fighter.SetAnimatorBool("Crouch", true);
			}
			public override void OnExit()
			{
				Fighter.IsCrouching = false;
				Fighter.SetAnimatorBool("Crouch", false);
			}
			public override void Tick()
			{
				var c = Fighter.PendingCommands;
				if (!c.crouch)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
				}
				if (c.light)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackLightFlat(Fighter));
				}
				if (c.heavy)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new AttackHeavyFlat(Fighter));
				}
			}
		}
		public class AttackLightFlat : FightingGame.Combat.State.FSMState
		{
			float t, s, a, r;
			public AttackLightFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Attack-Light";
			public override void OnEnter()
			{
				var md = Fighter.actionSet != null ? Fighter.actionSet.Get("Light") : null;
				s = md != null ? md.startup : 0.05f;
				a = md != null ? md.active : 0.04f;
				r = md != null ? md.recovery : 0.12f;
				t = 0;
				Fighter.TriggerAttack("Light");
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t < s)
				{
					return;
				}
				if (t < s + a)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (t < s + a + r)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
			}
		}
		public class AttackHeavyFlat : FightingGame.Combat.State.FSMState
		{
			float t, s, a, r;
			public AttackHeavyFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Attack-Heavy";
			public override void OnEnter()
			{
				var md = Fighter.actionSet != null ? Fighter.actionSet.Get("Heavy") : null;
				s = md != null ? md.startup : 0.12f;
				a = md != null ? md.active : 0.05f;
				r = md != null ? md.recovery : 0.22f;
				t = 0;
				Fighter.TriggerAttack("Heavy");
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t < s)
				{
					return;
				}
				if (t < s + a)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (t < s + a + r)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HRoot.Movement.Flat.ChangeState(new IdleFlat(Fighter));
			}
		}
		public MovementDomainState(FightingGame.Combat.Actors.FighterActor fa, HState parent) : base(fa, parent)
		{
			Locomotion = new LocomotionState(fa, this);
			Flat = new FightingGame.Combat.State.FSMachine();
			Flat.SetInitial(new IdleFlat(fa));
		}
		public override string Name => "Movement";
		public override void OnEnter()
		{
			if (Locomotion != null)
			{
				Locomotion.OnEnter();
			}
		}
		public override void OnTick()
		{
			Flat.Tick();
		}
	}

	// Domain: Offense (placeholder for future migration)
	public class OffenseDomainState : HState
	{
		public AttackState GroundLight
		{
			get;
			private set;
		}
		public AttackState GroundHeavy
		{
			get;
			private set;
		}
		public AttackState AirLight
		{
			get;
			private set;
		}
		public AttackState AirHeavy
		{
			get;
			private set;
		}
		public ThrowState Throw
		{
			get;
			private set;
		}
		// Embedded flat FSM: handles offense sequencing independently
		public FightingGame.Combat.State.FSMachine Flat
		{
			get;
			private set;
		}
		public class AttackFlat : FightingGame.Combat.State.FSMState
		{
			readonly string trig;
			float t, s, a, r;
			public AttackFlat(FightingGame.Combat.Actors.FighterActor fa, string trig) : base(fa)
			{
				this.trig = trig;
			}
			public override string Name => "Offense-" + trig;
			public override void OnEnter()
			{
				var md = Fighter.actionSet != null ? Fighter.actionSet.Get(trig) : null;
				s = md != null ? md.startup : 0.1f; a = md != null ? md.active : 0.05f; r = md != null ? md.recovery : 0.16f;
				t = 0;
				Fighter.TriggerAttack(trig);
				#if UNITY_EDITOR
				Debug.Log($"[Offense] Enter Attack {trig}");
				#endif
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t < s) return;
				if (t < s + a) { Fighter.SetAttackActive(true); return; }
				if (t < s + a + r) { Fighter.SetAttackActive(false); return; }
				Fighter.ClearCurrentMove();
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}
		public class SuperFlat : FightingGame.Combat.State.FSMState
		{
			float t, s, a, r;
			public SuperFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Offense-Super";
			public override void OnEnter()
			{
				var md = Fighter.actionSet != null ? Fighter.actionSet.Get("Super") : null;
				s = md != null ? md.startup : 0.12f; a = md != null ? md.active : 0.08f; r = md != null ? md.recovery : 0.28f;
				t = 0f;
				Fighter.TriggerAttack("Super");
				#if UNITY_EDITOR
				Debug.Log("[Offense] Enter Super");
				#endif
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t < s) return;
				if (t < s + a) { Fighter.SetAttackActive(true); return; }
				if (t < s + a + r) { Fighter.SetAttackActive(false); return; }
				Fighter.ClearCurrentMove();
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}
		public class AirAttackFlat : FightingGame.Combat.State.FSMState
		{
			readonly string trig;
			float t, s, a, r;
			public AirAttackFlat(FightingGame.Combat.Actors.FighterActor fa, string trig) : base(fa)
			{
				this.trig = trig;
			}
			public override string Name => "Offense-Air-" + trig;
			public override void OnEnter()
			{
				var md = Fighter.actionSet != null ? Fighter.actionSet.Get(trig) : null;
				s = md != null ? md.startup : 0.06f;
				a = md != null ? md.active : 0.05f;
				r = md != null ? md.recovery : 0.16f;
				t = 0;
				Fighter.TriggerAttack(trig);
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				var md = Fighter.CurrentMove;
				bool tryCancel = Fighter.TryConsumeComboCancel(out string to);
				bool contact = Fighter.HasRecentHitConfirm();
				bool allow = false;
				if (md != null)
				{
					if (!contact && md.canCancelOnWhiff && t >= md.onWhiffCancelWindow.x && t <= md.onWhiffCancelWindow.y)
					{
						allow = true;
					}
					if (contact)
					{
						if (md.canCancelOnHit && t >= md.onHitCancelWindow.x && t <= md.onHitCancelWindow.y)
						{
							allow = true;
						}
						if (md.canCancelOnBlock && t >= md.onBlockCancelWindow.x && t <= md.onBlockCancelWindow.y)
						{
							allow = true;
						}
					}
					if (allow && md.cancelIntoTriggers != null && md.cancelIntoTriggers.Length > 0)
					{
						bool listed = false;
						for (int i = 0; i < md.cancelIntoTriggers.Length; i++)
						{
							if (md.cancelIntoTriggers[i] == to)
							{
								listed = true;
								break;
							}
						}
						allow = listed;
					}
				}
				if (tryCancel && allow && !string.IsNullOrEmpty(to))
				{
					Fighter.HRoot.Offense.BeginAttackFlat(to);
					return;
				}
				if (t < s)
				{
					return;
				}
				if (t < s + a)
				{
					Fighter.SetAttackActive(true);
					return;
				}
				if (t < s + a + r)
				{
					Fighter.SetAttackActive(false);
					return;
				}
				Fighter.ClearCurrentMove();
				Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
			}
		}
		public class ThrowFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public ThrowFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Offense-Throw";
			public override void OnEnter()
			{
				t = 0.15f;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				t -= Time.deltaTime;
				if (t <= 0)
				{
					var opp = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
					if (opp && Fighter.IsOpponentInThrowRange(1.0f))
					{
						opp.StartThrowTechWindow(0.25f);
						if (!opp.WasTechTriggeredAndClear())
						{
							Fighter.ApplyThrowOn(opp);
						}
					}
					Fighter.SetAnimatorBool("Throw", false);
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class AirThrowFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public AirThrowFlat(FightingGame.Combat.Actors.FighterActor f) : base(f) { }
			public override string Name => "Offense-AirThrow";
			public override void OnEnter()
			{
				t = 0.12f;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				t -= Time.deltaTime;
				if (t <= 0)
				{
					var opp = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
					if (opp && !opp.IsGrounded() && Vector2.Distance(opp.transform.position, Fighter.transform.position) < 1.1f)
					{
						opp.StartThrowTechWindow(0.2f);
						if (!opp.WasTechTriggeredAndClear())
						{
							Fighter.ApplyThrowOn(opp);
						}
					}
					Fighter.SetAnimatorBool("Throw", false);
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class GuardBreakThrowFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public GuardBreakThrowFlat(FightingGame.Combat.Actors.FighterActor fa) : base(fa) { }
			public override string Name => "Offense-GuardBreakThrow";
			public override void OnEnter()
			{
				t = 0.15f;
				Fighter.SetAnimatorBool("Throw", true);
			}
			public override void Tick()
			{
				t -= Time.deltaTime;
				if (t <= 0)
				{
					var opp = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
					bool oppBlocking = opp && opp.PendingCommands.block;
					if (opp && oppBlocking && Fighter.IsOpponentInThrowRange(1.0f))
					{
						opp.StartThrowTechWindow(0.25f);
						if (!opp.WasTechTriggeredAndClear()) Fighter.ApplyThrowOn(opp);
					}
					Fighter.SetAnimatorBool("Throw", false);
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class HealFlat : FightingGame.Combat.State.FSMState
		{
			readonly string trig;
			float t, r;
			public HealFlat(FightingGame.Combat.Actors.FighterActor fa, string trig) : base(fa) { this.trig = trig; }
			public override string Name => "Heal";
			public override void OnEnter()
			{
				Fighter.ExecuteHeal(trig);
				r = 0.22f; t = 0f;
				#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Heal");
				#endif
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t >= r)
				{
					Fighter.ClearCurrentMove();
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public OffenseDomainState(FightingGame.Combat.Actors.FighterActor f, HState parent) : base(f, parent)
		{
			var root = parent as RootState;
			var locomotion = root != null ? root.Locomotion : null;
			GroundLight = new AttackState(f, this, "Light", locomotion);
			GroundHeavy = new AttackState(f, this, "Heavy", locomotion);
			AirLight = new AttackState(f, this, "Light", locomotion);
			AirHeavy = new AttackState(f, this, "Heavy", locomotion);
			Throw = new ThrowState(f, this, locomotion);
			Flat = new FightingGame.Combat.State.FSMachine();
		}
		public override string Name => "Offense";
		public override void OnTick()
		{
			Flat.Tick();
		}
		public void BeginAttackFlat(string trig)
		{
			Flat.ChangeState(new AttackFlat(Fighter, trig));
		}
		public void BeginAirAttackFlat(string trig)
		{
			Flat.ChangeState(new AirAttackFlat(Fighter, trig));
		}
		public void BeginThrowFlat()
		{
			Flat.ChangeState(new ThrowFlat(Fighter));
		}
		public void BeginHealFlat(string trig)
		{
			Flat.ChangeState(new HealFlat(Fighter, trig));
		}
		public void BeginSuperFlat()
		{
			Flat.ChangeState(new SuperFlat(Fighter));
		}
		public void BeginAirThrowFlat()
		{
			Flat.ChangeState(new AirThrowFlat(Fighter));
		}
		public void BeginGuardBreakThrowFlat()
		{
			Flat.ChangeState(new GuardBreakThrowFlat(Fighter));
		}
	}

	// Domain: Defense (placeholder for future migration)
	public class DefenseDomainState : HState
	{
		public BlockStandState BlockStand
		{
			get;
			private set;
		}
		public BlockCrouchState BlockCrouch
		{
			get;
			private set;
		}
		public DodgeState Dodge
		{
			get;
			private set;
		}
		public HitstunState Hitstun
		{
			get;
			private set;
		}
		public DownedState Downed
		{
			get;
			private set;
		}
		public WakeupState Wakeup
		{
			get;
			private set;
		}
		// Embedded flat FSM for defense chain
		public FightingGame.Combat.State.FSMachine Flat
		{
			get;
			private set;
		}
		public class BlockFlat : FightingGame.Combat.State.FSMState
		{
			readonly bool crouch;
			public BlockFlat(FightingGame.Combat.Actors.FighterActor f, bool c) : base(f)
			{
				crouch = c;
			}
			public override string Name => crouch ? "Block(Crouch)" : "Block";
			public override void OnEnter()
			{
				Fighter.SetAnimatorBool("Block", true);
				if (crouch)
				{
					Fighter.IsCrouching = true;
					Fighter.SetAnimatorBool("Crouch", true);
				}
			}
			public override void OnExit()
			{
				Fighter.SetAnimatorBool("Block", false);
			}
			public override void Tick()
			{
				var c = Fighter.PendingCommands;
				if (!c.block)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class DodgeFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public DodgeFlat(FightingGame.Combat.Actors.FighterActor f) : base(f) { }
			public override string Name => "Dodge";
			public override void OnEnter()
			{
				Fighter.StartDodge(); t = Fighter.Stats.dodgeInvuln;
				#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Dodge (i-frames active)");
				#endif
			}
			public override void Tick()
			{
				t -= Time.deltaTime; if (t <= 0) { Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter)); }
			}
		}
		public class HitstunFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public HitstunFlat(FightingGame.Combat.Actors.FighterActor f, float d) : base(f)
			{
				t = d;
			}
			public override string Name => "Hitstun";
			public override void Tick()
			{
				t -= Time.deltaTime;
				if (t <= 0)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class HealFlat : FightingGame.Combat.State.FSMState
		{
			readonly string trig;
			float t, r;
			public HealFlat(FightingGame.Combat.Actors.FighterActor f, string trig) : base(f) { this.trig = trig; }
			public override string Name => "Heal";
			public override void OnEnter()
			{
				Fighter.ExecuteHeal(trig);
				r = 0.22f; t = 0f;
				#if UNITY_EDITOR
				Debug.Log("[Defense] Enter Heal");
				#endif
			}
			public override void Tick()
			{
				t += Time.deltaTime;
				if (t >= r)
				{
					Fighter.ClearCurrentMove();
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public class DownedFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			readonly bool hard;
			public DownedFlat(FightingGame.Combat.Actors.FighterActor f, bool hard, float d) : base(f)
			{
				this.hard = hard;
				t = d;
			}
			public override string Name => hard ? "Downed(Hard)" : "Downed(Soft)";
			public override void OnEnter()
			{
				Fighter.SetAnimatorBool("Downed", true);
			}
			public override void OnExit()
			{
				Fighter.SetAnimatorBool("Downed", false);
			}
			public override void Tick()
			{
				t -= Time.deltaTime;
				if (t <= 0)
				{
					Fighter.HRoot.Defense.Flat.ChangeState(new WakeupFlat(Fighter));
				}
			}
		}
		public class WakeupFlat : FightingGame.Combat.State.FSMState
		{
			float t;
			public WakeupFlat(FightingGame.Combat.Actors.FighterActor f) : base(f) { }
			public override string Name => "Wakeup";
			public override void OnEnter()
			{
				t = Fighter.stats ? Fighter.stats.wakeupInvuln : 0.25f;
				Fighter.SetUpperLowerInvuln(true, true);
				if (Fighter.animator && Fighter.animator.runtimeAnimatorController)
				{
					Fighter.animator.SetTrigger("Wakeup");
				}
			}
			public override void OnExit()
			{
				Fighter.SetUpperLowerInvuln(false, false);
			}
			public override void Tick()
			{
				t -= Time.deltaTime;
				var c = Fighter.PendingCommands;
				float half = (Fighter.stats != null ? Fighter.stats.wakeupInvuln : 0.25f) *0.5f;
				if (t > 0 && t > (Fighter.stats != null ? Fighter.stats.wakeupInvuln : 0.25f) -half)
				{
					float dir = 0f;
					if (c.moveX > 0.4f)
					{
						dir = Fighter.facingRight ? 1f : -1f; // forward roll
					}
					else if (c.moveX < -0.4f)
					{
						dir = Fighter.facingRight ? -1f : 1f; // backrise
					}
					if (Mathf.Abs(dir) > 0.1f)
					{
						Fighter.AddExternalImpulse(dir * 0.12f);
					}
				}
				if (t <= 0f)
				{
					Fighter.HRoot.Movement.Flat.ChangeState(new MovementDomainState.IdleFlat(Fighter));
				}
			}
		}
		public DefenseDomainState(FightingGame.Combat.Actors.FighterActor f, HState parent) : base(f, parent)
		{
			// Reuse existing implementations by instantiating them with this as parent
			BlockStand = new BlockStandState(f, this);
			BlockCrouch = new BlockCrouchState(f, this);
			Dodge = new DodgeState(f, this);
			var root = parent as RootState;
			var locomotion = root != null ? root.Locomotion : null;
			Hitstun = new HitstunState(f, this, locomotion);
			Downed = new DownedState(f, this, locomotion);
			Wakeup = new WakeupState(f, this, locomotion);
			Flat = new FightingGame.Combat.State.FSMachine();
		}
		public override string Name => "Defense";
		public override void OnTick()
		{
			Flat.Tick();
		}
		public void BeginHitstunFlat(float seconds)
		{
			Flat.ChangeState(new HitstunFlat(Fighter, seconds));
		}
		public void BeginDownedFlat(bool hard, float duration)
		{
			Flat.ChangeState(new DownedFlat(Fighter, hard, duration));
		}
		public void BeginWakeupFlat()
		{
			Flat.ChangeState(new WakeupFlat(Fighter));
		}
		public void BeginHealFlatDefense(string trig) { Flat.ChangeState(new HealFlat(Fighter, trig)); }
	}

	public class LocomotionState : HState
	{
		public readonly HStateMachine Machine = new HStateMachine();
		public GroundedState Grounded
		{
			get;
			private set;
		}
		public AirState Air
		{
			get;
			private set;
		}
		public LocomotionState(FightingGame.Combat.Actors.FighterActor fa, HState parent) : base(fa, parent)
		{
			Grounded = new GroundedState(fa, this);
			Air = new AirState(fa, this);
		}
		public override void OnEnter()
		{
			Machine.SetInitial(this, Fighter.IsGrounded() ? (HState)Grounded.Idle : (HState)Air.Jump);
		}
		public override void OnTick()
		{
			if (Fighter.IsGrounded())
			{
				if (Machine.Current == null || Machine.Current.Parent != Grounded)
				{
					Machine.ChangeState(Grounded.Idle);
				}
			}
			else
			{
				if (Machine.Current == null || Machine.Current.Parent != Air)
				{
					Machine.ChangeState(Air.Jump);
				}
			}
			Machine.Tick();
		}
	}

	public class GroundedState : HState
	{
		public HStateMachine Machine => (Parent as LocomotionState).Machine;
		public IdleState Idle
		{
			get;
			private set;
		}
		public WalkState Walk
		{
			get;
			private set;
		}
		public CrouchState Crouch
		{
			get;
			private set;
		}
		public BlockStandState BlockStand
		{
			get;
			private set;
		}
		public BlockCrouchState BlockCrouch
		{
			get;
			private set;
		}
		public AttackState AttackLight
		{
			get;
			private set;
		}
		public AttackState AttackHeavy
		{
			get;
			private set;
		}
		public HitstunState Hitstun
		{
			get;
			private set;
		}
		public DownedState Downed
		{
			get;
			private set;
		}
		public WakeupState Wakeup
		{
			get;
			private set;
		}
		public DodgeState Dodge
		{
			get;
			private set;
		}
		public ThrowState Throw
		{
			get;
			private set;
		}
		public GroundedState(FightingGame.Combat.Actors.FighterActor f, HState parent) : base(f, parent)
		{
			Idle = new IdleState(f, this);
			Walk = new WalkState(f, this);
			Crouch = new CrouchState(f, this);
			BlockStand = new BlockStandState(f, this);
			BlockCrouch = new BlockCrouchState(f, this);
			var locomotion = Parent as LocomotionState;
			AttackLight = new AttackState(f, this, "Light", locomotion);
			AttackHeavy = new AttackState(f, this, "Heavy", locomotion);
			Hitstun = new HitstunState(f, this, locomotion);
			Downed = new DownedState(f, this, locomotion);
			Wakeup = new WakeupState(f, this, locomotion);
			Dodge = new DodgeState(f, this);
			Throw = new ThrowState(f, this, locomotion);
		}
	}

	public class AirState : HState
	{
		public HStateMachine Machine => (Parent as LocomotionState).Machine;
		public JumpAirState Jump
		{
			get;
			private set;
		}
		public AttackState AirLight
		{
			get;
			private set;
		}
		public AttackState AirHeavy
		{
			get;
			private set;
		}
		public HitstunState Hitstun
		{
			get;
			private set;
		}
		public AirState(FightingGame.Combat.Actors.FighterActor f, HState parent) : base(f, parent)
		{
			Jump = new JumpAirState(f, this);
			var locomotion = Parent as LocomotionState;
			AirLight = new AttackState(f, this, "Light", locomotion);
			AirHeavy = new AttackState(f, this, "Heavy", locomotion);
			Hitstun = new HitstunState(f, this, locomotion);
		}
	}

	public class IdleState : HState
	{
		public IdleState(FightingGame.Combat.Actors.FighterActor f, HState p) : base(f, p) { }
		public override string Name => "Idle";
		public override void OnTick()
		{
			var g = Parent as GroundedState;
			var loco = Parent.Parent as LocomotionState;
			var c = Fighter.PendingCommands;
			if (c.block)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(c.crouch ? (HState)def.BlockCrouch : (HState)def.BlockStand);
					return;
				}
				g.Machine.ChangeState(c.crouch ? (HState)g.BlockCrouch : (HState)g.BlockStand);
				return;
			}
			if (c.dodge)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(def.Dodge);
					return;
				}
				g.Machine.ChangeState(g.Dodge);
				return;
			}
			if (c.crouch)
			{
				g.Machine.ChangeState(g.Crouch);
				return;
			}
			if (c.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				loco.Machine.ChangeState(loco.Air.Jump);
				return;
			}
			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (c.light)
			{
				g.Machine.ChangeState(g.AttackLight);
				return;
			}
			if (c.heavy)
			{
				g.Machine.ChangeState(g.AttackHeavy);
				return;
			}
			if (Mathf.Abs(c.moveX) > 0.01f) {
				g.Machine.ChangeState(g.Walk);
				return;
			}
			Fighter.HaltHorizontal();
		}
	}

	public class WalkState : HState
	{
		public WalkState(FightingGame.Combat.Actors.FighterActor fa, HState p) : base(fa, p) { }
		public override string Name => "Walk";
		public override void OnTick()
		{
			var g = Parent as GroundedState;
			var loco = Parent.Parent as LocomotionState;
			var c = Fighter.PendingCommands;
			if (Mathf.Abs(c.moveX) < 0.01f) {
				g.Machine.ChangeState(g.Idle);
				return;
			}
			if (c.block)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(c.crouch ? (HState)def.BlockCrouch : (HState)def.BlockStand);
					return;
				}
				g.Machine.ChangeState(c.crouch ? (HState)g.BlockCrouch : (HState)g.BlockStand);
				return;
			}
			if (c.crouch)
			{
				g.Machine.ChangeState(g.Crouch);
				return;
			}
			if (c.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				loco.Machine.ChangeState(loco.Air.Jump);
				return;
			}
			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (c.light)
			{
				g.Machine.ChangeState(g.AttackLight);
				return;
			}
			if (c.heavy)
			{
				g.Machine.ChangeState(g.AttackHeavy);
				return;
			}
			Fighter.Move(c.moveX);
		}
	}

	public class CrouchState : HState
	{
		public CrouchState(FightingGame.Combat.Actors.FighterActor fa, HState p) : base(fa, p) { }
		public override string Name => "Crouch";
		public override void OnEnter()
		{
			Fighter.IsCrouching = true;
			Fighter.SetAnimatorBool("Crouch", true);
		}
		public override void OnExit()
		{
			Fighter.IsCrouching = false;
			Fighter.SetAnimatorBool("Crouch", false);
		}
		public override void OnTick()
		{
			var g = Parent as GroundedState;
			var c = Fighter.PendingCommands;
			if (!c.crouch)
			{
				g.Machine.ChangeState(g.Idle);
				return;
			}
			if (c.block)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(def.BlockCrouch);
					return;
				}
				g.Machine.ChangeState(g.BlockCrouch);
				return;
			}
			// Offense routed via EnterAttackHFSM; keep fallback for compatibility
			if (c.light)
			{
				g.Machine.ChangeState(g.AttackLight);
				return;
			}
			if (c.heavy)
			{
				g.Machine.ChangeState(g.AttackHeavy);
				return;
			}
		}
	}

	public class BlockStandState : HState
	{
		public BlockStandState(FightingGame.Combat.Actors.FighterActor f, HState p) : base(f, p) { }
		public override string Name => "Block";
		public override void OnEnter()
		{
			Fighter.SetAnimatorBool("Block", true);
			Fighter.IsCrouching = false;
			Fighter.SetAnimatorBool("Crouch", false);
		}
		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Block", false);
		}
		public override void OnTick()
		{
			var c = Fighter.PendingCommands;
			if (!c.block)
			{
				var mov = Fighter.HRoot?.Movement?.Locomotion;
				if (mov != null)
				{
					Fighter.HMachine.ChangeState(mov.Grounded.Idle);
					return;
				}
			}
			if (c.crouch)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(def.BlockCrouch);
					return;
				}
			}
		}
	}

	public class BlockCrouchState : HState
	{
		public BlockCrouchState(FightingGame.Combat.Actors.FighterActor fa, HState p) : base(fa, p) { }
		public override string Name => "Block(Crouch)";
		public override void OnEnter()
		{
			Fighter.IsCrouching = true;
			Fighter.SetAnimatorBool("Crouch", true);
			Fighter.SetAnimatorBool("Block", true);
		}
		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Block", false);
		}
		public override void OnTick()
		{
			var c = Fighter.PendingCommands;
			if (!c.block)
			{
				var mov = Fighter.HRoot?.Movement?.Locomotion;
				if (mov != null)
				{
					Fighter.HMachine.ChangeState(c.crouch ? (HState)mov.Grounded.Crouch : (HState)mov.Grounded.Idle);
					return;
				}
			}
			if (!c.crouch)
			{
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(def.BlockStand);
					return;
				}
			}
		}
	}

	public class AttackState : HState
	{
		readonly string trigger;
		readonly LocomotionState locomotion;
		float startup, active, recovery;
		float elapsed;
		enum Phase
		{
			Startup,
			Active,
			Recovery
		}
		Phase phase;
		public AttackState(FightingGame.Combat.Actors.FighterActor fa, HState p, string trig, LocomotionState locomotionRef) : base(fa, p)
		{
			trigger = trig;
			locomotion = locomotionRef;
		}
		public override string Name => "Attack-" + trigger;
		public override void OnEnter()
		{
			var md = Fighter.actionSet != null ? Fighter.actionSet.Get(trigger) : null;
			startup = md != null ? md.startup : 0.08f;
			active = md != null ? md.active : 0.06f;
			recovery = md != null ? md.recovery : 0.18f;
			elapsed = 0;
			phase = Phase.Startup;
			Fighter.TriggerAttack(trigger);
		}
		public override void OnTick()
		{
			elapsed += Time.deltaTime;
			var md = Fighter.CurrentMove;
			bool tryCancel = Fighter.TryConsumeComboCancel(out string to);
			bool contact = Fighter.HasRecentHitConfirm();
			bool allowCancel = false;
			if (md != null)
			{
				if (!contact && md.canCancelOnWhiff && elapsed >= md.onWhiffCancelWindow.x && elapsed <= md.onWhiffCancelWindow.y) allowCancel = true;
				if (contact)
				{
					if (md.canCancelOnHit && elapsed >= md.onHitCancelWindow.x && elapsed <= md.onHitCancelWindow.y) allowCancel = true;
					if (md.canCancelOnBlock && elapsed >= md.onBlockCancelWindow.x && elapsed <= md.onBlockCancelWindow.y) allowCancel = true;
				}
			}
			switch (phase)
			{
				case Phase.Startup:
					if (elapsed >= startup)
					{
						phase = Phase.Active;
						elapsed = 0;
						Fighter.SetAttackActive(true);
					}
					break;
				case Phase.Active:
					if (tryCancel && allowCancel && !string.IsNullOrEmpty(to))
					{
						Fighter.TriggerAttack(to);
						phase = Phase.Startup;
						elapsed = 0;
						break;
					}
					if (elapsed >= active)
					{
						phase = Phase.Recovery;
						elapsed = 0;
						Fighter.SetAttackActive(false);
					}
					break;
				case Phase.Recovery:
					if (tryCancel && allowCancel && !string.IsNullOrEmpty(to))
					{
						Fighter.TriggerAttack(to);
						phase = Phase.Startup;
						elapsed = 0;
						break;
					}
					if (elapsed >= recovery)
					{
						if (locomotion != null)
						{
							// Return to neutral based on grounded state (switch Root machine to locomotion neutral)
							Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
						}
						Fighter.ClearCurrentMove();
					}
					break;
			}
		}
		public override void OnExit()
		{
			Fighter.SetAttackActive(false);
		}
	}

	public class HitstunState : HState
	{
		readonly LocomotionState locomotion;
		float timer;
		public HitstunState(FightingGame.Combat.Actors.FighterActor fa, HState p, LocomotionState locomotionRef = null) : base(fa, p)
		{
			locomotion = locomotionRef;
		}
		public override string Name => "Hitstun";
		public void Begin(float d)
		{
			timer = d;
		}
		public override void OnTick()
		{
			timer -= Time.deltaTime;
			if (timer <= 0)
			{
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	public class DownedState : HState
	{
		readonly LocomotionState locomotion;
		float timer;
		bool hard;
		public DownedState(FightingGame.Combat.Actors.FighterActor fa, HState p, LocomotionState locomotionRef = null) : base(fa, p)
		{
			locomotion = locomotionRef;
		}
		public override string Name => hard ? "Downed(Hard)" : "Downed(Soft)";
		public void Begin(bool isHard, float duration)
		{
			hard = isHard;
			timer = duration;
		}
		public override void OnEnter()
		{
			Fighter.SetAnimatorBool("Downed", true);
		}
		public override void OnExit()
		{
			Fighter.SetAnimatorBool("Downed", false);
		}
		public override void OnTick()
		{
			timer -= Time.deltaTime;
			if (timer <= 0f) {
				// Prefer Defense domain Wakeup if available
				var def = Fighter.HRoot?.Defense;
				if (def != null)
				{
					Fighter.HMachine.ChangeState(def.Wakeup);
					return;
				}
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	public class WakeupState : HState
	{
		readonly LocomotionState locomotion;
		float timer;
		public WakeupState(FightingGame.Combat.Actors.FighterActor fa, HState p, LocomotionState locomotionRef = null) : base(fa, p)
		{
			locomotion = locomotionRef;
		}
		public override string Name => "Wakeup";
		public override void OnEnter()
		{
			timer = Fighter.stats != null ? Fighter.stats.wakeupInvuln : 0.25f;
			Fighter.SetUpperLowerInvuln(true, true);
			if (Fighter.animator && Fighter.animator.runtimeAnimatorController)
			{
				Fighter.animator.SetTrigger("Wakeup");
			}
		}
		public override void OnExit()
		{
			Fighter.SetUpperLowerInvuln(false, false);
		}
		public override void OnTick()
		{
			timer -= Time.deltaTime;
			// allow quick direction adjustment during first half
			var c = Fighter.PendingCommands;
			float half = (Fighter.stats != null ? Fighter.stats.wakeupInvuln : 0.25f) *0.5f;
			if (timer > 0 && timer > (Fighter.stats != null ? Fighter.stats.wakeupInvuln : 0.25f) -half)
			{
				float dir = 0f;
				if (c.moveX > 0.4f)
				{
					dir = Fighter.facingRight ? 1f : -1f; // forward roll
				}
				else if (c.moveX < -0.4f)
				{
					dir = Fighter.facingRight ? -1f : 1f; // backrise
				}
				if (Mathf.Abs(dir) > 0.1f)
				{
					Fighter.AddExternalImpulse(dir * 0.12f);
				}
			}
			if (timer <= 0f)
			{
				Fighter.HMachine.ChangeState(Fighter.HRoot.Locomotion);
			}
		}
	}

	public class DodgeState : HState
	{
		float timer;
		public DodgeState(FightingGame.Combat.Actors.FighterActor fa, HState p) : base(fa, p) { }
		public override string Name => "Dodge";
		public override void OnEnter()
		{
			timer = Fighter.Stats.dodgeDuration;
			Fighter.StartDodge();
		}
		public override void OnTick()
		{
			timer -= Time.deltaTime;
			if (timer <= 0)
			{
				var mov = Fighter.HRoot?.Movement?.Locomotion;
				if (mov != null)
				{
					Fighter.HMachine.ChangeState(mov.Grounded.Idle);
				}
			}
		}
	}

	public class ThrowState : HState
	{
		readonly LocomotionState locomotion;
		float elapsed;
		public ThrowState(FightingGame.Combat.Actors.FighterActor fa, HState p, LocomotionState locomotionRef = null) : base(fa, p)
		{
			locomotion = locomotionRef;
		}
		public override string Name => "Throw";
		public override void OnEnter()
		{
			elapsed = 0.15f;
			Fighter.SetAnimatorBool("Throw", true);
		}
		public override void OnTick()
		{
			elapsed -= Time.deltaTime;
			if (elapsed <= 0)
			{
				var opp = Fighter.opponent ? Fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (opp && Fighter.IsOpponentInThrowRange(1.0f))
				{
					opp.StartThrowTechWindow(0.25f);
					if (!opp.WasTechTriggeredAndClear())
					{
						Fighter.ApplyThrowOn(opp);
					}
				}
				Fighter.SetAnimatorBool("Throw", false);
				if (locomotion != null)
				{
					// Return via locomotion sub-machine
					locomotion.Machine.ChangeState(locomotion.Grounded.Idle);
				}
				else
				{
					// Fallback: route via root machine to Movement.Idle
					var mov = Fighter.HRoot?.Movement?.Locomotion;
					if (mov != null)
					{
						Fighter.HMachine.ChangeState(mov.Grounded.Idle);
					}
				}
			}
		}
	}

	public class JumpAirState : HState
	{
		public JumpAirState(FightingGame.Combat.Actors.FighterActor fa, HState p) : base(fa, p) { }
		public override string Name => "Jump";
		public override void OnTick()
		{
			var loco = Parent.Parent as LocomotionState;
			var air = Parent as AirState;
			if (Fighter.IsGrounded())
			{
				loco.Machine.ChangeState(loco.Grounded.Idle);
				return;
			}
			var c = Fighter.PendingCommands;
			if (Mathf.Abs(c.moveX) > 0.01f) Fighter.AirMove(c.moveX);
			if (c.jump && Fighter.CanJump())
			{
				Fighter.DoJump();
				return;
			}
			if (c.light)
			{
				loco.Machine.ChangeState(air.AirLight);
				return;
			}
			if (c.heavy)
			{
				loco.Machine.ChangeState(air.AirHeavy);
				return;
			}
		}
	}
}