using System.Collections.Generic;
using UnityEngine;

namespace Fighter {
	public class InputHistoryBuffer {
		readonly List<(FightingGame.Combat.CommandToken token, float time)> history = new();
		public float lifetimeSeconds = 0.8f;
		public void SetLifetime(float seconds) { lifetimeSeconds = Mathf.Max(0.01f, seconds); }
		public void Add(FightingGame.Combat.CommandToken token, float time) { history.Add((token, time)); Cleanup(); }
		public void Clear() { history.Clear(); }
		public void Cleanup() {
			float now = Time.time;
			for (int i = history.Count - 1; i >= 0; i--) if (now - history[i].time > lifetimeSeconds) history.RemoveAt(i);
		}
		public bool MatchTailExact(FightingGame.Combat.CommandToken[] seq, float window) {
			if (seq == null || seq.Length == 0) return false; Cleanup(); float now = Time.time; int n = seq.Length, m = history.Count; if (m < n) return false; for (int i = 0; i < n; i++) { var ht = history[m - n + i]; if (ht.token != seq[i] || now - ht.time > window) return false; } return true;
		}
	}
}