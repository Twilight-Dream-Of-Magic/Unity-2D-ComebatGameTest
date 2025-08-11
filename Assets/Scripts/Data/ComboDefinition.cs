using UnityEngine;
using Combat;

namespace Data {
    [CreateAssetMenu(menuName = "Fighter/Combo Definition")]
    public class ComboDefinition : ScriptableObject {
        public string comboName = "LLH";
        public CommandToken[] sequence;
        public string attackTrigger = "Heavy"; // what to trigger when matched
        public float cancelWindowStart = 0.05f; // seconds from attack start
        public float cancelWindowEnd = 0.25f;   // when cancel is allowed
    }
}