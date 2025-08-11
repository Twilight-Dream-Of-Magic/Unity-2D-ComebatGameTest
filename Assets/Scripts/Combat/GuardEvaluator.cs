namespace Combat {
    public static class GuardEvaluator {
        /// <summary>
        /// KOF-like guard rules:
        /// - Must be on ground and holding block
        /// - High/Overhead: must be standing (not crouching)
        /// - Low: must be crouching
        /// - Mid: both posture ok
        /// </summary>
        public static bool CanBlock(bool isHoldingBlock, bool isGrounded, bool isCrouching, HitLevel level) {
            if (!isHoldingBlock || !isGrounded) return false;
            switch (level) {
                case HitLevel.High: return !isCrouching;
                case HitLevel.Mid: return true;
                case HitLevel.Low: return isCrouching;
                case HitLevel.Overhead: return !isCrouching;
                default: return true;
            }
        }
    }
}