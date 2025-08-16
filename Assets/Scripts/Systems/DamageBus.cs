using UnityEngine;

namespace Systems {
	/// <summary>
	/// Static event bus for broadcasting combat damage notifications to UI or other systems.
	/// </summary>
	public static class DamageBus {
		public delegate void DamageEvent(int amount, Vector3 worldPosition, bool blocked, FightingGame.Combat.Actors.FighterActor attacker, FightingGame.Combat.Actors.FighterActor victim);
		public static event DamageEvent OnDamage;

		public static void Raise(int amount, Vector3 worldPosition, bool blocked, FightingGame.Combat.Actors.FighterActor attacker, FightingGame.Combat.Actors.FighterActor victim)
		{
			var handler = OnDamage;
			if (handler != null)
			{
				handler.Invoke(amount, worldPosition, blocked, attacker, victim);
			}
		}
	}
}