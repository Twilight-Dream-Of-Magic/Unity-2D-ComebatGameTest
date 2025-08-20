using UnityEngine;
using FightingGame.Combat;

namespace Data
{
	/// <summary>
	/// Data-driven definition of a combat action (attack, super, heal, etc.).
	/// 描述一个可执行的战斗动作（普攻/重击/必杀/治疗等）的完整数据，供角色在状态机中引用。
	/// </summary>
	[CreateAssetMenu(menuName = "Fighter/Action Definition")]
	public class CombatActionDefinition : ScriptableObject
	{
		[Header("Identity")]
		public string nameId; // Unique identifier / 唯一标识符
		public string triggerName; // Animator trigger name / 动画触发器名称

		[Header("Frame Data (seconds)")]
		public float startup = 0.083f;  // Startup frames / 起手时间 (5f @60fps)
		public float active = 0.05f;    // Active frames / 攻击判定帧 (3f)
		public float recovery = 0.166f; // Recovery frames / 收招时间 (10f)

		[Header("Hit/Block Stun (seconds)")]
		public float hitstun = 0.1f;     // Stun duration on hit / 命中硬直 (6f)
		public float blockstun = 0.066f; // Stun duration on block / 防御硬直 (4f)

		[Header("Damage & Meter")]
		public int damage = 8; // Damage dealt / 造成的伤害
		public int meterOnHit = 50; // Meter gain on hit / 命中获得能量
		public int meterOnBlock = 20; // Meter gain on block / 防御获得能量
		public int meterCost = 0; // Cost to perform this action / 使用动作消耗能量
		public int healAmount = 0; // Heal amount applied on use / 使用时的治疗量

		[Header("Knockback & Pushback")]
		public Vector2 knockback = new Vector2(2f, 2f); // Knockback vector / 击退向量
		public float pushbackOnHit = 0.4f; // Pushback when hitting opponent / 命中推退距离
		public float pushbackOnBlock = 0.6f; // Pushback when blocked / 防御推退距离

		[Header("Knockdown")]
		public KnockdownKind knockdownKind = KnockdownKind.None; // Knockdown type / 倒地类型

		[Header("Hit Properties")]
		public HitLevel hitLevel = HitLevel.Mid; // Hit level (High/Low/Mid) / 打击判定等级
		public HitType hitType = HitType.Strike; // Hit type (Strike/Projectile/Throw) / 打击类型
		public int priority = 1; // Priority vs other moves / 动作优先级
		public bool canBeBlocked = true; // Whether this move can be blocked / 是否可防御

		[Header("Hit-Stop (seconds)")]
		public float hitstopOnHit = 0.1f;   // Hit-stop on successful hit / 命中停顿 (6f)
		public float hitstopOnBlock = 0.066f; // Hit-stop on block / 防御停顿 (4f)

		[Header("Cancel Rules")]
		public bool canCancelOnHit = true;   // Allow cancel when hitting / 命中可取消
		public bool canCancelOnBlock = false; // Allow cancel when blocked / 防御可取消
		public bool canCancelOnWhiff = false; // Allow cancel when whiffed / 空挥可取消

		[Tooltip("Cancel window when move hits: [start,end] in seconds from attack start")]
		public Vector2 onHitCancelWindow = new Vector2(0.0f, 0.25f); // 命中取消窗口

		[Tooltip("Cancel window when move is blocked: [start,end] in seconds from attack start")]
		public Vector2 onBlockCancelWindow = new Vector2(0.0f, 0.18f); // 防御取消窗口

		[Tooltip("Cancel window when move whiffs: [start,end] in seconds from attack start")]
		public Vector2 onWhiffCancelWindow = new Vector2(0.0f, 0.12f); // 空挥取消窗口

		public string[] cancelIntoTriggers; // Allowed cancel targets (null/empty = allow any) / 可取消目标（空=任意）

		[Header("Aerial")]
		public float landingLag = 0.06f; // Landing lag if used in air / 空中落地硬直
	}
}
