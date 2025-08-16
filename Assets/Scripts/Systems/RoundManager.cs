using UnityEngine;
using UnityEngine.UI;
using Framework;
using Unity.VisualScripting;

namespace Systems {
    /// <summary>
    /// Manages a single round: updates a countdown timer, detects end conditions (KO or time over),
    /// decides the winner text, pauses time, and broadcasts timer/end events.
    /// 管理单回合：更新倒计时、判定结束（KO/超时）、决定胜负文本、暂停时间并广播事件。
    /// </summary>
    public class RoundManager : MonoSingleton<RoundManager> {
        public float roundTime = 60f;
        public Text timerText; // optional
        public GameObject resultPanel;
        public Text resultText; // optional

        // 單一權威：僅持有兩個戰鬥資源引用，P1/P2
        public Fighter.Core.FighterResources p1Resources;
        public Fighter.Core.FighterResources p2Resources;

        public System.Action<int> OnTimerChanged;     // seconds left
        public System.Action<string> OnRoundEnd;      // result text

        float timeLeft;
        bool ended;
        bool timeout;
        int lastSeconds = -1;

        public bool IsEnded { get { return ended; } }
        public string LastResultText { get; private set; }

        protected override void DoAwake() 
        {
            p1Resources = gameObject.AddComponent<Fighter.Core.FighterResources>();
            p2Resources = gameObject.AddComponent<Fighter.Core.FighterResources>();
        }
        protected override void DoStart() {
            timeLeft = roundTime;
            Time.timeScale = 1f;
            LastResultText = null;
            if (resultText) { resultText.gameObject.SetActive(false); resultText.fontSize = 40; }
            WireResourceRefs();
            BroadcastTimerIfChanged();
        }
        protected override void DoUpdate() {
            WireResourceRefs();
            if (ended) { return; }
            timeLeft -= Time.deltaTime;
            BroadcastTimerIfChanged();
            var f1 = p1Resources != null ? p1Resources.fighter : null;
            var f2 = p2Resources != null ? p2Resources.fighter : null;
            if ((f1 && f1.currentHealth == 0) || (f2 && f2.currentHealth == 0) || timeLeft <= 0) { timeout = timeLeft <= 0; EndRound(); }
        }

        void BroadcastTimerIfChanged() {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeLeft));
            if (seconds != lastSeconds) { lastSeconds = seconds; OnTimerChanged?.Invoke(seconds); if (timerText) { timerText.text = seconds.ToString(); } }
        }

        private void EndRound() {
            ended = true; Time.timeScale = 0f;
            string txt = "Draw";
            var f1 = p1Resources != null ? p1Resources.fighter : null;
            var f2 = p2Resources != null ? p2Resources.fighter : null;
            int p1HpNow = f1 ? f1.currentHealth : 0; int p2HpNow = f2 ? f2.currentHealth : 0;
            if (f1 && f2 && p1HpNow == 0 && p2HpNow == 0) { txt = "Double KO - Draw"; }
            else if (timeout) {
                bool bothAlive = (p1HpNow > 0) && (p2HpNow > 0);
                if (bothAlive && p1HpNow == p2HpNow) { txt = "Time Over - Draw"; }
                else { if (p1HpNow > p2HpNow) { txt = "Time Over - P1 Wins, P2 Loses"; } else if (p2HpNow > p1HpNow) { txt = "Time Over - P2 Wins, P1 Loses"; } else { txt = "Time Over - Draw"; } }
            }
            else { if (p1HpNow == 0 && p2HpNow > 0) { txt = "P2 Wins, P1 Loses"; } else if (p2HpNow == 0 && p1HpNow > 0) { txt = "P1 Wins, P2 Loses"; } else { txt = "Draw"; } }
            LastResultText = txt; OnRoundEnd?.Invoke(txt);
            if (resultText) { resultText.text = txt; if (txt.Contains("P1 Wins")) { resultText.color = new Color(0.4f, 0.8f, 1f, 1f); } else if (txt.Contains("P2 Wins")) { resultText.color = new Color(1f, 0.4f, 0.4f, 1f); } else { resultText.color = Color.white; } resultText.gameObject.SetActive(true); }
            if (resultPanel) { resultPanel.SetActive(true); }
        }

        void WireResourceRefs() {
            if (p1Resources != null && p1Resources.fighter == null)
            {
                var fighters = FindObjectsOfType<FightingGame.Combat.Actors.FighterActor>();
                for (int i = 0; i < fighters.Length; i++)
                {
                    if (fighters[i].team == FightingGame.Combat.Actors.FighterTeam.Player) { p1Resources.fighter = fighters[i]; break; }
                }
            }
            if (p2Resources != null && p2Resources.fighter == null)
            {
                var fighters = FindObjectsOfType<FightingGame.Combat.Actors.FighterActor>();
                for (int i = 0; i < fighters.Length; i++)
                {
                    if (fighters[i].team == FightingGame.Combat.Actors.FighterTeam.AI) { p2Resources.fighter = fighters[i]; break; }
                }
            }
        }
    }
}