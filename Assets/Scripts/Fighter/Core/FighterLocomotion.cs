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
        }

        public void HaltHorizontal() {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        public void AirMove(float x) {
            rb.velocity = new Vector2(x * (fighter.stats != null ? fighter.stats.walkSpeed : 6f), rb.velocity.y);
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
    }
}