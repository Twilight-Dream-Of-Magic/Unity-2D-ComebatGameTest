using UnityEngine;

namespace Fighter {
    /// <summary>
    /// Centralized jump rules for a fighter. Encapsulates: max air jumps, coyote time,
    /// input buffer, minimal interval between jumps, and a token bucket limiter for repeated jumps.
    /// Both player and AI follow the same rule.
    /// </summary>
    public class JumpRule : MonoBehaviour {
        [Header("Limits")]
        public int maxAirJumps = 1;

        [Header("Timing")]
        public float coyoteTime = 0.1f;
        public float bufferTime = 0.15f;
        public float minInterval = 0.05f;

        [Header("Token Bucket")]
        public int tokenCapacity = 4;
        public int tokensPerWindow = 2;
        public float tokenWindowSeconds = 3f;

        int airJumpsUsed;
        float timeSinceLeftGround;
        float timeSinceLastJump;
        float bufferTimer = 999f;
        bool jumpHeld;

        float tokens;
        float tokenAccu;

        void Start() {
            var stats = GetComponent<FighterController>()?.stats;
            if (stats != null) {
                maxAirJumps = stats.maxAirJumps;
                coyoteTime = stats.jumpCoyoteTime;
                bufferTime = stats.jumpBufferTime;
                minInterval = stats.minJumpInterval;
                tokenCapacity = Mathf.Max(1, stats.jumpTokenCapacity);
                tokensPerWindow = Mathf.Max(0, stats.jumpTokensPerWindow);
                tokenWindowSeconds = Mathf.Max(0.1f, stats.jumpTokenWindowSeconds);
            }
            tokens = tokenCapacity;
        }

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
            if (jumpPressedThisFrame) { jumpHeld = true; bufferTimer = 0f; }
            else { bufferTimer += Time.deltaTime; }

            // token bucket refill: tokensPerWindow every tokenWindowSeconds
            if (tokensPerWindow > 0 && tokenWindowSeconds > 0.0001f) {
                tokenAccu += (tokensPerWindow / tokenWindowSeconds) * Time.deltaTime;
                if (tokenAccu >= 1f) {
                    int add = Mathf.FloorToInt(tokenAccu);
                    tokenAccu -= add;
                    tokens = Mathf.Min(tokenCapacity, tokens + add);
                }
            }
        }

        public void SetJumpHeld(bool held) { jumpHeld = held; }

        /// <summary>Whether a jump can be performed now, considering limits and timing.</summary>
        public bool CanPerformJump(bool grounded) {
            if (timeSinceLastJump < minInterval) return false;
            if (tokens < 1f) return false;
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
            if (tokenCapacity > 0) tokens = Mathf.Max(0, tokens - 1f);
        }
    }
}