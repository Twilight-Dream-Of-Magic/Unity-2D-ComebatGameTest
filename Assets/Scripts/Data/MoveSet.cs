using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Fighter/Move Set")]
    public class MoveSet : ScriptableObject {
        [System.Serializable]
        public struct Entry { public string triggerName; public MoveData move; }
        public Entry[] entries;

        public MoveData Get(string trigger) {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++) if (entries[i].triggerName == trigger) return entries[i].move;
            return null;
        }
    }
}