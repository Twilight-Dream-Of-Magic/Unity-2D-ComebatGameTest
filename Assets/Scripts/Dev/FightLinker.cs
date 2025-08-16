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
            // create world-space state billboards with binder (handles subscribe/unsubscribe)
            CreateWorldBillboardFor(p1, "P1", new Color(0.9f, 0.95f, 1f, 1f));
            CreateWorldBillboardFor(p2, "AI", new Color(1f, 0.85f, 0.85f, 1f));
        }
        static void CreateWorldBillboardFor(FighterActor f, string prefix, Color color) {
            if (!f) return;
            var anchorComp = f.GetComponentInChildren<Fighter.Core.HeadStateAnchor>();
            var anchor = anchorComp != null ? anchorComp.transform : f.transform;
            var go = new GameObject(prefix + "StateWS");
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = Vector3.zero;
            var billboard = go.AddComponent<UI.HUD.WorldStateBillboard>();
            billboard.fighter = f;
            billboard.anchor = anchor;
            billboard.SetText(prefix + ": --");
            billboard.SetColor(color);
            var binder = go.AddComponent<UI.HUD.WorldStateBillboardBinder>();
            binder.fighter = f;
            binder.billboard = billboard;
            binder.labelPrefix = prefix;
            binder.labelColor = color;
        }
    }
}