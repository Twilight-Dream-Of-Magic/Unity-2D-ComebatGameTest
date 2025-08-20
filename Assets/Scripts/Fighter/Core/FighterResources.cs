using UnityEngine;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates resource operations: HP and meter add/consume, and invulnerability toggles.
    /// FighterController will delegate to this to keep responsibilities isolated.
    /// 资源管理：统一处理气槽与生命的增减，以及上/下半身无敌切换；控制器通过该组件修改资源，便于解耦。
    /// </summary>
    public class FighterResources : MonoBehaviour {
        /// <summary>Owning fighter controller. 所属角色控制器。</summary>
        public FightingGame.Combat.Actors.FighterActor fighter;

        /// <summary>Raised when HP changes (current,max). 生命值变化事件（当前，最大）。</summary>
        public System.Action<int,int> OnHealthChanged; // (current, max)
        /// <summary>Raised when Meter changes (current,max). 气槽变化事件（当前，最大）。</summary>
        public System.Action<int,int> OnMeterChanged;  // (current, max)

        void Awake() { if (!fighter) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>(); }

        void Start() {
            // Broadcast initial values so UI binders render immediately
            int maxHp = fighter && fighter.stats ? fighter.stats.maxHealth : 20000;
            OnHealthChanged?.Invoke(fighter ? fighter.currentHealth : maxHp, maxHp);
            int maxMeter = fighter && fighter.stats ? fighter.stats.maxMeter : 1000;
            OnMeterChanged?.Invoke(fighter ? fighter.meter : 0, maxMeter);
            #if UNITY_EDITOR
            Debug.Log("[FighterResources] Broadcast initial HP/Meter to UI binders.");
            #endif
        }

        /// <summary>
        /// Increase meter by value and notify listeners.
        /// 增加气槽并通知监听者。
        /// </summary>
        public void IncreaseMeter(int value) {
            int before = fighter.meter;
            int maxMeter = fighter.stats != null ? fighter.stats.maxMeter : 1000;
            int minMeter = fighter.stats != null ? fighter.stats.minMeter : 0;
            fighter.meter = Mathf.Clamp(fighter.meter + value, minMeter, maxMeter);
            if (fighter.meter != before) OnMeterChanged?.Invoke(fighter.meter, maxMeter);
        }
        /// <summary>
        /// Decrease meter by value if sufficient; returns true on success.
        /// 扣除指定气槽（不足则返回 false）。
        /// </summary>
        public bool DecreaseMeter(int value) {
            if (fighter.meter < value) return false;
            fighter.meter -= value;
            int maxMeter2 = fighter.stats != null ? fighter.stats.maxMeter : 1000;
            OnMeterChanged?.Invoke(fighter.meter, maxMeter2);
            return true;
        }
        /// <summary>
        /// Increase health by value and notify listeners.
        /// 增加生命并通知监听者。
        /// </summary>
        public void IncreaseHealth(int value) {
            int maxHp = fighter.stats != null ? fighter.stats.maxHealth : 100;
            int before = fighter.currentHealth;
            int minHp = fighter.stats != null ? fighter.stats.minHealth : 0;
            fighter.currentHealth = Mathf.Clamp(fighter.currentHealth + value, minHp, maxHp);
            if (fighter.currentHealth != before) OnHealthChanged?.Invoke(fighter.currentHealth, maxHp);
        }
        /// <summary>
        /// Decrease health by value (>0); delegates to IncreaseHealth with negative value.
        /// 扣除生命（>0），内部通过负值调用 IncreaseHealth。
        /// </summary>
        public void DecreaseHealth(int value) { if (value <= 0) return; IncreaseHealth(-value); }

        /// <summary>Set upper-body invulnerability. 设定上半身无敌。</summary>
        public void SetUpperBodyInvuln(bool on) { fighter.SetUpperBodyInvuln(on); }
        /// <summary>Set lower-body invulnerability. 设定下半身无敌。</summary>
        public void SetLowerBodyInvuln(bool on) { fighter.SetLowerBodyInvuln(on); }
    }
}