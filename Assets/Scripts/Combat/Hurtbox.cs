using UnityEngine;

namespace FightingGame.Combat {
    /// <summary>
    /// A trigger collider region that can receive hits. Split into regions (Head/Torso/Legs) so the
    /// fighter can toggle invulnerability per region each frame. Owned by a FighterController.
    /// Gizmos render for quick white-box debugging.
    /// </summary>
    public enum HurtRegion { Head, Torso, Legs }

    [RequireComponent(typeof(BoxCollider2D))]
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
        public Vector2 desiredSize = Vector2.zero;
        public Vector2 desiredOffset = Vector2.zero;
        public bool ensureTrigger = true;

        private BoxCollider2D _box;

        private void CacheBox() {
            if (_box == null) {
                _box = GetComponent<BoxCollider2D>();
                if (_box == null) {
                    _box = gameObject.AddComponent<BoxCollider2D>();
                }
            }
        }

        /// <summary>
        /// 由外部呼叫設定尺寸/偏移，避免透過生命周期覆蓋。
        /// </summary>
        public void ConfigureCollider(Vector2 size, Vector2 offset, bool isTrigger = true) {
            desiredSize = size;
            desiredOffset = offset;
            CacheBox();
            ApplyColliderSizing(isTrigger);
        }

        private void ApplyColliderSizing(bool isTrigger) {
            if (_box == null) {
                return;
            }
            if (ensureTrigger && isTrigger) {
                _box.isTrigger = true;
            }
            if (desiredSize.sqrMagnitude > 0f) {
                _box.size = desiredSize;
            }
            _box.offset = desiredOffset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            CacheBox();
            if (_box == null) return;
            var bounds = _box.bounds;
            Color fillColor = enabledThisFrame ? new Color(0f, 1f, 1f, 0.15f) : new Color(0f, 1f, 1f, 0.04f);
            Gizmos.color = fillColor;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = enabledThisFrame ? Color.cyan : new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
        
    }
}