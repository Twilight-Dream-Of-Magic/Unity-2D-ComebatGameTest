using UnityEngine;

namespace Combat {
    /// <summary>
    /// Immutable payload describing an attack at the moment of contact. Values can be overridden by MoveData
    /// when Hitbox builds the effective info.
    /// </summary>
    public enum HitLevel { High, Mid, Low, Overhead }
    public enum HitType { Strike, Projectile, Throw }

    [System.Serializable]
    public struct DamageInfo {
        [Header("Base Damage & Stun (seconds)")]
        public int damage;
        public float hitstun;
        public float blockstun;

        [Header("Block & Priority")]
        public bool canBeBlocked;
        public int priority;

        [Header("Knockback & Pushback")]
        public Vector2 knockback;
        public float pushbackOnHit;
        public float pushbackOnBlock;

        [Header("Hit-Stop (seconds)")]
        public float hitstopOnHit;
        public float hitstopOnBlock;

        [Header("Hit Properties")]
        public HitLevel level;
        public HitType type;
    }
}