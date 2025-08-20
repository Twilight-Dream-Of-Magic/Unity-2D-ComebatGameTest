using UnityEngine;

namespace Fighter.Core
{
	/// <summary>
	/// Encapsulates resource operations: health and meter add/consume, and invulnerability toggles.
	/// Fighter controller will delegate to this to keep responsibilities isolated.
	/// 資源管理：統一處理氣槽與生命的增減，以及上/下半身無敵切換；控制器通過該組件修改資源，便於解耦。
	/// </summary>
	public class FighterResources : MonoBehaviour
	{
		/// <summary>Owning fighter controller. 所屬角色控制器。</summary>
		public FightingGame.Combat.Actors.FighterActor fighter;

		/// <summary>Raised when health changes (current, max). 生命值變化事件（當前，最大）。</summary>
		public System.Action<int, int> OnHealthChanged;

		/// <summary>Raised when meter changes (current, max). 氣槽變化事件（當前，最大）。</summary>
		public System.Action<int, int> OnMeterChanged;

		private void Awake()
		{
			if (!fighter)
			{
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
		}

		private void Start()
		{
			// Broadcast initial values so UI binders render immediately
			int maximumHealth = fighter && fighter.stats ? fighter.stats.maxHealth : 20000;
			int currentHealth = fighter ? fighter.currentHealth : maximumHealth;
			OnHealthChanged?.Invoke(currentHealth, maximumHealth);

			int maximumMeter = fighter && fighter.stats ? fighter.stats.maxMeter : 1000;
			int currentMeter = fighter ? fighter.meter : 0;
			OnMeterChanged?.Invoke(currentMeter, maximumMeter);

#if UNITY_EDITOR
			Debug.Log("[FighterResources] Broadcast initial Health and Meter to UI binders.");
#endif
		}

		/// <summary>
		/// Increase meter by value and notify listeners.
		/// 增加氣槽並通知監聽者。
		/// </summary>
		public void IncreaseMeter(int value)
		{
			int meterBefore = fighter.meter;
			int maximumMeter = fighter.stats != null ? fighter.stats.maxMeter : 1000;
			int minimumMeter = fighter.stats != null ? fighter.stats.minMeter : 0;

			fighter.meter = Mathf.Clamp(fighter.meter + value, minimumMeter, maximumMeter);

			if (fighter.meter != meterBefore)
			{
				OnMeterChanged?.Invoke(fighter.meter, maximumMeter);
			}
		}

		/// <summary>
		/// Decrease meter by value if sufficient; returns true on success.
		/// 扣除指定氣槽（不足則返回 false）。
		/// </summary>
		public bool DecreaseMeter(int value)
		{
			if (fighter.meter < value)
			{
				return false;
			}

			fighter.meter -= value;
			int maximumMeter = fighter.stats != null ? fighter.stats.maxMeter : 1000;
			OnMeterChanged?.Invoke(fighter.meter, maximumMeter);
			return true;
		}

		/// <summary>
		/// Increase health by value and notify listeners.
		/// 增加生命並通知監聽者。
		/// </summary>
		public void IncreaseHealth(int value)
		{
			int maximumHealth = fighter.stats != null ? fighter.stats.maxHealth : 100;
			int healthBefore = fighter.currentHealth;
			int minimumHealth = fighter.stats != null ? fighter.stats.minHealth : 0;

			fighter.currentHealth = Mathf.Clamp(fighter.currentHealth + value, minimumHealth, maximumHealth);

			if (fighter.currentHealth != healthBefore)
			{
				OnHealthChanged?.Invoke(fighter.currentHealth, maximumHealth);
			}
		}

		/// <summary>
		/// Decrease health by value (>0); delegates to IncreaseHealth with negative value.
		/// 扣除生命（>0），內部通過負值調用 IncreaseHealth。
		/// </summary>
		public void DecreaseHealth(int value)
		{
			if (value <= 0)
			{
				return;
			}

			IncreaseHealth(-value);
		}

		/// <summary>
		/// Set upper-body invulnerability. 設定上半身無敵。
		/// </summary>
		public void SetUpperBodyInvulnerability(bool on)
		{
			fighter.SetUpperBodyInvulnerable(on);
		}

		/// <summary>
		/// Set lower-body invulnerability. 設定下半身無敵。
		/// </summary>
		public void SetLowerBodyInvulnerability(bool on)
		{
			fighter.SetLowerBodyInvulnerable(on);
		}
	}
}
