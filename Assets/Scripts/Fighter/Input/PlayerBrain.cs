using UnityEngine;
using FightingGame.Combat;

namespace Fighter.InputSystem
{
	/// <summary>
	/// Player input reader and dispatcher into CommandQueue and FighterActor
	/// </summary>
	[DefaultExecutionOrder(30)]
	public class PlayerBrain : MonoBehaviour
	{
		[Header("Wiring")] public FightingGame.Combat.Actors.FighterActor fighter;
		[Header("Reader")] public float horizontalScale = 1f;
		public Data.InputTuningConfig inputTuning;

		CommandQueue commandQueue;
		SpecialInputResolver resolver;

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
			resolver = GetComponent<SpecialInputResolver>();
			if (!resolver)
			{
				resolver = gameObject.AddComponent<SpecialInputResolver>();
			}
			resolver.fighter = fighter;
			resolver.commandQueue = commandQueue;
			resolver.tuning = inputTuning;
		}

		void Update()
		{
			ReadKeyboard();
		}

		void ReadKeyboard()
		{
			var c = new FightingGame.Combat.Actors.FighterCommands();
			c.moveX = Input.GetAxisRaw("Horizontal") * horizontalScale;
			c.jump = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
			c.crouch = Input.GetKey(KeyCode.S);
			c.block = Input.GetKey(KeyCode.L);
			c.dodge = Input.GetKey(KeyCode.Semicolon);
			bool lightDown = Input.GetKeyDown(KeyCode.J);
			bool heavyDown = Input.GetKeyDown(KeyCode.K);
			c.light = lightDown;
			c.heavy = heavyDown;
			fighter.SetCommands(in c);

			// Directions to Normal channel for specials history
			if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
			{
				commandQueue.EnqueueNormal(CommandToken.Up);
			}
			if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
			{
				commandQueue.EnqueueNormal(CommandToken.Down);
			}
			if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
			{
				commandQueue.EnqueueNormal(fighter.facingRight ? CommandToken.Back : CommandToken.Forward);
			}
			if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
			{
				commandQueue.EnqueueNormal(fighter.facingRight ? CommandToken.Forward : CommandToken.Back);
			}

			// Enqueue combo-tail keys first (for specials)
			if (lightDown)
			{
				commandQueue.EnqueueCombo(CommandToken.Light);
			}
			if (heavyDown)
			{
				commandQueue.EnqueueCombo(CommandToken.Heavy);
			}

			// Fallback: if no special consumed (no CurrentMove set this frame), trigger basic attack immediately
			if (lightDown && fighter.CurrentMove == null)
			{
				fighter.EnterAttackHFSM("Light");
			}
			if (heavyDown && fighter.CurrentMove == null)
			{
				fighter.EnterAttackHFSM("Heavy");
			}

			// Throw: direct domain call (air/guard-break/normal), no queue
			if (Input.GetKeyDown(KeyCode.U))
			{
				var off = fighter.HRoot?.Offense;
				var opp = fighter.opponent ? fighter.opponent.GetComponent<FightingGame.Combat.Actors.FighterActor>() : null;
				if (off != null)
				{
					if (!fighter.IsGrounded())
					{
						off.BeginAirThrowFlat();
					}
					else if (opp && opp.PendingCommands.block && fighter.IsOpponentInThrowRange(1.0f))
					{
						off.BeginGuardBreakThrowFlat();
					}
					else
					{
						off.BeginThrowFlat();
					}
				}
			}
		}
	}
}