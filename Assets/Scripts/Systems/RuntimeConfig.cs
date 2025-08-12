using UnityEngine;
using Framework;

namespace Systems {
    public enum UIMode { Release, Debug }

    public class RuntimeConfig : MonoSingleton<RuntimeConfig> {
        [Header("UI Mode")]
        public UIMode uiMode = UIMode.Debug;
        [Header("Debug Detail Toggles")]
        public bool showStateTexts = true;
        public bool showNumericBars = true; // HP/Meter numbers
        public bool showDebugHUD = true;

        public System.Action OnConfigChanged;

        protected override void OnSingletonInit() { }

        public void SetUIMode(UIMode mode) { if (uiMode != mode) { uiMode = mode; OnConfigChanged?.Invoke(); } }
        public void SetShowStateTexts(bool v) { if (showStateTexts != v) { showStateTexts = v; OnConfigChanged?.Invoke(); } }
        public void SetShowNumericBars(bool v) { if (showNumericBars != v) { showNumericBars = v; OnConfigChanged?.Invoke(); } }
        public void SetShowDebugHUD(bool v) { if (showDebugHUD != v) { showDebugHUD = v; OnConfigChanged?.Invoke(); } }
    }
}