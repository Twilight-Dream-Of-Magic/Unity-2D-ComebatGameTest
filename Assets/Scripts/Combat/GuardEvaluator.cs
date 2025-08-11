namespace Combat {
    public static class GuardEvaluator {
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