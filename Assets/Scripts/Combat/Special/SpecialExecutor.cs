using UnityEngine;
using Systems;

namespace Fighter {
	public class SpecialExecutor : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor fighter;
		public SpecialMatcher matcher;
		void Awake() { if (!fighter) fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>(); if (!matcher) matcher = GetComponent<SpecialMatcher>(); }
		public bool HandleToken(FightingGame.Combat.CommandToken token, float time) {
			if (RuntimeConfig.Instance && !RuntimeConfig.Instance.specialsEnabled) return false;
			if (matcher == null) return false;
			matcher.Ingest(token, time);
			if (matcher.TryMatch(out var trigger, out var kind)) {
				var stateName = fighter.GetCurrentStateName();
				var off = fighter.HRoot?.Offense;
				bool attacking = stateName.StartsWith("Attack");
				if (kind == Data.SpecialKind.Heal) {
					if (attacking) fighter.RequestComboCancel(trigger); else { if (off!=null) off.BeginHealFlat(trigger); else fighter.ExecuteHeal(trigger); }
				} else {
					if (attacking) fighter.RequestComboCancel(trigger); else { if (off!=null) off.BeginAttackFlat(trigger); else fighter.EnterAttackHFSM(trigger); }
				}
				matcher.buffer.Clear();
				return true;
			}
			return false;
		}
	}
}