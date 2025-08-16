using UnityEngine;

namespace Data {
    /// <summary>
    /// A collection mapping input triggers to combat action definitions.
    /// 將輸入觸發詞映射到對應的戰鬥動作定義的集合。
    /// </summary>
    [CreateAssetMenu(menuName = "Fighter/Action Set")]
    public class CombatActionSet : ScriptableObject {
        [System.Serializable]
        public struct Entry { public string triggerName; public CombatActionDefinition actionDefinition; }
        public Entry[] entries;

        public CombatActionDefinition Get(string trigger) {
            if (entries == null)
            {
                return null;
            }
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].triggerName == trigger)
                {
                    return entries[i].actionDefinition;
                }
            }
            return null;
        }
    }
}