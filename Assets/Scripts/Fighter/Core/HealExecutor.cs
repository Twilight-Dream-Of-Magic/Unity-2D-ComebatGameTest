using UnityEngine;

namespace Fighter.Core
{
	/// <summary>
	/// Executes healing actions for a fighter.
	/// �����ί�������̎���������c�����֏͡�
	/// </summary>
	public class HealExecutor : MonoBehaviour
	{
		/// <summary>
		/// Reference to the owning fighter. �����߽�ɫ���á�
		/// </summary>
		public FightingGame.Combat.Actors.FighterActor fighter;

		/// <summary>
		/// Perform a healing action using the given trigger.
		/// ����ָ���� Trigger �����ί�������
		/// </summary>
		/// <param name="trigger">The trigger name that corresponds to a heal action / �ί��������|�l���Q��</param>
		public void Execute(string trigger)
		{
			if (fighter == null)
			{
				return;
			}

			var healAction = fighter.actionSet != null ? fighter.actionSet.Get(trigger) : null;
			if (healAction == null)
			{
				return;
			}

			if (fighter.meter < healAction.meterCost)
			{
				return;
			}

			FighterResources resourceComponent = fighter.GetComponent<FighterResources>();
			if (resourceComponent == null)
			{
				resourceComponent = fighter.gameObject.AddComponent<FighterResources>();
			}

			resourceComponent.DecreaseMeter(healAction.meterCost);

			if (healAction.healAmount > 0)
			{
				resourceComponent.IncreaseHealth(healAction.healAmount);
			}

			if (fighter.animator != null && fighter.animator.runtimeAnimatorController != null)
			{
				fighter.animator.SetTrigger(trigger);
			}
		}
	}
}
