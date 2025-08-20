using UnityEngine;

namespace FightingGame.Combat
{
	/// <summary>
	/// BodyVolume — character body **occupancy collider**.
	/// EN: Prevents character interpenetration by providing a solid body volume used only for physical pushing.
	///     - Participates in physics collision resolution between characters.
	///     - Does **not** participate in attack/defense judgment (Hitbox/Hurtbox are responsible).
	/// ZH: 角色本體的「佔位碰撞體」，僅用於實體推擠避免重疊/穿插。
	///     - 僅參與物理解算的推擠，不參與攻防判定（攻防由 Hitbox/Hurtbox 負責）。
	/// 風格：使用制表符（Tab）縮排，TabSize=4；命名不縮寫；雙語註釋。
	/// </summary>
	[RequireComponent(typeof(Collider2D))]
	[DisallowMultipleComponent]
	[AddComponentMenu("Combat/Body Volume")]
	public sealed class BodyVolume : MonoBehaviour
	{
		/// <summary>
		/// EN: Unity callback to initialize component defaults in the editor.
		///     Ensures the attached Collider2D is a **solid collider** (isTrigger = false),
		///     because this body volume must take part in physics collisions.
		/// ZH: 在編輯器中初始化預設值；強制將 Collider2D 設為「實體碰撞」（isTrigger = false），
		///     以便本體佔位能參與物理解算。
		/// </summary>
		private void Reset()
		{
			Collider2D collider2D = GetComponent<Collider2D>();
			if (collider2D != null)
			{
				collider2D.isTrigger = false; // 佔位碰撞體需為實體碰撞（不可為觸發器）
			}
		}
	}
}
