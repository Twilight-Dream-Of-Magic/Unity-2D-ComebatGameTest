using UnityEngine;
using FightingGame.Combat;

namespace Data {
    public enum SpecialKind { Damage, Heal }

    [CreateAssetMenu(menuName = "Fighter/Special Move Set")]
    public class SpecialMoveSet : ScriptableObject {
        [System.Serializable]
        public class SpecialEntry {
            public string name;
            public CommandToken[] sequence;
            public float maxWindowSeconds = 0.6f;
            public string triggerName; // e.g., "Super", "Heal"
            public SpecialKind kind = SpecialKind.Damage;
        }
        public SpecialEntry[] specials;
    }
}