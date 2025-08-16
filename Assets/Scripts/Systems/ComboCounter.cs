using UnityEngine;
using System;
using Framework;

namespace Systems {
    /// <summary>
    /// Tracks consecutive hits landed by the same attacker within a timeout window (unscaled time),
    /// raises OnComboChanged, and resets when time elapses or explicitly cleared.
    /// 连击统计：在超时窗口内由同一攻击者累计命中；触发 OnComboChanged；超时或显式重置时清零。
    /// </summary>
    public class ComboCounter : MonoSingleton<ComboCounter> {
        /// <summary>Current combo count. 当前连击数。</summary>
        public int currentCount { get; private set; }
        /// <summary>Current attacker responsible for the combo. 当前连击的攻击者。</summary>
        public FightingGame.Combat.Actors.FighterActor currentAttacker { get; private set; }
        /// <summary>Timeout in seconds to keep the combo alive. 维持连击的超时时间（秒）。</summary>
        public float timeoutSeconds = 1.2f;
        float timeLeft;

        /// <summary>Raised on combo change (count, attacker). 连击变化事件（计数, 攻击者）。</summary>
        public event Action<int,FightingGame.Combat.Actors.FighterActor> OnComboChanged; // (count, attacker)

        protected override void DoAwake() {
            FightingGame.Combat.Actors.FighterActor.OnAnyDamage += OnAnyDamage;
        }
        protected override void DoDestroy() {
            FightingGame.Combat.Actors.FighterActor.OnAnyDamage -= OnAnyDamage;
        }

        protected override void DoUpdate() {
            if (currentCount > 0) {
                timeLeft -= Time.unscaledDeltaTime;
                if (timeLeft <= 0) ResetCombo();
            }
        }

        void OnAnyDamage(FightingGame.Combat.Actors.FighterActor attacker, FightingGame.Combat.Actors.FighterActor victim) {
            if (currentAttacker == attacker) {
                currentCount++;
            } else {
                currentAttacker = attacker; currentCount = 1;
            }
            timeLeft = timeoutSeconds;
            OnComboChanged?.Invoke(currentCount, currentAttacker);
        }

        /// <summary>Reset combo to zero and clear attacker. 重置连击并清空攻击者。</summary>
        public void ResetCombo() { currentCount = 0; currentAttacker = null; timeLeft = 0; OnComboChanged?.Invoke(0, null); }
    }
}