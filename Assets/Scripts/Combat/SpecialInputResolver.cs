using System.Collections.Generic;
using UnityEngine;
using FightingGame.Combat;
using Data;

namespace Fighter {
	/// <summary>
	/// Thin coordinator that wires CommandQueue tokens to SpecialMatcher and SpecialExecutor.
	/// </summary>
	public class SpecialInputResolver : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public CommandQueue commandQueue;
		public SpecialMoveSet specialSet;
		public InputTuningConfig tuning;
		SpecialMatcher matcher;
		SpecialExecutor executor;

		void Awake() {
			if (!fighter) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			if (!commandQueue) commandQueue = GetComponent<CommandQueue>();
			matcher = GetComponent<SpecialMatcher>(); if (!matcher) matcher = gameObject.AddComponent<SpecialMatcher>(); matcher.specialSet = specialSet; matcher.Configure(tuning);
			executor = GetComponent<SpecialExecutor>(); if (!executor) executor = gameObject.AddComponent<SpecialExecutor>(); executor.fighter = fighter; executor.matcher = matcher;
			if (commandQueue) {
				commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Down,  time => executor.HandleToken(CommandToken.Down, time), priority: 100);
				commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Up,    time => executor.HandleToken(CommandToken.Up, time), priority: 100);
				commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Forward, time => executor.HandleToken(CommandToken.Forward, time), priority: 100);
				commandQueue.RegisterHandler(CommandChannel.Normal, CommandToken.Back,  time => executor.HandleToken(CommandToken.Back, time), priority: 100);
				commandQueue.RegisterHandler(CommandChannel.Combo, CommandToken.Light,  time => executor.HandleToken(CommandToken.Light, time), priority: 100);
				commandQueue.RegisterHandler(CommandChannel.Combo, CommandToken.Heavy,  time => executor.HandleToken(CommandToken.Heavy, time), priority: 100);
			}
		}

		void OnDestroy() {
			if (commandQueue) {
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Down,  time => executor.HandleToken(CommandToken.Down, time));
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Up,    time => executor.HandleToken(CommandToken.Up, time));
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Forward, time => executor.HandleToken(CommandToken.Forward, time));
				commandQueue.UnregisterHandler(CommandChannel.Normal, CommandToken.Back,  time => executor.HandleToken(CommandToken.Back, time));
				commandQueue.UnregisterHandler(CommandChannel.Combo, CommandToken.Light,  time => executor.HandleToken(CommandToken.Light, time));
				commandQueue.UnregisterHandler(CommandChannel.Combo, CommandToken.Heavy,  time => executor.HandleToken(CommandToken.Heavy, time));
			}
		}
	}
}