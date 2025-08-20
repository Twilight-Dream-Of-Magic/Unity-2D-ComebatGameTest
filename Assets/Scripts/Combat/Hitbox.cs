using UnityEngine;

namespace FightingGame.Combat
{
	/// <summary>
	/// Hitbox — an attack collider owned by a Fighter.
	/// EN: When overlapping with a Hurtbox of a different owner, constructs DamageInfo (overridable by action data)
	///     and forwards it to the target's DamageReceiver.
	/// ZH: 角色擁有的攻擊碰撞體；當與他人 Hurtbox 接觸時，建立 DamageInfo（可由動作數據覆蓋）並傳遞給目標的 DamageReceiver。
	/// </summary>
	[RequireComponent(typeof(BoxCollider2D))]
	public class Hitbox : MonoBehaviour
	{
		/// <summary>Owner fighter actor of this hitbox. 擁有此 Hitbox 的角色。</summary>
		public Actors.FighterActor owner;

		/// <summary>If true, the hitbox is currently active. 是否啟用中。</summary>
		public bool active;

		/// <summary>Base damage info template. 基礎傷害資訊模板。</summary>
		public DamageInfo baseInfo;

		[Header("Collider Sizing (Hitbox 管理)")]
		/// <summary>Desired collider size. 預期的碰撞體尺寸。</summary>
		public Vector2 desiredSize = new Vector2(1.4f, 1.0f);

		/// <summary>Desired collider offset. 預期的碰撞體偏移。</summary>
		public Vector2 desiredOffset = new Vector2(1.0f, 0.0f);

		/// <summary>Auto-adjust height using CapsuleCollider2D if available. 若有 CapsuleCollider2D，則自動依其高度。</summary>
		public bool autoHeightByCapsule = false;

		/// <summary>Fallback height when auto-height fails. 自動高度失敗時的備用高度。</summary>
		public float heightFallback = 1.0f;

		private BoxCollider2D _box;

		/// <summary>Cache or add the BoxCollider2D component. 快取或新增 BoxCollider2D。</summary>
		private void CacheBox()
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
		/// Configure collider size/offset externally.
		/// EN: Called externally to configure collider, prevents lifecycle overwrite.
		/// ZH: 由外部呼叫設定尺寸/偏移，避免透過生命周期覆蓋。
		/// </summary>
		public void ConfigureCollider(Vector2 size, Vector2 offset, bool autoByCapsule = false, float fallbackHeight = 1.0f, bool isTrigger = true)
		{
			this.autoHeightByCapsule = autoByCapsule;
			this.heightFallback = fallbackHeight;
			this.desiredSize = size;
			this.desiredOffset = offset;

			this.CacheBox();
			this.ApplyColliderSizing(isTrigger);
		}

		/// <summary>
		/// Applies collider sizing parameters to the BoxCollider2D.
		/// EN: Resizes collider based on desired parameters or capsule fallback.
		/// ZH: 依據設定或 CapsuleCollider2D 高度套用 BoxCollider2D 尺寸。
		/// </summary>
		private void ApplyColliderSizing(bool isTrigger)
		{
			if (_box == null)
			{
				return;
			}

			if (isTrigger)
			{
				_box.isTrigger = true;
			}

			float height = this.desiredSize.y;

			if (this.autoHeightByCapsule)
			{
				var capsule = GetComponentInParent<CapsuleCollider2D>();

				if (capsule != null)
				{
					height = capsule.size.y;
				}
				else
				{
					height = (this.heightFallback > 0.0f) ? this.heightFallback : this.desiredSize.y;
				}
			}

			if (this.desiredSize.sqrMagnitude > 0.0f)
			{
				_box.size = new Vector2(this.desiredSize.x, height);
			}

			_box.offset = this.desiredOffset;
		}

		private void OnTriggerStay2D(Collider2D other)
		{
			this.TryApply(other);
		}

		/// <summary>
		/// Attempts to apply hit logic when colliding with a Hurtbox.
		/// EN: If valid, builds effective DamageInfo and forwards to target.
		/// ZH: 嘗試在碰撞 Hurtbox 時套用攻擊，若有效則建立傷害資訊並傳遞。
		/// </summary>
		public void TryApply(Collider2D other)
		{
			var hurt = other.GetComponent<Hurtbox>();

			if (hurt == null)
			{
				return;
			}

			if (!this.active)
			{
				return;
			}

			if (hurt.owner == this.owner)
			{
				return;
			}

#if UNITY_EDITOR
			Debug.Log($"[Hitbox] {owner?.name} hit {hurt.owner?.name} dmg={baseInfo.damage} level={baseInfo.level} active={active}");
#endif

			DamageInfo info = this.BuildEffectiveDamageInfo();
			hurt.owner.TakeHit(info, this.owner);
		}

		/// <summary>
		/// Builds the effective DamageInfo, overriding baseInfo with action data.
		/// EN: Incorporates current move parameters if available.
		/// ZH: 建立最終的 DamageInfo，若有動作數據則覆蓋基礎值。
		/// </summary>
		private DamageInfo BuildEffectiveDamageInfo()
		{
			DamageInfo info = this.baseInfo;

			var action = this.owner.CurrentAction;

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

		/// <summary>Enable or disable this hitbox. 啟用或停用 Hitbox。</summary>
		public void SetActive(bool value)
		{
			this.active = value;
		}

#if UNITY_EDITOR
		/// <summary>Draws hitbox gizmos in editor for debugging. 在編輯器繪製 Hitbox 的 Gizmo 用於除錯。</summary>
		private void OnDrawGizmos()
		{
			this.CacheBox();

			if (_box == null)
			{
				return;
			}

			var bounds = _box.bounds;

			// 略微縮小，避免與 Hurtbox 線框重疊導致視覺難以區分
			var size = bounds.size * 0.96f;
			var center = bounds.center;

			Color fill = new Color(1f, 0.4f, 0.7f, this.active ? 0.20f : 0.08f); // 粉色實心（啟用時更顯眼）
			Color wire = new Color(1f, 0.4f, 0.7f, 1f); // 粉色線框

			Gizmos.color = fill;
			Gizmos.DrawCube(center, size);

			Gizmos.color = wire;
			Gizmos.DrawWireCube(center, size);
		}
#endif
	}
}
