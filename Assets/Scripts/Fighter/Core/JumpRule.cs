using UnityEngine;

namespace Fighter {
    /// <summary>
    /// Centralized jump rules for a fighter. Encapsulates: max air jumps, coyote time,
    /// input buffer, and minimal interval between jumps. Both player and AI follow the same rule.
    /// </summary>
    public class JumpRule : MonoBehaviour {
        [Header("Limits")]
        public int maxAirJumps = 1;

        [Header("Timing")]
        public float coyoteTime = 0.1f;
        public float bufferTime = 0.15f;
        public float minInterval = 0.05f;

        int airJumpsUsed;
        float timeSinceLeftGround;
        float timeSinceLastJump;
        float bufferTimer = 999f;
        bool jumpHeld;
        bool jumpPressedEdge;

        /// <summary>Call every frame to feed grounded state and jump pressed edge.</summary>
        public void Tick(bool grounded, bool jumpPressedThisFrame) {
            if (grounded) {
                timeSinceLeftGround = 0f;
                airJumpsUsed = 0;
            } else {
                timeSinceLeftGround += Time.deltaTime;
            }

            timeSinceLastJump += Time.deltaTime;

            // detect edge and held
            if (jumpPressedThisFrame) { jumpPressedEdge = true; jumpHeld = true; bufferTimer = 0f; }
            else { bufferTimer += Time.deltaTime; }
        }

        public void SetJumpHeld(bool held) { jumpHeld = held; }

        /// <summary>Whether a jump can be performed now, considering limits and timing.</summary>
        public bool CanPerformJump(bool grounded) {
            if (timeSinceLastJump < minInterval) return false;
            if (grounded) return true;
            if (timeSinceLeftGround <= coyoteTime) return true;
            return airJumpsUsed < maxAirJumps;
        }

        /// <summary>Whether buffered input should auto-consume now to perform a jump.</summary>
        public bool ShouldConsumeBufferedJump(bool grounded) {
            // consume if pressed-edge within buffer, or if holding jump and now eligible (quality of life)
            bool buffered = bufferTimer <= bufferTime;
            bool held = jumpHeld;
            return (buffered || held) && CanPerformJump(grounded);
        }

        /// <summary>Notify the rule that a jump has been executed.</summary>
        public void NotifyJumpExecuted(bool grounded) {
            if (!grounded && timeSinceLeftGround > 0f) airJumpsUsed++;
            timeSinceLastJump = 0f;
            bufferTimer = 999f; // clear buffer
            jumpPressedEdge = false;
        }
    }
}