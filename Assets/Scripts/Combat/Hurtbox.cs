using UnityEngine;

namespace FightingGame.Combat
{
	/// <summary>
	/// Hurtbox — a trigger collider region that can receive hits.
	/// EN: Split into regions (Head/Torso/Legs) so the fighter can toggle invulnerability per region each frame.
	///     Owned by a FighterActor. Gizmos are rendered for quick debugging.
	/// ZH: 可受擊的觸發碰撞區域；劃分為頭部/軀幹/腿部，以便角色每幀切換部位無敵狀態。由 FighterActor 擁有，並可在編輯器顯示 Gizmo 以利除錯。
	/// </summary>
	public enum HurtRegion
	{
		/// <summary>Head region. 頭部區域。</summary>
		Head,

		/// <summary>Torso region. 軀幹區域。</summary>
		Torso,

		/// <summary>Legs region. 腿部區域。</summary>
		Legs
	}

	[RequireComponent(typeof(BoxCollider2D))]
	public class Hurtbox : MonoBehaviour
	{
		/// <summary>Owning fighter. Set by setup so hit detection can route damage. 擁有者角色，供傷害判定路由使用。</summary>
		public FightingGame.Combat.Actors.FighterActor owner;

		/// <summary>Semantic body region used by guard/invulnerability logic. 用於防禦/無敵邏輯的語義區域。</summary>
		public HurtRegion region = HurtRegion.Torso;

		/// <summary>Latched each frame to enable/disable this region. 每幀由 FighterController 鎖定此區域是否啟用。</summary>
		public bool enabledThisFrame = true;

		[Header("Posture Activation")]
		/// <summary>Active while standing. 站立時啟用。</summary>
		public bool activeStanding = true;

		/// <summary>Active while crouching. 蹲下時啟用。</summary>
		public bool activeCrouching = true;

		/// <summary>Active while airborne. 空中時啟用。</summary>
		public bool activeAirborne = true;

		[Header("Collider Sizing (Hurtbox 管理)")]
		/// <summary>Desired collider size. 預期的碰撞體尺寸。</summary>
		public Vector2 desiredSize = Vector2.zero;

		/// <summary>Desired collider offset. 預期的碰撞體偏移。</summary>
		public Vector2 desiredOffset = Vector2.zero;

		/// <summary>If true, ensure collider is trigger. 若為 true，則確保為觸發器。</summary>
		public bool ensureTrigger = true;

		private BoxCollider2D _box;

		/// <summary>Cache or add the BoxCollider2D. 快取或新增 BoxCollider2D。</summary>
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
		/// Configure collider externally.
		/// EN: Called externally to set size/offset; avoids lifecycle overwrite.
		/// ZH: 由外部呼叫設定尺寸/偏移，避免透過生命周期覆蓋。
		/// </summary>
		public void ConfigureCollider(Vector2 size, Vector2 offset, bool isTrigger = true)
		{
			this.desiredSize = size;
			this.desiredOffset = offset;

			this.CacheBox();
			this.ApplyColliderSizing(isTrigger);
		}

		/// <summary>
		/// Apply collider sizing to BoxCollider2D.
		/// EN: Resizes and repositions collider based on settings.
		/// ZH: 套用尺寸與偏移到 BoxCollider2D。
		/// </summary>
		private void ApplyColliderSizing(bool isTrigger)
		{
			if (_box == null)
			{
				return;
			}

			if (this.ensureTrigger && isTrigger)
			{
				_box.isTrigger = true;
			}

			if (this.desiredSize.sqrMagnitude > 0f)
			{
				_box.size = this.desiredSize;
			}

			_box.offset = this.desiredOffset;
		}

#if UNITY_EDITOR
		/// <summary>
		/// Draw gizmos in editor for debug.
		/// EN: Renders filled and wireframe cubes to visualize hurtbox.
		/// ZH: 在編輯器中繪製實心與線框方塊，用於視覺化 Hurtbox。
		/// </summary>
		private void OnDrawGizmos()
		{
			this.CacheBox();

			if (_box == null)
			{
				return;
			}

			var bounds = _box.bounds;

			Color fillColor = this.enabledThisFrame ? new Color(0f, 1f, 1f, 0.15f) : new Color(0f, 1f, 1f, 0.04f);
			Gizmos.color = fillColor;
			Gizmos.DrawCube(bounds.center, bounds.size);

			Color wireColor = this.enabledThisFrame ? Color.cyan : new Color(0f, 1f, 1f, 0.25f);
			Gizmos.color = wireColor;
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
#endif
	}
}
