using UnityEngine;
using FightingGame.Combat;

namespace Data {
    /// <summary>
    /// Data-driven definition of a combat action (attack, super, heal, etc.).
    /// 描述一個可執行的戰鬥動作（普攻/重擊/必殺/治療等）的完整資料，供角色在狀態機中引用。
    /// </summary>
    [CreateAssetMenu(menuName = "Fighter/Action Definition")]
    public class CombatActionDefinition : ScriptableObject {
        [Header("Identity")]
        public string nameId;
        public string triggerName;

        [Header("Frame Data (seconds)")]
        public float startup = 0.083f;  // 5f @60fps
        public float active = 0.05f;    // 3f
        public float recovery = 0.166f; // 10f

        [Header("Hit/Block Stun (seconds)")]
        public float hitstun = 0.1f;    // 6f
        public float blockstun = 0.066f; // 4f

        [Header("Damage & Meter")]
        public int damage = 8;
        public int meterOnHit = 50;
        public int meterOnBlock = 20;
        public int meterCost = 0; // cost to use this action
        public int healAmount = 0; // heal on use (applied on trigger)

        [Header("Knockback & Pushback")]
        public Vector2 knockback = new Vector2(2f, 2f);
        public float pushbackOnHit = 0.4f;
        public float pushbackOnBlock = 0.6f;

        [Header("Knockdown")]
        public KnockdownKind knockdownKind = KnockdownKind.None;

        [Header("Hit Properties")]
        public HitLevel hitLevel = HitLevel.Mid;
        public HitType hitType = HitType.Strike;
        public int priority = 1;
        public bool canBeBlocked = true;

        [Header("Hit-Stop (seconds)")]
        public float hitstopOnHit = 0.1f;   // 6f
        public float hitstopOnBlock = 0.066f; // 4f

        [Header("Cancel Rules")]
        public bool canCancelOnHit = true;
        public bool canCancelOnBlock = false;
        public bool canCancelOnWhiff = false;
        [Tooltip("Cancel window when move hits: [start,end] in seconds from attack start")] public Vector2 onHitCancelWindow = new Vector2(0.0f, 0.25f);
        [Tooltip("Cancel window when move is blocked: [start,end] in seconds from attack start")] public Vector2 onBlockCancelWindow = new Vector2(0.0f, 0.18f);
        [Tooltip("Cancel window when move whiffs: [start,end] in seconds from attack start")] public Vector2 onWhiffCancelWindow = new Vector2(0.0f, 0.12f);
        public string[] cancelIntoTriggers; // allowed cancel targets (null/empty = allow any)

        [Header("Aerial")]
        public float landingLag = 0.06f; // added on landing if this action was used in air
    }
}