using UnityEngine;

namespace Fighter.AI {
    [CreateAssetMenu(menuName = "Fighter/AI Config")]
    public class AIConfig : ScriptableObject {
        [Range(0f,1f)] public float blockProbability = 0.2f;
        public Vector2 attackCooldownRange = new Vector2(0.6f, 1.2f);
        public float approachDistance = 2.2f;
        public float retreatDistance = 1.0f;
    }
}