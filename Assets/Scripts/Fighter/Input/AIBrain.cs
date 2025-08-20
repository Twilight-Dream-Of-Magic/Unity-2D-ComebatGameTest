using UnityEngine;
using FightingGame.Combat;

namespace Fighter.InputSystem
{
	/// <summary>
	/// AIBrain with difficulty-based strategy FSMs. Each difficulty maps to a dedicated strategy
	/// that continuously produces commands and action tokens to avoid idling.
	/// 具難度分級的 AI 腦：每個難度對應一套策略狀態機，持續穩定輸出行為，杜絕發呆。
	/// </summary>
	public enum AIDifficulty
	{
		Easy,
		Normal,
		Hard
	}

	[DefaultExecutionOrder(30)]
	public class AIBrain : MonoBehaviour
	{
		[Header("Wiring")] public FightingGame.Combat.Actors.FighterActor fighter;
		[Header("Configs")] public AI.AIConfig easy;
		public AI.AIConfig normal;
		public AI.AIConfig hard;
		[Header("Tuning")] public Data.InputTuningConfig inputTuning;
		[Header("Difficulty")] public AIDifficulty difficulty = AIDifficulty.Normal;

		CommandQueue commandQueue;
		IAIStrategy currentStrategy;
		AI.AIConfig activeConfig;

		void Awake()
		{
			if (!fighter)
			{
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
			commandQueue = GetComponent<CommandQueue>();
			if (!commandQueue)
			{
				commandQueue = gameObject.AddComponent<CommandQueue>();
			}
			if (inputTuning)
			{
				commandQueue.tuning = inputTuning;
			}
			// Register action handlers (queue-driven)
			commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Light, OnLight, priority: 10);
			commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Heavy, OnHeavy, priority: 10);
			commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Throw, OnThrow, priority: 10);
			// Also handle on Combo channel so specials fall back to basic attacks when not matched
			commandQueue.RegisterHandler(CommandChannel.Combo, CommandToken.Light, OnLight, priority: 5);
			commandQueue.RegisterHandler(CommandChannel.Combo, CommandToken.Heavy, OnHeavy, priority: 5);

			// Choose active config with fallbacks
			activeConfig = PickConfigByDifficulty();
			// Choose strategy
			switch (difficulty)
			{
				case AIDifficulty.Easy:
					currentStrategy = new EasyStrategy();
					break;
				case AIDifficulty.Hard:
					currentStrategy = new HardStrategy();
					break;
				default:
					currentStrategy = new NormalStrategy();
					break;
			}
			currentStrategy.Initialize(fighter, commandQueue, activeConfig);
			// Subscribe opponent state changes for reactive play
			if (fighter && fighter.opponent)
			{
				var opp = fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>();
				if (opp)
				{
					opp.OnStateChanged += OnOpponentStateChanged;
					opponentActor = opp;
				}
			}
		}

		void OnDestroy()
		{
			if (commandQueue)
			{
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Light, OnLight);
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Heavy, OnHeavy);
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Throw, OnThrow);
				commandQueue.UnregisterHandler(CommandChannel.Combo, CommandToken.Light, OnLight);
				commandQueue.UnregisterHandler(CommandChannel.Combo, CommandToken.Heavy, OnHeavy);
			}
			if (opponentActor != null)
			{
				opponentActor.OnStateChanged -= OnOpponentStateChanged;
			}
		}

		void Update()
		{
			if (!fighter || !fighter.opponent)
			{
				return;
			}
			var commands = new FightingGame.Combat.Actors.FighterCommands();
			currentStrategy.Tick(ref commands);
			// Apply external nudge (pursue-and-engage)
			if (nudgeActive)
			{
				ApplyPursueAndAttack(ref commands);
			}
			fighter.SetCommands(commands);
			EvaluateReactivePlays();
		}

		// External nudge APIs for RoundManager to avoid AI idling too long
		public void ForceAttackLight()
		{
			if (!fighter)
			{
				return;
			}
			var state = fighter.GetCurrentStateName();
			if (!state.StartsWith("Attack"))
			{
				fighter.EnterAttackHFSM("Light");
			}
		}
		public void ForceAttackHeavy()
		{
			if (!fighter)
			{
				return;
			}
			var state = fighter.GetCurrentStateName();
			if (!state.StartsWith("Attack"))
			{
				fighter.EnterAttackHFSM("Heavy");
			}
		}

		// Nudge: pursue target then attack when near (edge distance by hurtbox)
		bool nudgeActive; Transform nudgeTarget; float nudgeUntil; float nudgeNear;
		public void NudgePursueAndAttack(FightingGame.Combat.Actors.FighterActor target, float durationSeconds = 2.0f, float nearOverride = -1f)
		{
			if (target == null)
			{
				return;
			}
			nudgeActive = true; nudgeTarget = target.transform; nudgeUntil = Time.time + Mathf.Max(0.2f, durationSeconds);
			nudgeNear = (nearOverride > 0f) ? nearOverride : 0.15f;
		}
		void ApplyPursueAndAttack(ref FightingGame.Combat.Actors.FighterCommands commands)
		{
			if (!nudgeActive || nudgeTarget == null)
			{
				nudgeActive = false; return;
			}
			if (Time.time > nudgeUntil)
			{
				nudgeActive = false; return;
			}
			var targetActor = nudgeTarget.GetComponent<FightingGame.Combat.Actors.FighterActor>();
			float distEdge = ComputeHurtboxEdgeDistanceX(fighter, targetActor);
			float dx = nudgeTarget.position.x - fighter.transform.position.x;
			if (distEdge > nudgeNear)
			{
				commands.moveX = Mathf.Sign(dx);
				// 可選短跳接近
				if (Mathf.Abs(dx) > 1.5f && fighter.CanJump() && Random.value < 0.1f) commands.jump = true;
			}
			else
			{
				if (Random.value < 0.5f) ForceAttackLight(); else ForceAttackHeavy();
				nudgeActive = false;
			}
		}
		float ComputeHurtboxEdgeDistanceX(FightingGame.Combat.Actors.FighterActor a, FightingGame.Combat.Actors.FighterActor b)
		{
			if (a == null || b == null)
			{
				return Mathf.Abs((b ? b.transform.position.x : 0f) - (a ? a.transform.position.x : 0f));
			}
			var ah = a.hurtboxes; var bh = b.hurtboxes;
			if (ah == null || bh == null || ah.Length == 0 || bh.Length == 0)
			{
				return Mathf.Abs(b.transform.position.x - a.transform.position.x);
			}
			float min = float.MaxValue;
			for (int i = 0; i < ah.Length; i++)
			{
				var ca = ah[i] != null ? ah[i].GetComponent<BoxCollider2D>() : null;
				if (ca == null) continue;
				var ba = ca.bounds;
				for (int j = 0; j < bh.Length; j++)
				{
					var cb = bh[j] != null ? bh[j].GetComponent<BoxCollider2D>() : null;
					if (cb == null) continue;
					var bb = cb.bounds;
					float dx = 0f;
					if (ba.max.x < bb.min.x) dx = bb.min.x - ba.max.x; // a 在左，距離為右盒左邊-左盒右邊
					else if (bb.max.x < ba.min.x) dx = ba.min.x - bb.max.x; // b 在左
					else dx = 0f; // 水平方向已重疊
					if (dx < min) min = dx;
				}
			}
			if (min == float.MaxValue) return Mathf.Abs(b.transform.position.x - a.transform.position.x);
			return min;
		}

		AI.AIConfig PickConfigByDifficulty()
		{
			var cfg = difficulty switch
			{
				AIDifficulty.Easy => easy != null ? easy : (normal != null ? normal : hard),
				AIDifficulty.Hard => hard != null ? hard : (normal != null ? normal : easy),
				_ => normal != null ? normal : (easy != null ? easy : hard)
			};
			if (cfg == null)
			{
				// Create a minimal runtime config if nothing provided
				cfg = ScriptableObject.CreateInstance<AI.AIConfig>();
				cfg.blockProbability = 0.25f;
				cfg.attackCooldownRange = new Vector2(0.5f, 0.9f);
				cfg.approachDistance = 2.0f;
				cfg.retreatDistance = 0.9f;
			}
			return cfg;
		}

		// Action handlers: if attacking, request cancel; otherwise perform
		bool OnLight(float time)
		{
			var s = fighter.GetCurrentStateName();
			if (s.StartsWith("Attack"))
			{
				fighter.RequestComboCancel("Light");
			}
			else
			{
				var root = fighter.HRoot;
				if (root != null && root.Locomotion != null)
				{
					fighter.HMachine.ChangeState(root.Locomotion.Grounded.AttackLight);
				}
				else
				{
					fighter.EnterAttackHFSM("Light");
				}
			}
			return true;
		}
		bool OnHeavy(float time)
		{
			var s = fighter.GetCurrentStateName();
			if (s.StartsWith("Attack"))
			{
				fighter.RequestComboCancel("Heavy");
			}
			else
			{
				var root = fighter.HRoot;
				if (root != null && root.Locomotion != null)
				{
					fighter.HMachine.ChangeState(root.Locomotion.Grounded.AttackHeavy);
				}
				else
				{
					fighter.EnterAttackHFSM("Heavy");
				}
			}
			return true;
		}
		bool OnThrow(float time)
		{
			if (fighter.opponent && Vector2.Distance(fighter.transform.position, fighter.opponent.position) < 1.2f)
			{
				var off = fighter.HRoot?.Offense;
				var opp = fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>();
				if (!fighter.IsGrounded())
				{
					if (off != null)
					{
						off.BeginAirThrowFlat();
						return true;
					}
				}
				bool oppBlocking = opp && opp.PendingCommands.block;
				if (oppBlocking && fighter.IsOpponentInThrowRange(1.0f))
				{
					if (off != null)
					{
						off.BeginGuardBreakThrowFlat();
						return true;
					}
				}
				if (off != null)
				{
					off.BeginThrowFlat();
					return true;
				}
				fighter.EnterThrowHFSM();
				fighter.ApplyThrowOn(opp);
				return true;
			}
			return false;
		}

		interface IAIStrategy
		{
			void Initialize(FightingGame.Combat.Actors.FighterActor fighter, CommandQueue queue, AI.AIConfig cfg);
			void Tick(ref FightingGame.Combat.Actors.FighterCommands commands);
		}

		abstract class BaseStrategy : IAIStrategy
		{
			protected FightingGame.Combat.Actors.FighterActor fighter;
			protected CommandQueue queue;
			protected AI.AIConfig cfg;
			protected float nextAttackAt;
			protected float stateUntil;
			protected float lastActionAt;
			protected float reevaluateInterval = 0.2f;
            protected float idleThreshold = 1.0f;

            protected enum State
			{
				Approach,
				Attack,
				Poke,
				EvadeBlock,
				EvadeDodge,
				Retreat,
				WhiffPunish,
				Okizeme,
				Pressure,
				FrameTrap,
				ThrowMixup,
				AntiAir
			}
			protected State state = State.Approach;

			public virtual void Initialize(FightingGame.Combat.Actors.FighterActor fighter, CommandQueue queue, AI.AIConfig cfg)
			{
				this.fighter = fighter;
				this.queue = queue;
				this.cfg = cfg;
				this.nextAttackAt = Time.time + Random.Range(cfg.attackCooldownRange.x, cfg.attackCooldownRange.y);
				this.stateUntil = Time.time + 0.1f;
				this.lastActionAt = Time.time;
			}

			public abstract void Tick(ref FightingGame.Combat.Actors.FighterCommands commands);

			protected virtual void TickApproach(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				float move = Mathf.Sign(dx);
				if (dist > cfg.approachDistance) commands.moveX = move;
				else commands.moveX = 0f;
				// opportunistic jump to approach
				if (dist > cfg.approachDistance * 1.2f && fighter.CanJump() && Random.value < 0.15f) commands.jump = true;
				MarkActionIf(commands);
				if (dist <= cfg.approachDistance * 0.95f) state = State.Poke;
				MaybeEnterDefense(ref commands);
			}

			protected virtual void TickPoke(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				// probe with light; if in range to attack cadence
				if (CanAttack())
				{
					EnqueueLight();
				}
				commands.moveX = 0f;
				MarkActionIf(commands);
				MaybeEnterDefense(ref commands);
				if (dist < cfg.retreatDistance * 0.8f) state = State.Retreat;
				if (dist < 1.2f) state = State.Attack;
			}

			protected virtual void TickAttack(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (CanAttack())
				{
					if (dist < 1.0f && Random.value < 0.2f) EnqueueThrow();

					else EnqueueLightOrHeavy(0.6f);
				}
				commands.moveX = dist > cfg.approachDistance * 0.9f? Mathf.Sign(dx) : 0f;
				MarkActionIf(commands);
				MaybeEnterDefense(ref commands);
				if (dist > cfg.approachDistance * 1.2f) state = State.Approach;
			}

			protected virtual void TickEvadeBlock(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				commands.block = true;
				commands.moveX = 0f;
				MarkActionIf(commands);
				if (Time.time >= stateUntil) state = State.Poke;
			}

			protected virtual void TickEvadeDodge(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				commands.dodge = true;
				commands.moveX = 0f;
				MarkActionIf(commands);
				if (Time.time >= stateUntil) state = State.Poke;
			}

			protected virtual void TickRetreat(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				commands.moveX = -Mathf.Sign(dx);
				MarkActionIf(commands);
				if (Time.time >= stateUntil || dist > cfg.retreatDistance * 1.4f) state = State.Poke;
			}

			protected virtual void TickWhiffPunish(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (dist < cfg.approachDistance * 1.3f && CanAttack()) EnqueueHeavy();
				commands.moveX = 0f;
				MarkActionIf(commands);
				state = State.Poke;
			}

			protected virtual void TickOkizeme(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				// simple okizeme: stick and choose light/throw
				commands.moveX = dist > 0.8f? Mathf.Sign(dx) : 0f;
				if (CanAttack())
				{
					if (dist < 0.9f && Random.value < 0.35f) EnqueueThrow();

					else EnqueueLight();
				}
				MarkActionIf(commands);
				state = State.Pressure;
			}

			protected virtual void TickPressure(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (CanAttack()) EnqueueLightOrHeavy(0.5f);
				commands.moveX = dist > 0.9f? Mathf.Sign(dx) * 0.5f: 0f;
				MarkActionIf(commands);
				MaybeEnterDefense(ref commands);
				if (Random.value < 0.15f) state = State.ThrowMixup;
			}

			protected virtual void TickFrameTrap(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (CanAttack()) EnqueueHeavy();
				commands.moveX = 0f;
				MarkActionIf(commands);
				state = State.Pressure;
			}

			protected virtual void TickThrowMixup(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (dist < 1.0f && CanAttack()) EnqueueThrow();

				else if (CanAttack()) EnqueueLight();
				commands.moveX = 0f;
				MarkActionIf(commands);
				state = State.Pressure;
			}

			protected virtual void TickAntiAir(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				if (CanAttack()) EnqueueHeavy();
				commands.moveX = 0f;
				MarkActionIf(commands);
				state = State.Poke;
			}

			protected virtual void Reevaluate(ref FightingGame.Combat.Actors.FighterCommands commands, float dx, float dist)
			{
				stateUntil = Time.time + reevaluateInterval;
				var opp = fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>();
				bool opponentThreat = opp && opp.DebugHitActive;
				bool opponentWhiff = opp && !opp.DebugHitActive && Random.value < 0.25f;
				bool opponentDown = opp && (opp.GetCurrentStateName().StartsWith("Downed") || opp.GetCurrentStateName() == "Wakeup");

				if (opponentDown)
				{
					state = State.Okizeme;
					return;
				}
				if (opponentWhiff && dist <= cfg.approachDistance * 1.2f) {
					state = State.WhiffPunish;
					return;
				}
				if (opponentThreat && Random.value < cfg.blockProbability)
				{
					state = State.EvadeBlock;
					stateUntil = Time.time + 0.35f;
					return;
				}
				if (opponentThreat && Random.value < 0.2f && fighter.IsGrounded()) {
					state = State.EvadeDodge;
					stateUntil = Time.time + 0.1f;
					return;
				}
				if (dist < cfg.retreatDistance)
				{
					state = State.Retreat;
					stateUntil = Time.time + 0.5f;
					return;
				}
				if (dist <= cfg.approachDistance)
				{
					state = State.Poke;
					return;
				}
				state = State.Approach;
			}

			protected bool CanAttack()
			{
				return Time.time >= nextAttackAt;
			}
			protected void EnqueueLight()
			{
				queue.EnqueueCombo(CommandToken.Light);
				nextAttackAt = Time.time + Random.Range(cfg.attackCooldownRange.x, cfg.attackCooldownRange.y);
				lastActionAt = Time.time;
			}
			protected void EnqueueHeavy()
			{
				queue.EnqueueCombo(CommandToken.Heavy);
				nextAttackAt = Time.time + Mathf.Max(0.25f, cfg.attackCooldownRange.x);
				lastActionAt = Time.time;
			}
			protected void EnqueueThrow()
			{
				var off = fighter.HRoot?.Offense;
				if (off != null) off.BeginThrowFlat();
				nextAttackAt = Time.time + 0.5f;
				lastActionAt = Time.time;
			}
			protected void EnqueueLightOrHeavy(float lightBias)
			{
				if (Random.value < lightBias) EnqueueLight();
				else EnqueueHeavy();
			}

			protected void MaybeEnterDefense(ref FightingGame.Combat.Actors.FighterCommands commands)
			{
				var opp = fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>();
				if (opp && opp.DebugHitActive)
				{
					if (Random.value < cfg.blockProbability)
					{
						commands.block = true;
						state = State.EvadeBlock;
						stateUntil = Time.time + 0.3f;
					}
					else if (fighter.IsGrounded() && Random.value < 0.15f) {
						commands.dodge = true;
						state = State.EvadeDodge;
						stateUntil = Time.time + 0.1f;
					}
				}
			}

			protected void MarkActionIf(in FightingGame.Combat.Actors.FighterCommands commands)
			{
				if (commands.block || commands.dodge || commands.jump || Mathf.Abs(commands.moveX) > 0.01f) lastActionAt = Time.time;
			}
		}

		class EasyStrategy : BaseStrategy
		{
			public override void Initialize(FightingGame.Combat.Actors.FighterActor fighter, CommandQueue queue, AI.AIConfig cfg)
			{
				base.Initialize(fighter, queue, cfg);
				reevaluateInterval = 0.3f;
				idleThreshold = 1.2f;
			}
			public override void Tick(ref FightingGame.Combat.Actors.FighterCommands commands)
			{
				// reset snapshot
				commands.moveX = 0f;
				commands.jump = commands.crouch = commands.light = commands.heavy = commands.block = commands.dodge = false;
				if (fighter == null || fighter.opponent == null) return;
				float dx = fighter.opponent.position.x - fighter.transform.position.x;
				float dist = Mathf.Abs(dx);
				if (Time.time >= stateUntil || Time.time - lastActionAt > idleThreshold)
				{
					Reevaluate(ref commands, dx, dist);
				}
				// Easy: only use Approach, Poke, Attack, EvadeBlock, Retreat
				switch (state)
				{
					case State.Approach:
						TickApproach(ref commands, dx, dist);
						break;
					case State.Poke:
						TickPoke(ref commands, dx, dist);
						break;
					case State.Attack:
						TickAttack(ref commands, dx, dist);
						break;
					case State.EvadeBlock:
						TickEvadeBlock(ref commands, dx, dist);
						break;
					case State.Retreat:
						TickRetreat(ref commands, dx, dist);
						break;
					default:
						state = State.Poke;
						TickPoke(ref commands, dx, dist);
						break;
				}
			}
		}

		class NormalStrategy : BaseStrategy
		{
			public override void Initialize(FightingGame.Combat.Actors.FighterActor fighter, CommandQueue queue, AI.AIConfig cfg)
			{
				base.Initialize(fighter, queue, cfg);
				reevaluateInterval = 0.2f;
				idleThreshold = 1.0f;
			}
			public override void Tick(ref FightingGame.Combat.Actors.FighterCommands commands)
			{
				// reset snapshot
				commands.moveX = 0f;
				commands.jump = commands.crouch = commands.light = commands.heavy = commands.block = commands.dodge = false;
				if (fighter == null || fighter.opponent == null) return;
				float dx = fighter.opponent.position.x - fighter.transform.position.x;
				float dist = Mathf.Abs(dx);
				if (Time.time >= stateUntil || Time.time - lastActionAt > idleThreshold)
				{
					Reevaluate(ref commands, dx, dist);
				}
				// Normal: Approach, Poke, Attack, EvadeBlock, EvadeDodge, Retreat, WhiffPunish, Pressure, ThrowMixup
				switch (state)
				{
					case State.Approach:
						TickApproach(ref commands, dx, dist);
						break;
					case State.Poke:
						TickPoke(ref commands, dx, dist);
						break;
					case State.Attack:
						TickAttack(ref commands, dx, dist);
						break;
					case State.EvadeBlock:
						TickEvadeBlock(ref commands, dx, dist);
						break;
					case State.EvadeDodge:
						TickEvadeDodge(ref commands, dx, dist);
						break;
					case State.Retreat:
						TickRetreat(ref commands, dx, dist);
						break;
					case State.WhiffPunish:
						TickWhiffPunish(ref commands, dx, dist);
						break;
					case State.Pressure:
						TickPressure(ref commands, dx, dist);
						break;
					case State.ThrowMixup:
						TickThrowMixup(ref commands, dx, dist);
						break;
					default:
						state = State.Poke;
						TickPoke(ref commands, dx, dist);
						break;
				}
			}
		}

		class HardStrategy : BaseStrategy
		{
			public override void Initialize(FightingGame.Combat.Actors.FighterActor fighter, CommandQueue queue, AI.AIConfig cfg)
			{
				base.Initialize(fighter, queue, cfg);
				reevaluateInterval = 0.15f;
				idleThreshold = 0.8f;
			}
			public override void Tick(ref FightingGame.Combat.Actors.FighterCommands commands)
			{
				// reset snapshot
				commands.moveX = 0f;
				commands.jump = commands.crouch = commands.light = commands.heavy = commands.block = commands.dodge = false;
				if (fighter == null || fighter.opponent == null) return;
				float dx = fighter.opponent.position.x - fighter.transform.position.x;
				float dist = Mathf.Abs(dx);
				if (Time.time >= stateUntil || Time.time - lastActionAt > idleThreshold)
				{
					Reevaluate(ref commands, dx, dist);
				}
				// Hard: use full set including FrameTrap, Okizeme, AntiAir
				switch (state)
				{
					case State.Approach:
						TickApproach(ref commands, dx, dist);
						break;
					case State.Poke:
						TickPoke(ref commands, dx, dist);
						break;
					case State.Attack:
						TickAttack(ref commands, dx, dist);
						break;
					case State.EvadeBlock:
						TickEvadeBlock(ref commands, dx, dist);
						break;
					case State.EvadeDodge:
						TickEvadeDodge(ref commands, dx, dist);
						break;
					case State.Retreat:
						TickRetreat(ref commands, dx, dist);
						break;
					case State.WhiffPunish:
						TickWhiffPunish(ref commands, dx, dist);
						break;
					case State.Okizeme:
						TickOkizeme(ref commands, dx, dist);
						break;
					case State.Pressure:
						TickPressure(ref commands, dx, dist);
						break;
					case State.FrameTrap:
						TickFrameTrap(ref commands, dx, dist);
						break;
					case State.ThrowMixup:
						TickThrowMixup(ref commands, dx, dist);
						break;
					case State.AntiAir:
						TickAntiAir(ref commands, dx, dist);
						break;
					default:
						state = State.Poke;
						TickPoke(ref commands, dx, dist);
						break;
				}
			}
		}

		FightingGame.Combat.Actors.FighterActor opponentActor;
		string opponentState;
		float nextReactionAt;
		void OnOpponentStateChanged(string state, string move)
		{
			opponentState = state;
			#if UNITY_EDITOR
			Debug.Log($"[AI Reactive] Opponent state -> {state} {move}");
			#endif
		}
		void EvaluateReactivePlays()
		{
			if (opponentActor == null)
			{
				return;
			}
			if (Time.time < nextReactionAt)
			{
				return;
			}
			float edgeDist = ComputeHurtboxEdgeDistanceX(fighter, opponentActor);
			float dx = opponentActor.transform.position.x - fighter.transform.position.x;
			// Anti-air: if opponent airborne (Jump) and近距離，重擊
			if (opponentState == "Jump" && edgeDist < 0.9f)
			{
				fighter.EnterAttackHFSM("Heavy"); nextReactionAt = Time.time + 0.5f; return;
			}
			// Pressure on block：對手在防禦時靠近並點輕
			if ((opponentState == "Block" || opponentState == "Block(Crouch)") && edgeDist < 0.6f)
			{
				ForceAttackLight(); nextReactionAt = Time.time + 0.4f; return;
			}
			// Punish heal：對手治愈時追擊
			if (opponentState == "Heal")
			{
				NudgePursueAndAttack(opponentActor, 1.2f, 0.1f); nextReactionAt = Time.time + 0.6f; return;
			}
			// Whiff punish: 對手剛從 Attack-* 回到 Idle，且距離允許 -> 重擊
			if (opponentState.StartsWith("Attack") && edgeDist > 0.4f && edgeDist < 1.4f)
			{
				// 等下個 Update 檢測到對方不再 Attack 時施放
			}
		}
	}
}