using UnityEngine;
using Combat;

namespace Data {
    [CreateAssetMenu(menuName = "Fighter/Special Move Set")]
    public class SpecialMoveSet : ScriptableObject {
        [System.Serializable]
        public class SpecialEntry {
            public string name;
            public CommandToken[] sequence;
            public float maxWindowSeconds = 0.6f;
            public string triggerName; // e.g., "Super", "Heal"
        }
        public SpecialEntry[] specials;
    }
}