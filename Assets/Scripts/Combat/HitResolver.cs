using UnityEngine;

namespace Combat {
    public static class HitResolver {
        public struct Result {
            public int finalDamage;
            public float appliedStun;
            public float appliedHitstop;
            public float appliedPushback;
            public bool wasBlocked;
        }

        public static Result Resolve(in DamageInfo info, bool canBlock, float chipRatio) {
            Result r = new Result();
            r.wasBlocked = info.canBeBlocked && canBlock;
            r.finalDamage = r.wasBlocked ? Mathf.CeilToInt(info.damage * chipRatio) : info.damage;
            r.appliedStun = r.wasBlocked ? info.blockstun : info.hitstun;
            r.appliedHitstop = r.wasBlocked ? info.hitstopOnBlock : info.hitstopOnHit;
            r.appliedPushback = r.wasBlocked ? info.pushbackOnBlock : info.pushbackOnHit;
            return r;
        }
    }
}