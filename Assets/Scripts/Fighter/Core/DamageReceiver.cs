using UnityEngine;
using Combat;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates damage intake: applies invulnerability filters, resolves hit vs block,
    /// updates HP/meter, triggers hitstun/KO, and applies knockback/pushback.
    /// </summary>
    public class DamageReceiver : MonoBehaviour {
        public FighterController fighter;
        public Animator animator;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!animator) animator = GetComponent<Animator>();
        }

        public void TakeHit(DamageInfo info, FighterController attacker) {
            if ((info.level == HitLevel.High || info.level == HitLevel.Overhead) && fighter.UpperBodyInvuln) return;
            if (info.level == HitLevel.Low && fighter.LowerBodyInvuln) return;

            var res = HitResolver.Resolve(info, fighter.CanBlock(info), fighter.stats != null ? fighter.stats.blockDamageRatio : 0.2f);
            int maxHp = fighter.stats != null ? fighter.stats.maxHealth : 100;
            int before = fighter.currentHealth;
            fighter.currentHealth = Mathf.Clamp(fighter.currentHealth - res.finalDamage, 0, maxHp);

            if (fighter.currentHealth == 0) { fighter.StateMachine.SetState(fighter.KO); if (AnimatorReady()) animator.SetTrigger("KO"); return; }

            float dir = Mathf.Sign(transform.position.x - attacker.transform.position.x);
            if (!res.wasBlocked) {
                var rb = fighter.rb; rb.velocity = new Vector2(dir * info.knockback.x, info.knockback.y);
                if (AnimatorReady()) animator.SetTrigger("Hit");
                fighter.Hitstun.Begin(res.appliedStun);
                fighter.StateMachine.SetState(fighter.Hitstun);

                // attacker feedback
                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnHit : 20);
                attacker.AddExternalImpulse(-dir * res.appliedPushback);
                // victim feedback
                Systems.CameraShaker.Instance?.Shake(0.1f, res.appliedHitstop);

                if (fighter.currentHealth < before) { fighter.RaiseDamaged(attacker); }
            } else {
                if (AnimatorReady()) animator.SetTrigger("BlockHit");
                fighter.Hitstun.Begin(res.appliedStun);
                fighter.StateMachine.SetState(fighter.Hitstun);

                // attacker feedback
                attacker.OnHitConfirmedLocal(res.appliedHitstop);
                attacker.AddMeter(attacker.CurrentMove ? attacker.CurrentMove.meterOnBlock : 10);
                attacker.AddExternalImpulse(-dir * res.appliedPushback);
                // victim blocked feedback (smaller shake)
                Systems.CameraShaker.Instance?.Shake(0.06f, res.appliedHitstop);
            }
        }

        bool AnimatorReady() => animator && animator.runtimeAnimatorController;
    }
}