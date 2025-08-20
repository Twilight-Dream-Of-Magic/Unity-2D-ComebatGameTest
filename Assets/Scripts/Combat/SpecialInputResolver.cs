using UnityEngine;
using System.Collections.Generic;

namespace FightingGame.Combat
{
	/// <summary>
	/// SpecialInputResolver — detects special move command sequences over time.
	/// EN: Uses token history + KMP matching to detect motions (length >= 3, only key-downs recorded).
	///     On match, routes to Offense/Defense to execute triggers.
	/// ZH: 利用時間窗歷史與 KMP 匹配偵測搓招（長度≥3，僅記錄按下瞬間），匹配後分派至攻防域執行 Trigger。
	/// </summary>
	[DefaultExecutionOrder(35)]
	public class SpecialInputResolver : MonoBehaviour
	{
		/// <summary>Command sequences definition asset. 指令序列資源。</summary>
		public Data.CommandSequenceSet sequenceSet;

		/// <summary>Designer tuning for history lifetime and base window. 設計師用的時間窗調參。</summary>
		public Data.InputTuningConfig tuning;

		/// <summary>Owning fighter. 所屬角色。</summary>
		public FightingGame.Combat.Actors.FighterActor fighter;

		private struct TimedToken
		{
			public CommandToken token;
			public float time;
		}

		private readonly List<TimedToken> history = new List<TimedToken>(64);
		private float lastTriggerAt;
		private string lastTriggerName;

		private void Awake()
		{
			if (this.fighter == null)
			{
				this.fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			}
		}

		/// <summary>
		/// Inject runtime config: fighter + tuning + sequence set.
		/// EN: Used for dependency injection during runtime.
		/// ZH: 運行時注入角色、調參與序列資源。
		/// </summary>
		public void SetConfig(FightingGame.Combat.Actors.FighterActor f, Data.InputTuningConfig t, Data.CommandSequenceSet newSet)
		{
			this.fighter = f;
			this.tuning = t;
			this.sequenceSet = newSet;
		}

		/// <summary>
		/// Record a command token with timestamp (only key-downs should call this).
		/// EN: Records input into history.
		/// ZH: 記錄輸入指令 token（僅記錄按下瞬間）。
		/// </summary>
		public void Push(CommandToken token)
		{
			if (token == CommandToken.None)
			{
				return;
			}

			this.history.Add(new TimedToken { token = token, time = Time.time });
			this.Cleanup();
		}

		/// <summary>
		/// Removes expired tokens from history based on lifetime.
		/// EN: Cleans up old tokens beyond history lifetime.
		/// ZH: 根據生命週期清除歷史輸入。
		/// </summary>
		private void Cleanup()
		{
			float life = (this.tuning != null) ? Mathf.Max(0.05f, this.tuning.specialHistoryLifetime) : 1.2f;
			float now = Time.time;

			int i = 0;
			while (i < this.history.Count && (now - this.history[i].time) > life)
			{
				i++;
			}

			if (i > 0)
			{
				this.history.RemoveRange(0, i);
			}
		}

		/// <summary>
		/// Try resolve any configured sequence and execute it.
		/// EN: Returns true if sequence matched and executed.
		/// ZH: 嘗試解析並執行任何已配置的指令，返回是否成功。
		/// </summary>
		public bool TryResolveAndExecute()
		{
			var cfg = Systems.RuntimeConfig.Instance;

			if (cfg != null && !cfg.specialsEnabled)
			{
				return false;
			}

			if (this.fighter == null)
			{
				return false;
			}

			return this.TryResolveNew();
		}

		private bool TryResolveNew()
		{
			if (this.sequenceSet == null || this.sequenceSet.specials == null || this.sequenceSet.specials.Length == 0)
			{
				return false;
			}

			float now = Time.time;

			for (int i = 0; i < this.sequenceSet.specials.Length; i++)
			{
				var entry = this.sequenceSet.specials[i];

				if (entry == null || entry.sequence == null)
				{
					continue;
				}

				// 強制序列長度 >= 3 才認可，否則視為普通鍵不攔截
				if (entry.sequence.Length < 3)
				{
					continue;
				}

				float baseWindow = (entry.maxWindowSeconds > 0f)
					? entry.maxWindowSeconds
					: ((this.tuning != null) ? Mathf.Max(0.1f, this.tuning.defaultSpecialWindowSeconds) : 1.0f);

				int extra = Mathf.Max(0, entry.sequence.Length - 2);
				float scaledWindow = baseWindow + extra * 0.2f;

				var filtered = this.BuildWindowedHistory(scaledWindow, now);

				if (filtered.Count == 0 || filtered.Count < entry.sequence.Length)
				{
					continue;
				}

				int idx = KMP_Find(filtered, entry.sequence);

				if (idx >= 0)
				{
#if UNITY_EDITOR
					Debug.Log($"[SpecialInputResolver] Sequence matched -> trigger={entry.triggerName}, kind={entry.kind}, window={scaledWindow:F2}s");
#endif
					return this.ExecuteByKind(entry.triggerName, entry.kind == Data.SequenceKind.Heal);
				}
			}

			return false;
		}

		private bool ExecuteByKind(string triggerName, bool isHeal)
		{
			float now = Time.time;

			if (!string.IsNullOrEmpty(this.lastTriggerName) && this.lastTriggerName == triggerName && now - this.lastTriggerAt < 0.15f)
			{
				return false;
			}

			if (this.fighter == null)
			{
				return false;
			}

			var runtimeConfig = Systems.RuntimeConfig.Instance;

			if (isHeal)
			{
				if (this.fighter.currentHealth <= runtimeConfig.playerLowHPThreshold)
				{
					return false;
				}

				var defense = (this.fighter.HRoot != null) ? this.fighter.HRoot.Defense : null;

				if (defense != null)
				{
					defense.BeginHealFlatDefense(triggerName);
				}
				else
				{
					this.fighter.ExecuteHeal(triggerName);
				}
			}
			else
			{
				string stateName = this.fighter.GetCurrentStateName();
				bool attacking = !string.IsNullOrEmpty(stateName) && stateName.StartsWith("Attack");

				if (attacking)
				{
					this.fighter.RequestComboCancel(triggerName);
				}
				else
				{
					var offense = (this.fighter.HRoot != null) ? this.fighter.HRoot.Offense : null;

					if (offense != null)
					{
						offense.BeginAttackFlat(triggerName);
					}
					else
					{
						this.fighter.EnterAttackHFSM(triggerName);
					}
				}
			}

			this.lastTriggerName = triggerName;
			this.lastTriggerAt = now;
			this.history.Clear();

			return true;
		}

		/// <summary>
		/// Build compact history within window: remove Neutral and run-length compress tokens.
		/// EN: Returns tokens within window, excluding Neutral.
		/// ZH: 構建時間窗內的精簡歷史：去掉 Neutral 並進行壓縮。
		/// </summary>
		private List<CommandToken> BuildWindowedHistory(float window, float now)
		{
			var tokens = new List<CommandToken>(this.history.Count);

			for (int i = Mathf.Max(0, this.history.Count - 64); i < this.history.Count; i++)
			{
				if (now - this.history[i].time <= window)
				{
					if (this.history[i].token != CommandToken.Neutral)
					{
						tokens.Add(this.history[i].token);
					}
				}
			}

			return tokens;
		}

		/// <summary>
		/// KmpFind — Knuth–Morris–Pratt subsequence search over tokens (exact contiguous match).
		/// EN: Returns the start index where <paramref name="needle"/> first appears in <paramref name="haystack"/>,
		///     or -1 if not found. Runs in O(n + m) time using the LPS (longest proper prefix/suffix) table.
		/// ZH: 在 token 序列中搜尋子序列的**連續匹配**起始位置；若無則返回 -1。時間複雜度 O(n + m)。
		/// 設計要點：
		/// 1) 以 i 掃描母串（haystack），以 j 掃描樣式（needle）。
		/// 2) 當 haystack[i] == needle[j]：雙指標前進；若 j 抵達樣式尾端，則找到起點 i - j。
		/// 3) 當不相等：若 j>0，退回 j = lps[j-1]（避免重掃 i 之前已比對過的前綴）；否則僅 i++。
		/// 不變量（Invariant）：在任意迴圈迭代起點，prefix 長度 j 總是等於 needle[0..j-1] 與 haystack[i-j..i-1] 的最長匹配前綴長度。
		/// </summary>
		private static int KMP_Find(System.Collections.Generic.List<CommandToken> haystack, CommandToken[] needle)
		{
			// Edge cases: null or empty pattern cannot be matched.
			if (needle == null || needle.Length == 0)
			{
				return -1;
			}

			// Precompute LPS table for the pattern.
			int[] longest_proper_prefix = BuildLPS(needle);

			int i = 0; // index in haystack
			int j = 0; // index in needle (length of current matched prefix)

			while (i < haystack.Count)
			{
				bool isEqual = haystack[i].Equals(needle[j]);

				if (isEqual)
				{
					// Characters match: advance both pointers.
					i++;
					j++;

					// If we've matched the entire needle, report the start index.
					if (j == needle.Length)
					{
						int startIndex = i - j;
						return startIndex;
					}
				}
				else
				{
					// Mismatch handling: if we have some matched prefix (j>0),
					// fall back j to the previous known proper prefix length via LPS.
					if (j != 0)
					{
						j = longest_proper_prefix[j - 1];
					}
					else
					{
						// No matched prefix: move i forward to consume next haystack token.
						i++;
					}
				}
			}

			// Not found
			return -1;
		}

		/// <summary>
		/// BuildLps — constructs the LPS (Longest Proper Prefix which is also Suffix) table for KMP.
		/// EN: lps[k] = length of the longest proper prefix of pattern[0..k] that is also a suffix of it.
		/// ZH: lps[k] 表示樣式前綴 pattern[0..k] 的**最長真前綴**與其**後綴**的相等長度。
		/// 建表邏輯：
		/// - 以 len 表示當前已匹配前綴長度，i 向右掃描 pattern。
		/// - 若 pat[i] == pat[len]：len++；lps[i] = len；i++。
		/// - 否則：若 len>0，回退 len = lps[len-1]；否則 lps[i]=0 並 i++。
		/// 不變量：在任意步驟，lps[0..i-1] 已正確，且 len = lps[i-1]（已知的最長可延伸前綴長度）。
		/// </summary>
		private static int[] BuildLPS(CommandToken[] pattern)
		{
			int[] longest_proper_prefix = new int[pattern.Length];

			// Length of the previous longest prefix suffix
			int length = 0;

			// lps[0] is always 0 (proper prefix cannot be whole string)
			longest_proper_prefix[0] = 0;

			int i = 1;

			while (i < pattern.Length)
			{
				bool isEqual = pattern[i].Equals(pattern[length]);

				if (isEqual)
				{
					length++;
					longest_proper_prefix[i] = length;
					i++;
				}
				else
				{
					if (length != 0)
					{
						// Fall back to the previous possible border.
						length = longest_proper_prefix[length - 1];
					}
					else
					{
						// No border: this position has lps 0.
						longest_proper_prefix[i] = 0;
						i++;
					}
				}
			}

			return longest_proper_prefix;
		}
	}
}
