using UnityEngine;

namespace FightingGame.Combat {
    /// <summary>
    /// Immutable payload describing an attack at the moment of contact. Values can be overridden by current action data.
    /// </summary>
    public enum HitLevel { High, Mid, Low, Overhead }
    public enum HitType { Strike, Projectile, Throw }
    public enum KnockdownKind { None, Soft, Hard }

    [System.Serializable]
    public struct DamageInfo {
        public int damage;
        public HitLevel level;
        public float hitstun;
        public float blockstun;
        public Vector2 knockback;
        public bool canBeBlocked;
        public float hitstopOnHit;
        public float hitstopOnBlock;
        public float pushbackOnHit;
        public float pushbackOnBlock;
        public KnockdownKind knockdownKind;
        public int meterOnHit;
        public int meterOnBlock;
        public bool isSuper; // super moves bypass dodge-invuln
    }
}