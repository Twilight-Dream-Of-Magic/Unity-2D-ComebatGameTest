using UnityEngine;

namespace Combat {
    public enum HitLevel { High, Mid, Low, Overhead }
    public enum HitType { Strike, Throw, Projectile }

    [System.Serializable]
    public struct DamageInfo {
        public int damage;
        public float hitstun;
        public float blockstun;
        public Vector2 knockback;
        public bool canBeBlocked;
        public bool causesChipOnBlock;
        public HitLevel level;
        public HitType type;
        public int priority;
        public float hitstopOnHit;
        public float hitstopOnBlock;
        public float pushbackOnHit;
        public float pushbackOnBlock;
    }
}