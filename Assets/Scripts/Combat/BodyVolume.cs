using UnityEngine;

namespace FightingGame.Combat {
	/// <summary>
	/// BodyVolume: 角色本體的「佔位碰撞體」。
	/// - 用於與其他角色發生實體推擠，避免重疊/穿插。
	/// - 不參與攻防判定（攻防仍由 Hitbox/Hurtbox 負責）。
	/// </summary>
	[RequireComponent(typeof(Collider2D))]
	public class BodyVolume : MonoBehaviour {
		private void Reset() {
			var col = GetComponent<Collider2D>();
			if (col != null) col.isTrigger = false; // 佔位碰撞體需為實體碰撞
		}
	}
}