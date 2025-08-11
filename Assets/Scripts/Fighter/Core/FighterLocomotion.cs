using UnityEngine;
using Systems;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates locomotion: ground/air movement, jumping, auto-facing, grounded checks.
    /// Keeps logic out of FighterController while exposing simple operations.
    /// </summary>
    public class FighterLocomotion : MonoBehaviour {
        public FighterController fighter;
        public Rigidbody2D rb;
        public CapsuleCollider2D bodyCollider;
        public Animator animator;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!rb) rb = GetComponent<Rigidbody2D>();
            if (!bodyCollider) bodyCollider = GetComponent<CapsuleCollider2D>();
            if (!animator) animator = GetComponent<Animator>();
        }

        public void Move(float x) {
            rb.velocity = new Vector2(x * (fighter.stats != null ? fighter.stats.walkSpeed : 6f), rb.velocity.y);
            ResolveOverlapPushout();
        }

        public void HaltHorizontal() {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        public void AirMove(float x) {
            rb.velocity = new Vector2(x * (fighter.stats != null ? fighter.stats.walkSpeed : 6f), rb.velocity.y);
            ResolveOverlapPushout();
        }

        public void Jump() {
            rb.velocity = new Vector2(rb.velocity.x, fighter.stats != null ? fighter.stats.jumpForce : 12f);
            if (animator && animator.runtimeAnimatorController) animator.SetTrigger("Jump");
        }

        public bool IsGrounded(LayerMask groundMask) {
            if (!bodyCollider) return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
            var b = bodyCollider.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
        }

        public void AutoFaceOpponent() {
            if (!fighter || !fighter.opponent) return;
            bool shouldFaceRight = transform.position.x <= fighter.opponent.position.x;
            if (shouldFaceRight != fighter.facingRight) {
                fighter.facingRight = shouldFaceRight;
                var s = transform.localScale; s.x = Mathf.Abs(s.x) * (fighter.facingRight ? 1 : -1); transform.localScale = s;
            }
        }

        public void ApplyFreezeVisual(bool frozen) {
            if (animator) animator.speed = frozen ? 0f : 1f;
            if (rb) rb.simulated = !frozen;
        }

        public void NudgeHorizontal(float deltaX) {
            if (Mathf.Abs(deltaX) <= 0.0001f) return;
            var pos = rb.position;
            float targetX = pos.x + deltaX;
            rb.MovePosition(new Vector2(targetX, pos.y));
        }

        // Simple pushout to avoid interpenetration and wall trap
        void ResolveOverlapPushout() {
            if (!bodyCollider) return;
            var b = bodyCollider.bounds;
            // push from other fighters' pushboxes
            var hits = Physics2D.OverlapBoxAll(b.center, b.size * 0.98f, 0f);
            foreach (var h in hits) {
                if (h == null || h.attachedRigidbody == rb) continue;
                if (h.GetComponent<Combat.Pushbox>() == null) continue;
                var other = h.bounds;
                if (!b.Intersects(other)) continue;
                float dxLeft = other.max.x - b.min.x;
                float dxRight = b.max.x - other.min.x;
                // choose minimal horizontal separation direction
                float push = Mathf.Abs(dxLeft) < Mathf.Abs(dxRight) ? -dxLeft : dxRight;
                rb.position += new Vector2(push * 1.01f, 0f);
            }
            // clamp to simple arena bounds (optional): -10..10
            float x = Mathf.Clamp(rb.position.x, -10f, 10f);
            rb.position = new Vector2(x, rb.position.y);
        }
    }
}