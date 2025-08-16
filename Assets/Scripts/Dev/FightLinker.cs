using UnityEngine;
using FightingGame.Combat.Actors;

namespace Dev {
    public static class FightLinker {
        public static void LinkOpponents(FighterActor p1, FighterActor p2, Vector2 arenaHalfExtents) {
            if (!p1 || !p2) return;
            p1.opponent = p2.transform; p2.opponent = p1.transform;
            var cameraFramer = Camera.main ? Camera.main.GetComponent<Systems.CameraFramer>() : null;
            if (cameraFramer) { cameraFramer.targetA = p1.transform; cameraFramer.targetB = p2.transform; cameraFramer.arenaHalfExtents = arenaHalfExtents; }
            if (p1.bodyCollider) p1.bodyCollider.isTrigger = false;
            if (p2.bodyCollider) p2.bodyCollider.isTrigger = false;
            var all1 = p1.GetComponentsInChildren<Collider2D>(true);
            var all2 = p2.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < all1.Length; i++) {
                if (all1[i] == null || all1[i].isTrigger) continue;
                for (int j = 0; j < all2.Length; j++) {
                    if (all2[j] == null || all2[j].isTrigger) continue;
                    Physics2D.IgnoreCollision(all1[i], all2[j], true);
                }
            }
            foreach (var hb in p1.GetComponentsInChildren<FightingGame.Combat.Hurtbox>(true)) { hb.owner = p1; hb.activeStanding = true; hb.activeCrouching = hb.region != FightingGame.Combat.HurtRegion.Head; hb.activeAirborne = hb.region != FightingGame.Combat.HurtRegion.Legs; }
            foreach (var hb in p2.GetComponentsInChildren<FightingGame.Combat.Hurtbox>(true)) { hb.owner = p2; hb.activeStanding = true; hb.activeCrouching = hb.region != FightingGame.Combat.HurtRegion.Head; hb.activeAirborne = hb.region != FightingGame.Combat.HurtRegion.Legs; }
        }
    }
}