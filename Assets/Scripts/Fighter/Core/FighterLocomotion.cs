using UnityEngine;
using Systems;
using FightingGame.Combat;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates locomotion: ground/air movement, jumping, auto-facing, grounded checks.
    /// Keeps logic out of FighterController while exposing simple operations.
    /// 位移封装：地面/空中移动、跳跃、自动朝向、落地检测；将细节从控制器中剥离，暴露简洁操作。
    /// </summary>
    public class FighterLocomotion : MonoBehaviour {
        public FightingGame.Combat.Actors.FighterActor fighter;
        public new Rigidbody2D rigidbody2D;
        public CapsuleCollider2D bodyCollider;
        public Animator animator;
        public bool enableBodyPushout = false;

        void Awake() {
            if (!fighter) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
            if (!rigidbody2D) rigidbody2D = GetComponent<Rigidbody2D>();
            if (!bodyCollider) bodyCollider = GetComponent<CapsuleCollider2D>();
            if (!animator) animator = GetComponent<Animator>();
        }

        /// <summary>Ground move by input scale. 地面移动。</summary>
        public void Move(float x) {
            rigidbody2D.velocity = new Vector2(x * (fighter.stats != null ? fighter.stats.walkSpeed : 6f), rigidbody2D.velocity.y);
            if (enableBodyPushout) ResolveOverlapPushout();
        }

        /// <summary>Stop horizontal velocity. 停止水平速度。</summary>
        public void HaltHorizontal() {
            rigidbody2D.velocity = new Vector2(0, rigidbody2D.velocity.y);
        }

        /// <summary>Air move by input scale. 空中位移。</summary>
        public void AirMove(float x) {
            rigidbody2D.velocity = new Vector2(x * (fighter.stats != null ? fighter.stats.walkSpeed : 6f), rigidbody2D.velocity.y);
            if (enableBodyPushout) ResolveOverlapPushout();
        }

        /// <summary>Perform jump and optionally trigger animation. 执行起跳并触发动画。</summary>
        public void Jump() {
            rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, fighter.stats != null ? fighter.stats.jumpForce : 12f);
            if (animator && animator.runtimeAnimatorController) animator.SetTrigger("Jump");
        }

        /// <summary>OverlapBox grounded check. 落地检测。</summary>
        public bool IsGrounded(LayerMask groundMask) {
            if (!bodyCollider) return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
            var b = bodyCollider.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.05f);
            Vector2 boxSize = new Vector2(b.size.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
        }

        /// <summary>Auto face opponent along X. 自动朝向对手。</summary>
        public void AutoFaceOpponent() {
            if (!fighter || !fighter.opponent) return;
            bool shouldFaceRight = transform.position.x <= fighter.opponent.position.x;
            if (shouldFaceRight != fighter.facingRight) {
                fighter.facingRight = shouldFaceRight;
                var s = transform.localScale; s.x = Mathf.Abs(s.x) * (fighter.facingRight ? 1 : -1); transform.localScale = s;
            }
        }

        /// <summary>Freeze/unfreeze animator and body. 冻结/解冻动画与刚体。</summary>
        public void ApplyFreezeVisual(bool frozen) {
            if (animator) animator.speed = frozen ? 0f : 1f;
            if (rigidbody2D) rigidbody2D.simulated = !frozen;
        }

        /// <summary>Nudge position by deltaX (FixedUpdate safe). 水平推移。</summary>
        public void NudgeHorizontal(float deltaX) {
            if (Mathf.Abs(deltaX) <= 0.0001f) return;
            var pos = rigidbody2D.position;
            float targetX = pos.x + deltaX;
            rigidbody2D.MovePosition(new Vector2(targetX, pos.y));
        }

        // Simple pushout to avoid interpenetration and wall trap
        void ResolveOverlapPushout() {
            if (!bodyCollider) return;
            var b = bodyCollider.bounds;
            // push from other fighters' body volumes
            var hits = Physics2D.OverlapBoxAll(b.center, b.size * 0.98f, 0f);
            foreach (var h in hits) {
                if (h == null || h.attachedRigidbody == rigidbody2D) continue;
                if (h.GetComponent<BodyVolume>() == null) continue;
                var other = h.bounds;
                if (!b.Intersects(other)) continue;
                float dxLeft = other.max.x - b.min.x;
                float dxRight = b.max.x - other.min.x;
                // choose minimal horizontal separation direction
                float push = Mathf.Abs(dxLeft) < Mathf.Abs(dxRight) ? -dxLeft : dxRight;
                rigidbody2D.position += new Vector2(push * 1.01f, 0f);
            }
            // clamp to simple arena bounds (optional): -10..10
            float x = Mathf.Clamp(rigidbody2D.position.x, -10f, 10f);
            rigidbody2D.position = new Vector2(x, rigidbody2D.position.y);
        }
    }
}