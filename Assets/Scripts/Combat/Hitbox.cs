using UnityEngine;

namespace Combat {
    [RequireComponent(typeof(Collider2D))]
    public class Hitbox : MonoBehaviour {
        public Fighter.FighterController owner;
        public DamageInfo damageInfo;
        public bool active;

        private void Reset() {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (!active) return;
            var hb = other.GetComponent<Hurtbox>();
            if (hb == null) return;
            if (hb.owner == null || hb.owner == owner) return;
            if (!hb.enabledThisFrame) return;

            DamageInfo info = BuildEffectiveDamageInfo();
            hb.owner.TakeHit(info, owner);
        }

        DamageInfo BuildEffectiveDamageInfo() {
            var info = damageInfo;
            var md = owner != null ? owner.CurrentMove : null;
            if (md != null) {
                info.damage = md.damage;
                info.hitstun = md.hitstun;
                info.blockstun = md.blockstun;
                info.knockback = md.knockback;
                info.canBeBlocked = md.canBeBlocked;
                info.level = md.hitLevel;
                info.type = md.hitType;
                info.priority = md.priority;
                info.hitstopOnHit = md.hitstopOnHit;
                info.hitstopOnBlock = md.hitstopOnBlock;
                info.pushbackOnHit = md.pushbackOnHit;
                info.pushbackOnBlock = md.pushbackOnBlock;
            }
            return info;
        }

        public void SetActive(bool value) { active = value; }

        private void OnDrawGizmos() {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            var b = col.bounds;
            Color c = active ? new Color(1f, 1f, 1f, 0.25f) : new Color(1f, 1f, 1f, 0.08f);
            Gizmos.color = c;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}