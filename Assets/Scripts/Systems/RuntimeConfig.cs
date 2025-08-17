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
        public bool specialsEnabled = true; // allow disabling specials to validate core loop
        [Header("Player Safeguards / Perks")]
        public bool playerLowHPGuardEnabled = true;
        public int playerLowHPThreshold = 750; // 15% of 5000 as default
        public float playerHealCooldown = 3.0f; // seconds between heals
        public KeyCode playerDodgeKey = KeyCode.LeftShift; // default dodge key (more ergonomic)
        public KeyCode playerBlockKey = KeyCode.L; // default block key

		public KeyCode playerHealKey = KeyCode.H; // optional heal key
		public KeyCode playerSuperKey = KeyCode.I; // optional super trigger
		public bool healInputEnabled = false; // gate super input; default OFF
		public bool superInputEnabled = false; // gate super input; default OFF

        [Header("Block Timing")]
        public float blockMaxHoldSeconds = 0.8f; // 玩家持續按住格擋可生效的最長時間
        public float blockCooldownSeconds = 0.35f; // 超時後的冷卻時間，期間無法格擋

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

        public void SetPlayerLowHP(int threshold) { playerLowHPThreshold = threshold; OnConfigChanged?.Invoke(); }
        public void SetPlayerHealCooldown(float cd) { playerHealCooldown = cd; OnConfigChanged?.Invoke(); }
    }
}