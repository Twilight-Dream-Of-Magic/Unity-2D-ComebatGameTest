using UnityEngine;

namespace Dev {
    /// <summary>
    /// One-click demo runner: when placed in an empty scene, it creates a full battle using BattleAutoSetup
    /// and enables scripted demo mode to showcase movement/attacks/AI/round end without manual input.
    /// </summary>
    public class DemoAutoRunner : MonoBehaviour {
        public bool scriptedDemo = true;
        public bool createManagers = true;
        public bool createHUD = true;
        public bool createGround = true;

        void Start() {
            var go = new GameObject("AutoBattle");
            var setup = go.AddComponent<BattleAutoSetup>();
            setup.createManagers = createManagers;
            setup.createUI = createHUD;
            setup.createGround = createGround;
            setup.demoScripted = scriptedDemo;
        }
    }
}