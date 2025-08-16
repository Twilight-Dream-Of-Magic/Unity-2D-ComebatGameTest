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
        public Vector2 arenaHalfExtents = new Vector2(256f, 3.5f);
        public Text timerText; // optional
        public GameObject resultPanel;
        public Text resultText; // optional

        // 單一權威：僅持有兩個戰鬥資源引用，P1/P2
        public Fighter.Core.FighterResources p1Resources;
        public Fighter.Core.FighterResources p2Resources;

        // 對外只讀：供 UI 查詢目前雙方 Fighter 引用
        public FightingGame.Combat.Actors.FighterActor p1 { get { return p1Resources != null ? p1Resources.fighter : null; } }
        public FightingGame.Combat.Actors.FighterActor p2 { get { return p2Resources != null ? p2Resources.fighter : null; } }

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
            // 若場上尚無 Fighter，這裡負責創建雙方，然後再掛資源組件
            var existing = FindObjectsOfType<FightingGame.Combat.Actors.FighterActor>();
            FightingGame.Combat.Actors.FighterActor p1 = null, p2 = null;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].team == FightingGame.Combat.Actors.FighterTeam.Player) { p1 = existing[i]; }
                else if (existing[i].team == FightingGame.Combat.Actors.FighterTeam.AI) { p2 = existing[i]; }
            }
            if (p1 == null)
            {
                p1 = Dev.FighterFactory.CreateFighter("Player", new Vector3(-1.6f, -1f, 0f), new Color(0.2f,0.6f,1f,1f), true, null);
            }
            if (p2 == null)
            {
                p2 = Dev.FighterFactory.CreateFighter("Enemy(AI)", new Vector3(1.6f, -1f, 0f), new Color(1f,0.4f,0.3f,1f), false, null);
            }
            // 資源由 RoundManager 擁有：附加在各自 Fighter 上並持有引用
            p1Resources = p1.GetComponent<Fighter.Core.FighterResources>();
            if (p1Resources == null) { p1Resources = p1.gameObject.AddComponent<Fighter.Core.FighterResources>(); }
            p2Resources = p2.GetComponent<Fighter.Core.FighterResources>();
            if (p2Resources == null) { p2Resources = p2.gameObject.AddComponent<Fighter.Core.FighterResources>(); }
            // 對手鏈接與相機
            Dev.FightLinker.LinkOpponents(p1, p2, arenaHalfExtents);
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
            // Observe player idling; if player發呆太久，AI 追擊並接戰
            NudgeAIIfPlayerIdling(f1, f2);
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

        float _playerIdleSince;
        string _lastPlayerState;
        void NudgeAIIfPlayerIdling(FightingGame.Combat.Actors.FighterActor player, FightingGame.Combat.Actors.FighterActor ai) {
            if (player == null || ai == null)
            {
                return;
            }
            string ps = player.GetCurrentStateName();
            if (ps != _lastPlayerState)
            {
                _lastPlayerState = ps;
                _playerIdleSince = Time.time;
            }
            bool playerIdleish = ps == "Idle" || ps == "LocomotionState" || ps == "Walk";
            if (playerIdleish && Time.time - _playerIdleSince > 1.2f)
            {
                var brain = ai.GetComponent<Fighter.InputSystem.AIBrain>();
                if (brain != null)
                {
                    brain.NudgePursueAndAttack(player, durationSeconds: 2.0f, nearOverride: 0.15f);
                    _playerIdleSince = Time.time;
                }
            }
        }
    }
}