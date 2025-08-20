using UnityEngine;

namespace FightingGame.Combat
{
	/// <summary>
	/// HitLevel — describes the vertical height of an attack.
	/// EN: Attack height classification (High, Mid, Low, Overhead).
	/// ZH: 攻擊高度分類（上段、中段、下段、上挑）。
	/// </summary>
	public enum HitLevel
	{
		/// <summary>High attack (can be crouched under). 上段攻擊（可低身閃避）。</summary>
		High,

		/// <summary>Mid attack (must be blocked standing). 中段攻擊（需站立防禦）。</summary>
		Mid,

		/// <summary>Low attack (must be blocked crouching). 下段攻擊（需蹲防）。</summary>
		Low,

		/// <summary>Overhead attack (must be blocked standing, hits crouchers). 上挑攻擊（需站立防禦，可打蹲下）。</summary>
		Overhead
	}

	/// <summary>
	/// HitType — describes the nature of the attack.
	/// EN: Attack classification (strike, projectile, throw).
	/// ZH: 攻擊類型（打擊、飛道具、投技）。
	/// </summary>
	public enum HitType
	{
		/// <summary>Strike attack. 打擊攻擊。</summary>
		Strike,

		/// <summary>Projectile attack. 飛道具攻擊。</summary>
		Projectile,

		/// <summary>Throw attack. 投技攻擊。</summary>
		Throw
	}

	/// <summary>
	/// KnockdownKind — describes knockdown severity.
	/// EN: Type of knockdown state applied on hit.
	/// ZH: 擊倒類型。
	/// </summary>
	public enum KnockdownKind
	{
		/// <summary>No knockdown. 不擊倒。</summary>
		None,

		/// <summary>Soft knockdown (can quick-rise). 軟擊倒（可快速起身）。</summary>
		Soft,

		/// <summary>Hard knockdown (must lie down). 硬擊倒（必須倒地）。</summary>
		Hard
	}

	/// <summary>
	/// DamageInfo — immutable payload describing an attack at the moment of contact.
	/// EN: Contains parameters for damage, stun, knockback, block properties, etc.
	///     Values can be overridden by current action data.
	/// ZH: 攻擊命中瞬間的不可變資料結構，包含傷害、硬直、擊退、防禦屬性等；
	///     具體數值可由當前動作資料覆蓋。
	/// </summary>
	[System.Serializable]
	public struct DamageInfo
	{
		/// <summary>Base damage value. 基礎傷害數值。</summary>
		public int damage;

		/// <summary>Attack height classification. 攻擊高度分類。</summary>
		public HitLevel level;

		/// <summary>Hitstun duration in seconds. 硬直時間（秒）。</summary>
		public float hitstun;

		/// <summary>Blockstun duration in seconds. 防禦硬直時間（秒）。</summary>
		public float blockstun;

		/// <summary>Knockback vector applied on hit. 擊中時的擊退向量。</summary>
		public Vector2 knockback;

		/// <summary>If true, the attack can be blocked. 是否可防禦。</summary>
		public bool canBeBlocked;

		/// <summary>Hitstop frames on successful hit. 擊中時的硬直幀數。</summary>
		public float hitstopOnHit;

		/// <summary>Hitstop frames on block. 防禦時的硬直幀數。</summary>
		public float hitstopOnBlock;

		/// <summary>Pushback distance applied on hit. 擊中後的推退距離。</summary>
		public float pushbackOnHit;

		/// <summary>Pushback distance applied on block. 防禦後的推退距離。</summary>
		public float pushbackOnBlock;

		/// <summary>Kind of knockdown induced by the attack. 擊倒種類。</summary>
		public KnockdownKind knockdownKind;

		/// <summary>Meter gain awarded on hit. 擊中時獲得的能量槽。</summary>
		public int meterOnHit;

		/// <summary>Meter gain awarded on block. 防禦時獲得的能量槽。</summary>
		public int meterOnBlock;

		/// <summary>If true, this is a super move (bypasses dodge invulnerability). 是否為超必殺（無視閃避無敵）。</summary>
		public bool isSuper;
	}
}
