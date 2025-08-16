using UnityEngine;

namespace FightingGame.Combat {
    /// <summary>
    /// A trigger collider region that can receive hits. Split into regions (Head/Torso/Legs) so the
    /// fighter can toggle invulnerability per region each frame. Owned by a FighterController.
    /// Gizmos render for quick white-box debugging.
    /// </summary>
    public enum HurtRegion { Head, Torso, Legs }

    public class Hurtbox : MonoBehaviour {
        /// <summary>Owning fighter. Set by setup code so hit detection can route damage correctly.</summary>
        public FightingGame.Combat.Actors.FighterActor owner;
        /// <summary>Semantic body region used by guard/invulnerability logic.</summary>
        public HurtRegion region = HurtRegion.Torso;
        /// <summary>Latched by FighterController each frame to enable/disable this region for hit testing.</summary>
        public bool enabledThisFrame = true;

        [Header("Posture Activation")]
        public bool activeStanding = true;
        public bool activeCrouching = true;
        public bool activeAirborne = true;

        [Header("Collider Sizing (Hurtbox 管理)")]
        public Vector2 desiredSize = new Vector2(0.7f, 1.0f);
        public Vector2 desiredOffset = Vector2.zero;
        public bool ensureTrigger = true;

        private void Awake() {
            ApplyColliderSizing();
        }

        private void OnValidate() {
            ApplyColliderSizing();
        }

        private void Reset() {
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.isTrigger = true;
        }

        void ApplyColliderSizing() {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) {
                box = gameObject.AddComponent<BoxCollider2D>();
            }
            if (ensureTrigger) {
                box.isTrigger = true;
            }
            box.size = desiredSize;
            box.offset = desiredOffset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            var collider = GetComponent<Collider2D>();
            if (collider == null) return;
            var bounds = collider.bounds;
            Color fillColor = enabledThisFrame ? new Color(0f, 1f, 1f, 0.15f) : new Color(0f, 1f, 1f, 0.04f);
            Gizmos.color = fillColor;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = enabledThisFrame ? Color.cyan : new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
        
    }
}