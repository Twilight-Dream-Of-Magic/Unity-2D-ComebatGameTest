using UnityEngine;

namespace Combat {
    public enum HurtRegion { Head, Torso, Legs }

    public class Hurtbox : MonoBehaviour {
        public Fighter.FighterController owner;
        public HurtRegion region = HurtRegion.Torso;
        public bool enabledThisFrame = true;

        private void Reset() {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnDrawGizmos() {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            var b = col.bounds;
            Color c = enabledThisFrame ? new Color(0f, 1f, 1f, 0.15f) : new Color(0f, 1f, 1f, 0.04f);
            Gizmos.color = c;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = enabledThisFrame ? Color.cyan : new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}