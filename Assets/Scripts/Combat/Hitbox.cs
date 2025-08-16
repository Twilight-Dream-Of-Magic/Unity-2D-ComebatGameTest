using UnityEngine;

namespace FightingGame.Combat {
	/// <summary>
	/// An attack collider owned by a Fighter. When touching a Hurtbox
	/// of a different owner, it builds the effective DamageInfo (can be overridden by current action data)
	/// and forwards it to the target's DamageReceiver.
	/// </summary>
	[RequireComponent(typeof(BoxCollider2D))]
	public class Hitbox : MonoBehaviour {
		public Actors.FighterActor owner;
		public bool active;
		public DamageInfo baseInfo;

		[Header("Collider Sizing (Hitbox 管理)")]
		public Vector2 desiredSize = new Vector2(1.4f, 1.0f);
		public Vector2 desiredOffset = new Vector2(1.0f, 0.0f);
		public bool autoHeightByCapsule = false;
		public float heightFallback = 1.0f;

		BoxCollider2D _box;

		void CacheBox()
		{
			if (_box == null)
			{
				_box = GetComponent<BoxCollider2D>();
				if (_box == null)
				{
					_box = gameObject.AddComponent<BoxCollider2D>();
				}
			}
		}

		/// <summary>
		/// 由外部呼叫設定尺寸/偏移，避免透過生命周期覆蓋。
		/// </summary>
		public void ConfigureCollider(Vector2 size, Vector2 offset, bool autoByCapsule = false, float fallbackHeight = 1.0f, bool isTrigger = true)
		{
			autoHeightByCapsule = autoByCapsule;
			heightFallback = fallbackHeight;
			desiredSize = size;
			desiredOffset = offset;
			CacheBox();
			ApplyColliderSizing(isTrigger);
		}

		void ApplyColliderSizing(bool isTrigger)
		{
			if (_box == null)
			{
				return;
			}
			if (isTrigger)
			{
				_box.isTrigger = true;
			}
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
			if (desiredSize.sqrMagnitude > 0.0f)
			{
				_box.size = new Vector2(desiredSize.x, height);
			}
			_box.offset = desiredOffset;
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
			CacheBox();
			if (_box == null)
			{
				return;
			}
			var bounds = _box.bounds;
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