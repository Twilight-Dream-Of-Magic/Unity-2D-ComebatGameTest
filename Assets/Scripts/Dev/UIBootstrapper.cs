using UnityEngine;

namespace Dev {
    public static class UIBootstrapper {
        public static void BuildHUD(FightingGame.Combat.Actors.FighterActor p1, FightingGame.Combat.Actors.FighterActor p2) {
            var canvasGo = new GameObject("Canvas");
            canvasGo.AddComponent<UI.CanvasRoot>();
            var hud = canvasGo.AddComponent<UI.BattleHUD>();
            hud.p1 = p1; hud.p2 = p2;
        }
    }
}