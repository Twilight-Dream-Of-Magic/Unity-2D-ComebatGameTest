using UnityEngine;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates resource operations: HP and meter add/consume, and invulnerability toggles.
    /// FighterController will delegate to this to keep responsibilities isolated.
    /// </summary>
    public class FighterResources : MonoBehaviour {
        public FighterController fighter;

        void Awake() { if (!fighter) fighter = GetComponent<FighterController>(); }

        public void AddMeter(int value) { fighter.meter = Mathf.Clamp(fighter.meter + value, 0, fighter.maxMeter); }
        public bool ConsumeMeter(int value) { if (fighter.meter < value) return false; fighter.meter -= value; return true; }
        public void AddHealth(int value) { int maxHp = fighter.stats != null ? fighter.stats.maxHealth : 100; fighter.currentHealth = Mathf.Clamp(fighter.currentHealth + value, 0, maxHp); }

        public void SetUpperBodyInvuln(bool on) { fighter.SetUpperBodyInvuln(on); }
        public void SetLowerBodyInvuln(bool on) { fighter.SetLowerBodyInvuln(on); }
    }
}