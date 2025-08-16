#if UNITY_EDITOR
using UnityEngine;

namespace Fighter.EditorTools {
    public class HitboxQuickToggle : MonoBehaviour {
        public FightingGame.Combat.Actors.FighterActor fighter;
        public bool activateAll;
        public KeyCode toggleKey = KeyCode.H;

        private void Reset() {
            if (fighter == null) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
        }

        private void Update() {
            if (Input.GetKeyDown(toggleKey)) {
                if (fighter == null || fighter.hitboxes == null) return;
                activateAll = !activateAll;
                foreach (var h in fighter.hitboxes) if (h != null) h.SetActive(activateAll);
            }
        }
    }
}
#endif