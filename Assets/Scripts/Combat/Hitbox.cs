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

		[Header("Collider Sizing (Hitbox 管理)")]
		public Vector2 desiredSize = new Vector2(1.4f, 1.0f);
		public Vector2 desiredOffset = new Vector2(1.0f, 0.0f);
		public bool autoHeightByCapsule = true;
		public float heightFallback = 1.0f;

		void Awake()
		{
			ApplyColliderSizing();
		}

		void OnValidate()
		{
			ApplyColliderSizing();
		}

		void ApplyColliderSizing()
		{
			var collider = GetComponent<Collider2D>() as BoxCollider2D;
			if (collider == null)
			{
				collider = gameObject.GetComponent<BoxCollider2D>();
				if (collider == null)
				{
					collider = gameObject.AddComponent<BoxCollider2D>();
				}
			}
			collider.isTrigger = true;
			float height = desiredSize.y;
			if (autoHeightByCapsule)
			{
				var capsule = GetComponentInParent<CapsuleCollider2D>();
				if (capsule != null)
				{
					height = capsule.size.y;
				}
				else
				{
					height = heightFallback > 0.0f ? heightFallback : desiredSize.y;
				}
			}
			collider.size = new Vector2(desiredSize.x, height);
			collider.offset = new Vector2(desiredOffset.x, desiredOffset.y);
		}

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
				info.isSuper = (action.meterCost > 0) || (action.triggerName == "Super");
			}
			return info;
		}

		public void SetActive(bool value)
		{
			active = value;
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			var collider = GetComponent<Collider2D>();
			if (collider == null)
			{
				return;
			}
			var bounds = collider.bounds;
			// 略微縮小，避免與 Hurtbox 線框重疊導致視覺難以區分
			var size = bounds.size * 0.96f;
			var center = bounds.center;
			Color fill = new Color(1f, 0.4f, 0.7f, active ? 0.20f : 0.08f); // 粉色實心（啟用時更顯眼）
			Color wire = new Color(1f, 0.4f, 0.7f, 1f); // 粉色線框
			Gizmos.color = fill;
			Gizmos.DrawCube(center, size);
			Gizmos.color = wire;
			Gizmos.DrawWireCube(center, size);
		}
#endif
	}
}