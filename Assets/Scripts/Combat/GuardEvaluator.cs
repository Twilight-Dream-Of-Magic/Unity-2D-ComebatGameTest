namespace FightingGame.Combat {
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

        /// <summary>
        /// Extended with timing window: requires not locked and within max hold.
        /// </summary>
        public static bool CanBlockTimed(FightingGame.Combat.Actors.FighterActor fighter, bool isHoldingBlock, bool isGrounded, bool isCrouching, HitLevel level, float maxHoldSeconds) {
            if (fighter != null && fighter.IsBlockLocked()) return false;
            if (maxHoldSeconds > 0f) {
                if (fighter == null) return false;
                if (fighter.GetBlockHeldSeconds() > maxHoldSeconds) return false;
            }
            return CanBlock(isHoldingBlock, isGrounded, isCrouching, level);
        }
    }
}