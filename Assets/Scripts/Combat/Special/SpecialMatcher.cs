using UnityEngine;
using Systems;

namespace Fighter {
	public class SpecialMatcher : MonoBehaviour {
		public Data.SpecialMoveSet specialSet;
		public Data.InputTuningConfig tuning;
		public InputHistoryBuffer buffer = new InputHistoryBuffer();

		public void Configure(Data.InputTuningConfig cfg) { tuning = cfg; if (tuning) buffer.SetLifetime(tuning.specialHistoryLifetime); }
		public void Ingest(FightingGame.Combat.CommandToken token, float time) { buffer.Add(token, time); }
		public bool TryMatch(out string trigger, out Data.SpecialKind kind) {
			trigger = null; kind = Data.SpecialKind.Damage;
			if (RuntimeConfig.Instance && !RuntimeConfig.Instance.specialsEnabled) return false;
			if (specialSet == null || specialSet.specials == null) return false;
			for (int i = 0; i < specialSet.specials.Length; i++) {
				var s = specialSet.specials[i]; if (s == null || s.sequence == null || s.sequence.Length == 0) continue;
				if (buffer.MatchTailExact(s.sequence, s.maxWindowSeconds > 0 ? s.maxWindowSeconds : (tuning ? tuning.defaultSpecialWindowSeconds : 0.32f))) { trigger = s.triggerName; kind = s.kind; return true; }
			}
			// JKJK super
			var def = new FightingGame.Combat.CommandToken[] { FightingGame.Combat.CommandToken.Light, FightingGame.Combat.CommandToken.Heavy, FightingGame.Combat.CommandToken.Light, FightingGame.Combat.CommandToken.Heavy };
			if (buffer.MatchTailExact(def, tuning ? tuning.defaultSpecialWindowSeconds : 0.32f)) { trigger = "Super"; kind = Data.SpecialKind.Damage; return true; }
			return false;
		}
	}
}