using UnityEngine;
using Data;
using Systems;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates attack lifecycle: selecting move data by trigger, metering, heal on use,
    /// toggling hitboxes on active frames, and applying hit-stop feedback.
    /// </summary>
    public class AttackExecutor : MonoBehaviour {
        public FighterController fighter;
        public Animator animator;
        public Combat.Hitbox[] hitboxes;

        bool hitStopApplied;
        Vector3[] originalLocalPos;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!animator) animator = GetComponent<Animator>();
            if (hitboxes == null || hitboxes.Length == 0) hitboxes = GetComponentsInChildren<Combat.Hitbox>(true);
            CacheOriginals();
        }

        void CacheOriginals() {
            if (hitboxes == null) return;
            originalLocalPos = new Vector3[hitboxes.Length];
            for (int i = 0; i < hitboxes.Length; i++) originalLocalPos[i] = hitboxes[i].transform.localPosition;
        }

        public void TriggerAttack(string trigger) {
            fighter.SetDebugMoveName(trigger);
            if (fighter.moveSet != null) fighter.SetCurrentMove(fighter.moveSet.Get(trigger));
            var move = fighter.CurrentMove;
            if (move != null && move.meterCost > 0 && !fighter.ConsumeMeter(move.meterCost)) { fighter.SetCurrentMove(null); return; }
            if (move != null && move.healAmount > 0) fighter.AddHealth(move.healAmount);
            if (animator && animator.runtimeAnimatorController) animator.SetTrigger(trigger);
        }

        public void SetAttackActive(bool on) {
            fighter.SetDebugHitActive(on);
            if (on) { fighter.IncrementAttackInstanceId(); fighter.ClearHitVictimsSet(); hitStopApplied = false; }
            if (hitboxes == null) return;

            if (on) MaybeOffsetAerialHeavy(); else RestoreHitboxPositions();

            foreach (var h in hitboxes) if (h != null) h.SetActive(on);
            fighter.SetActiveColor(on);
        }

        void MaybeOffsetAerialHeavy() {
            if (fighter.IsGrounded()) return;
            var move = fighter.CurrentMove; if (move == null) return;
            if (move.triggerName != "Heavy") return;
            float forward = fighter.facingRight ? 1f : -1f;
            for (int i = 0; i < hitboxes.Length; i++) {
                var t = hitboxes[i].transform;
                t.localPosition = originalLocalPos != null && i < originalLocalPos.Length ? originalLocalPos[i] + new Vector3(0.45f * forward, -0.25f, 0f) : t.localPosition + new Vector3(0.45f * forward, -0.25f, 0f);
            }
        }

        void RestoreHitboxPositions() {
            if (hitboxes == null || originalLocalPos == null) return;
            for (int i = 0; i < hitboxes.Length && i < originalLocalPos.Length; i++) hitboxes[i].transform.localPosition = originalLocalPos[i];
        }

        public void OnHitConfirmedLocal(float seconds) {
            if (hitStopApplied) return; hitStopApplied = true;
            int frames = FrameClock.SecondsToFrames(seconds);
            fighter.FreezeFrames(frames);
            Systems.CameraShaker.Instance?.Shake(0.12f, seconds);
        }
    }
}