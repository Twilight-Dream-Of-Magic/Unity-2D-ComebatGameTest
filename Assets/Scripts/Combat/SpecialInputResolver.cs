using UnityEngine;
using System.Collections.Generic;

namespace FightingGame.Combat {
	/// <summary>
	/// Detects command sequences over time using a token history with KMP matching, then routes
	/// to Offense/Defense domains to execute triggers. Length &gt;= 3 required. Only key-downs are recorded.
	/// 使用時間窗歷史+KMP 匹配搓招序列（長度≥3，僅記錄按下瞬間），命中後路由攻防域執行對應 Trigger。
	/// </summary>
	[DefaultExecutionOrder(35)]
	public class SpecialInputResolver : MonoBehaviour {
		/// <summary>Command sequences definition asset. 指令序列資源。</summary>
		public Data.CommandSequenceSet sequenceSet;
		/// <summary>Designer tuning for history lifetime and base window. 時窗調參。</summary>
		public Data.InputTuningConfig tuning;
		/// <summary>Owning fighter. 所屬角色。</summary>
		public FightingGame.Combat.Actors.FighterActor fighter;

		struct TimedToken { public CommandToken token; public float time; }
		readonly List<TimedToken> history = new List<TimedToken>(64);
		float lastTriggerAt;
		string lastTriggerName;

		void Awake() { if (!fighter) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>(); }

		/// <summary>
		/// Inject runtime config: fighter + tuning + sequence set.
		/// 於運行時注入角色、調參與序列資源。
		/// </summary>
		public void SetConfig(FightingGame.Combat.Actors.FighterActor f, Data.InputTuningConfig t, Data.CommandSequenceSet newSet) {
			fighter = f; tuning = t; sequenceSet = newSet;
		}

		/// <summary>
		/// Record a command token with timestamp (only key-downs should call this).
		/// 記錄指令 token（僅按下瞬間調用）。
		/// </summary>
		public void Push(CommandToken token) { if (token == CommandToken.None) return; history.Add(new TimedToken { token = token, time = Time.time }); Cleanup(); }

		void Cleanup() {
			float life = tuning != null ? Mathf.Max(0.05f, tuning.specialHistoryLifetime) : 1.2f;
			float now = Time.time; int i = 0; while (i < history.Count && (now - history[i].time) > life) i++; if (i > 0) history.RemoveRange(0, i);
		}

		/// <summary>
		/// Try resolve any configured sequence and execute it. 返回是否已處理。
		/// </summary>
		public bool TryResolveAndExecute() {
			var cfg = Systems.RuntimeConfig.Instance; if (cfg != null && !cfg.specialsEnabled) return false; if (fighter == null) return false; return TryResolveNew();
		}

		bool TryResolveNew() {
			if (sequenceSet == null || sequenceSet.specials == null || sequenceSet.specials.Length == 0) return false;
			float now = Time.time;
			for (int i = 0; i < sequenceSet.specials.Length; i++) {
				var entry = sequenceSet.specials[i];
				if (entry == null || entry.sequence == null) continue;
				// 強制序列長度>=3 才認可，否則視為普通鍵不攔截
				if (entry.sequence.Length < 3) continue;
				float baseWindow = entry.maxWindowSeconds > 0f ? entry.maxWindowSeconds : (tuning != null ? Mathf.Max(0.1f, tuning.defaultSpecialWindowSeconds) : 1.0f);
				int extra = Mathf.Max(0, entry.sequence.Length - 2);
				float scaledWindow = baseWindow + extra * 0.2f;
				var filtered = BuildWindowedHistory(scaledWindow, now);
				if (filtered.Count == 0 || filtered.Count < entry.sequence.Length) continue;
				int idx = KmpFind(filtered, entry.sequence);
				if (idx >= 0) {
#if UNITY_EDITOR
					Debug.Log($"[SpecialInputResolver] Sequence matched -> trigger={entry.triggerName}, kind={entry.kind}, window={scaledWindow:F2}s");
#endif
					return ExecuteByKind(entry.triggerName, entry.kind == Data.SequenceKind.Heal);
				}
			}
			return false;
		}

		bool ExecuteByKind(string triggerName, bool isHeal) {
			float now = Time.time; 
			if (!string.IsNullOrEmpty(lastTriggerName) && lastTriggerName == triggerName && now - lastTriggerAt < 0.15f) 
				return false; 
			if (fighter == null) 
				return false;

			var runtimeConfig = Systems.RuntimeConfig.Instance;
			if (isHeal)
			{
				if (fighter.currentHealth <= runtimeConfig.playerLowHPThreshold)
				{
					return false;
				}

				var defense = fighter.HRoot != null ? fighter.HRoot.Defense : null;
				if (defense != null)
				{
					defense.BeginHealFlatDefense(triggerName);
				}
				else
				{
					fighter.ExecuteHeal(triggerName);
				}
			}
			else
			{
				string stateName = fighter.GetCurrentStateName(); 
				bool attacking = !string.IsNullOrEmpty(stateName) && stateName.StartsWith("Attack");
				if (attacking)
				{
					fighter.RequestComboCancel(triggerName);
				}
				else 
				{
					var offense = fighter.HRoot != null ? fighter.HRoot.Offense : null;
					if (offense != null)
					{
						offense.BeginAttackFlat(triggerName);
					}
					else
					{
						fighter.EnterAttackHFSM(triggerName);
					}
				} 
			}
			lastTriggerName = triggerName;
			lastTriggerAt = now;
			history.Clear(); 
			return true;
		}

		/// <summary>
		/// Build compact history within window: remove Neutral and run-length compress tokens.
		/// 構建時窗內的精簡歷史：去掉 Neutral 並做連續壓縮。
		/// </summary>
		List<CommandToken> BuildWindowedHistory(float window, float now) {
			var tokens = new List<CommandToken>(history.Count);
			for (int i = Mathf.Max(0, history.Count - 64); i < history.Count; i++) {
				if (now - history[i].time <= window) {
					if (history[i].token != CommandToken.Neutral) tokens.Add(history[i].token);
				}
			}
			return tokens;
		}

		static int KmpFind(List<CommandToken> haystack, CommandToken[] needle) 
		{ if (needle == null || needle.Length == 0) return -1; int[] lps = BuildLps(needle); int i = 0, j = 0; while (i < haystack.Count) { if (haystack[i].Equals(needle[j])) { i++; j++; if (j == needle.Length) return i - j; } else { if (j != 0) j = lps[j - 1]; else i++; } } return -1; }
		static int[] BuildLps(CommandToken[] pat) 
		{ int[] lps = new int[pat.Length]; int len = 0; int i = 1; lps[0] = 0; while (i < pat.Length) { if (pat[i].Equals(pat[len])) { len++; lps[i] = len; i++; } else { if (len != 0) len = lps[len - 1]; else { lps[i] = 0; i++; } } } return lps; }
	}
}