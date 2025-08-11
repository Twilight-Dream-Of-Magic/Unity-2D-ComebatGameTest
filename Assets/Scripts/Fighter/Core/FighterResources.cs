using UnityEngine;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates resource operations: HP and meter add/consume, and invulnerability toggles.
    /// FighterController will delegate to this to keep responsibilities isolated.
    /// </summary>
    public class FighterResources : MonoBehaviour {
        public FighterController fighter;

        public System.Action<int,int> OnHealthChanged; // (current, max)
        public System.Action<int,int> OnMeterChanged;  // (current, max)

        void Awake() { if (!fighter) fighter = GetComponent<FighterController>(); }

        public void AddMeter(int value) {
            int before = fighter.meter;
            fighter.meter = Mathf.Clamp(fighter.meter + value, 0, fighter.maxMeter);
            if (fighter.meter != before) OnMeterChanged?.Invoke(fighter.meter, fighter.maxMeter);
        }
        public bool ConsumeMeter(int value) {
            if (fighter.meter < value) return false;
            fighter.meter -= value;
            OnMeterChanged?.Invoke(fighter.meter, fighter.maxMeter);
            return true;
        }
        public void AddHealth(int value) {
            int maxHp = fighter.stats != null ? fighter.stats.maxHealth : 100;
            int before = fighter.currentHealth;
            fighter.currentHealth = Mathf.Clamp(fighter.currentHealth + value, 0, maxHp);
            if (fighter.currentHealth != before) OnHealthChanged?.Invoke(fighter.currentHealth, maxHp);
        }

        public void SetUpperBodyInvuln(bool on) { fighter.SetUpperBodyInvuln(on); }
        public void SetLowerBodyInvuln(bool on) { fighter.SetLowerBodyInvuln(on); }
    }
}