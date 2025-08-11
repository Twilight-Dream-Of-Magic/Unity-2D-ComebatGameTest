using UnityEngine;
using Combat;

namespace Data {
    [CreateAssetMenu(menuName = "Fighter/Move Data")]
    public class MoveData : ScriptableObject {
        public string moveId;
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
        public int meterCost = 0; // cost to use this move
        public int healAmount = 0; // heal on use (applied on trigger)

        [Header("Knockback & Pushback")]
        public Vector2 knockback = new Vector2(2f, 2f);
        public float pushbackOnHit = 0.4f;
        public float pushbackOnBlock = 0.6f;

        [Header("Hit Properties")]
        public HitLevel hitLevel = HitLevel.Mid;
        public HitType hitType = HitType.Strike;
        public int priority = 1;
        public bool canBeBlocked = true;

        [Header("Hit-Stop (seconds)")]
        public float hitstopOnHit = 0.1f;   // 6f
        public float hitstopOnBlock = 0.066f; // 4f

        [Header("Cancel Window (seconds from start)")]
        public float cancelWindowStart = 0.0f;
        public float cancelWindowEnd = 0.1f;
    }
}