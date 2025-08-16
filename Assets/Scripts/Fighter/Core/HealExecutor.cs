using UnityEngine;

namespace Fighter.Core {
	/// <summary>
	/// Executes healing actions for a fighter.
	/// </summary>
	public class HealExecutor : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;

		public void Execute(string trigger)
		{
			if (fighter == null)
			{
				return;
			}
			var action = fighter.actionSet != null ? fighter.actionSet.Get(trigger) : null;
			if (action == null)
			{
				return;
			}
			if (fighter.meter < action.meterCost)
			{
				return;
			}
			var resources = fighter.GetComponent<FighterResources>();
			if (resources == null)
			{
				resources = fighter.gameObject.AddComponent<FighterResources>();
			}
			resources.DecreaseMeter(action.meterCost);
			if (action.healAmount > 0)
			{
				resources.IncreaseHealth(action.healAmount);
			}
			fighter.animator.SetTrigger(trigger);
		}
	}
}