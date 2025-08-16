using UnityEngine;
using Framework;

namespace Systems {
    /// <summary>UI display mode. UI 显示模式。</summary>
    public enum UIMode { Release, Debug }

    /// <summary>
    /// Central runtime configuration toggles for UI debug/visibility.
    /// 运行时配置中心：控制 UI 调试/可见性开关。
    /// </summary>
    public class RuntimeConfig : MonoSingleton<RuntimeConfig> {
        [Header("UI Mode")]
        public UIMode uiMode = UIMode.Debug;
        [Header("Debug Detail Toggles")]
        public bool showStateTexts = true;
        public bool showNumericBars = true; // HP/Meter numbers
        public bool showDebugHUD = true;
        [Header("Gameplay Toggles")]
        public bool specialsEnabled = false; // allow disabling specials to validate core loop

        /// <summary>Raised when any config changes. 任意配置改变时触发。</summary>
        public System.Action OnConfigChanged;

        protected override void DoAwake() { }

        /// <summary>Set UI mode and notify. 设定 UI 模式并广播。</summary>
        public void SetUIMode(UIMode mode) { if (uiMode != mode) { uiMode = mode; OnConfigChanged?.Invoke(); } }
        /// <summary>Toggle state texts and notify. 切换状态文本显示并广播。</summary>
        public void SetShowStateTexts(bool v) { if (showStateTexts != v) { showStateTexts = v; OnConfigChanged?.Invoke(); } }
        /// <summary>Toggle numeric bars and notify. 切换数值条显示并广播。</summary>
        public void SetShowNumericBars(bool v) { if (showNumericBars != v) { showNumericBars = v; OnConfigChanged?.Invoke(); } }
        /// <summary>Toggle debug HUD and notify. 切换调试 HUD 并广播。</summary>
        public void SetShowDebugHUD(bool v) { if (showDebugHUD != v) { showDebugHUD = v; OnConfigChanged?.Invoke(); } }
        /// <summary>Toggle specials and notify. 切换搓招开关并广播。</summary>
        public void SetSpecialsEnabled(bool v) { if (specialsEnabled != v) { specialsEnabled = v; OnConfigChanged?.Invoke(); } }
    }
}