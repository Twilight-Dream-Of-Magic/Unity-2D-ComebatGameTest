using UnityEngine;

namespace Combat {
    /// <summary>
    /// A trigger collider region that can receive hits. Split into regions (Head/Torso/Legs) so the
    /// fighter can toggle invulnerability per region each frame. Owned by a FighterController.
    /// Gizmos render for quick white-box debugging.
    /// </summary>
    public enum HurtRegion { Head, Torso, Legs }

    public class Hurtbox : MonoBehaviour {
        /// <summary>Owning fighter. Set by setup code so hit detection can route damage correctly.</summary>
        public Fighter.FighterController owner;
        /// <summary>Semantic body region used by guard/invulnerability logic.</summary>
        public HurtRegion region = HurtRegion.Torso;
        /// <summary>Latched by FighterController each frame to enable/disable this region for hit testing.</summary>
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