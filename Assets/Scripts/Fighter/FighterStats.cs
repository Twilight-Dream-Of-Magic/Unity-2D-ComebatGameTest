using UnityEngine;

namespace Fighter {
    [CreateAssetMenu(menuName = "Fighter/Stats")]
    public class FighterStats : ScriptableObject {
        public int maxHealth = 100;
        public float walkSpeed = 6f;
        public float jumpForce = 12f;
        public float gravityScale = 4f;
        public float blockDamageRatio = 0.2f;
        public float dodgeDuration = 0.25f;
        public float dodgeInvuln = 0.2f;
        public float hitStop = 0.06f;
    }
}