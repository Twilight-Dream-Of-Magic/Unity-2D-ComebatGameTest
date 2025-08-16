using UnityEngine;

namespace FightingGame.Combat {
	/// <summary>
	/// An attack collider owned by a Fighter. When touching a Hurtbox
	/// of a different owner, it builds the effective DamageInfo (can be overridden by current action data)
	/// and forwards it to the target's DamageReceiver.
	/// </summary>
	public class Hitbox : MonoBehaviour {
		public Actors.FighterActor owner;
		public bool active;
		public DamageInfo baseInfo;

		void OnTriggerStay2D(Collider2D other)
		{
			TryApply(other);
		}

		public void TryApply(Collider2D other)
		{
			var hurt = other.GetComponent<Hurtbox>();
			if (hurt == null)
			{
				return;
			}
			if (!active)
			{
				return;
			}
			if (hurt.owner == owner)
			{
				return;
			}
#if UNITY_EDITOR
			Debug.Log($"[Hitbox] {owner?.name} hit {hurt.owner?.name} dmg={baseInfo.damage} level={baseInfo.level} active={active}");
#endif
			var info = BuildEffectiveDamageInfo();
			hurt.owner.TakeHit(info, owner);
		}

		DamageInfo BuildEffectiveDamageInfo()
		{
			var info = baseInfo;
			var action = owner.CurrentMove;
			if (action != null)
			{
				info.damage = action.damage;
				info.level = action.hitLevel;
				info.hitstun = action.hitstun;
				info.blockstun = action.blockstun;
				info.knockback = action.knockback;
				info.canBeBlocked = action.canBeBlocked;
				info.hitstopOnHit = action.hitstopOnHit;
				info.hitstopOnBlock = action.hitstopOnBlock;
				info.pushbackOnHit = action.pushbackOnHit;
				info.pushbackOnBlock = action.pushbackOnBlock;
				info.knockdownKind = action.knockdownKind;
				info.meterOnHit = action.meterOnHit;
				info.meterOnBlock = action.meterOnBlock;
			}
			return info;
		}

		public void SetActive(bool value)
		{
			active = value;
		}
	}
}