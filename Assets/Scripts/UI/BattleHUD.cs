using UnityEngine;
using UnityEngine.UI;

namespace UI {
	/// <summary>
	/// BattleHUD 僅負責「組裝」：掛在 Canvas 上，使用 HUD 資料/視圖元件拼出整個戰鬥 HUD。
	/// - Canvas/SafeArea 由 CanvasRoot 控制；此類不再創建 Canvas。
	/// - 資料來源一律來自 RoundManager（Presenter 內部訂閱），此類不直接綁定事件。
	/// </summary>
	[DefaultExecutionOrder(50)]
	public class BattleHUD : MonoBehaviour {
		public FightingGame.Combat.Actors.FighterActor p1;
		public FightingGame.Combat.Actors.FighterActor p2;

		RectTransform _root;

		void Awake() {
			var canvasRoot = GetComponent<UI.CanvasRoot>();
			_root = canvasRoot != null ? canvasRoot.Rect : GetComponent<RectTransform>();
			if (_root == null)
			{
				_root = gameObject.AddComponent<RectTransform>();
			}
			BuildHUD();
		}

		void BuildHUD() {
			// P1 Health Bar
			var p1BarObj = new GameObject("P1Bar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			p1BarObj.transform.SetParent(_root, false);
			var p1BarRect = p1BarObj.GetComponent<RectTransform>();
			p1BarRect.sizeDelta = new Vector2(400, 10);
			p1BarRect.anchorMin = p1BarRect.anchorMax = p1BarRect.pivot = new Vector2(0.02f, 0.95f);
			var p1BarView = p1BarObj.AddComponent<UI.HUD.HealthBarView>();
			if (p1BarView.fill != null) { p1BarView.fill.color = new Color(0.2f, 0.6f, 1f, 0.95f); }
			var p1HpTextObj = new GameObject("P1HpText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			p1HpTextObj.transform.SetParent(_root, false);
			var p1HpTextRect = p1HpTextObj.GetComponent<RectTransform>();
			p1HpTextRect.sizeDelta = new Vector2(200, 40);
			p1HpTextRect.anchorMin = p1HpTextRect.anchorMax = p1HpTextRect.pivot = new Vector2(0.02f, 0.942f);
			var p1HpText = p1HpTextObj.GetComponent<UnityEngine.UI.Text>();
			p1HpText.alignment = TextAnchor.MiddleLeft; p1HpText.fontSize = 20; p1HpText.color = Color.white; p1HpText.font = GetDefaultUIFont(20);
			var p1HealthPresenter = p1BarObj.AddComponent<UI.HUD.HealthPresenter>();
			p1HealthPresenter.isP1 = true; p1HealthPresenter.bar = p1BarView; p1HealthPresenter.hpText = p1HpText;

			// P2 Health Bar
			var p2BarObj = new GameObject("P2Bar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			p2BarObj.transform.SetParent(_root, false);
			var p2BarRect = p2BarObj.GetComponent<RectTransform>();
			p2BarRect.sizeDelta = new Vector2(400, 10);
			p2BarRect.anchorMin = p2BarRect.anchorMax = p2BarRect.pivot = new Vector2(0.98f, 0.95f);
			var p2BarView = p2BarObj.AddComponent<UI.HUD.HealthBarView>();
			if (p2BarView.fill != null) { p2BarView.fill.color = new Color(1f, 0.35f, 0.35f, 0.95f); }
			var p2HpTextObj = new GameObject("P2HpText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			p2HpTextObj.transform.SetParent(_root, false);
			var p2HpTextRect = p2HpTextObj.GetComponent<RectTransform>();
			p2HpTextRect.sizeDelta = new Vector2(200, 40);
			p2HpTextRect.anchorMin = p2HpTextRect.anchorMax = p2HpTextRect.pivot = new Vector2(0.98f, 0.942f);
			var p2HpText = p2HpTextObj.GetComponent<UnityEngine.UI.Text>();
			p2HpText.alignment = TextAnchor.MiddleRight; p2HpText.fontSize = 20; p2HpText.color = Color.white; p2HpText.font = GetDefaultUIFont(20);
			var p2HealthPresenter = p2BarObj.AddComponent<UI.HUD.HealthPresenter>();
			p2HealthPresenter.isP1 = false; p2HealthPresenter.bar = p2BarView; p2HealthPresenter.hpText = p2HpText;

			// Meter texts
			var p1MeterObj = new GameObject("P1MeterText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			p1MeterObj.transform.SetParent(_root, false);
			var p1MeterRect = p1MeterObj.GetComponent<RectTransform>();
			p1MeterRect.sizeDelta = new Vector2(200, 36);
			p1MeterRect.anchorMin = p1MeterRect.anchorMax = p1MeterRect.pivot = new Vector2(0.02f, 0.895f);
			var p1MeterText = p1MeterObj.GetComponent<UnityEngine.UI.Text>();
			p1MeterText.alignment = TextAnchor.MiddleLeft; p1MeterText.fontSize = 18; p1MeterText.color = Color.white; p1MeterText.font = GetDefaultUIFont(18);
			var p1MeterPresenter = p1MeterObj.AddComponent<UI.HUD.MeterPresenter>();
			p1MeterPresenter.isP1 = true; p1MeterPresenter.meterText = p1MeterText;

			var p2MeterObj = new GameObject("P2MeterText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			p2MeterObj.transform.SetParent(_root, false);
			var p2MeterRect = p2MeterObj.GetComponent<RectTransform>();
			p2MeterRect.sizeDelta = new Vector2(200, 36);
			p2MeterRect.anchorMin = p2MeterRect.anchorMax = p2MeterRect.pivot = new Vector2(0.98f, 0.895f);
			var p2MeterText = p2MeterObj.GetComponent<UnityEngine.UI.Text>();
			p2MeterText.alignment = TextAnchor.MiddleRight; p2MeterText.fontSize = 18; p2MeterText.color = Color.white; p2MeterText.font = GetDefaultUIFont(18);
			var p2MeterPresenter = p2MeterObj.AddComponent<UI.HUD.MeterPresenter>();
			p2MeterPresenter.isP1 = false; p2MeterPresenter.meterText = p2MeterText;

			// Timer
			var timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			timerObj.transform.SetParent(_root, false);
			var timerRect = timerObj.GetComponent<RectTransform>();
			timerRect.sizeDelta = new Vector2(200, 40);
			timerRect.anchorMin = timerRect.anchorMax = timerRect.pivot = new Vector2(0.5f, 0.86f);
			var timerText = timerObj.GetComponent<UnityEngine.UI.Text>();
			timerText.alignment = TextAnchor.MiddleCenter; timerText.fontSize = 26; timerText.color = Color.white; timerText.font = GetDefaultUIFont(26);
			timerObj.AddComponent<UI.HUD.TimerPresenter>().timerText = timerText;

			// State
			var stateObj = new GameObject("StateText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			stateObj.transform.SetParent(_root, false);
			var stateRect = stateObj.GetComponent<RectTransform>();
			stateRect.sizeDelta = new Vector2(600, 36);
			stateRect.anchorMin = stateRect.anchorMax = stateRect.pivot = new Vector2(0.5f, 0.05f);
			var stateText = stateObj.GetComponent<UnityEngine.UI.Text>();
			stateText.alignment = TextAnchor.MiddleCenter; stateText.fontSize = 18; stateText.color = Color.white; stateText.font = GetDefaultUIFont(18);
			var statePresenter = stateObj.AddComponent<UI.HUD.StatePresenter>();
			statePresenter.fighter = p1; statePresenter.stateText = stateText;

			// Round manager (確保存在，並讓其自行透過 FighterResources 廣播事件)
			var round = Systems.RoundManager.Instance != null ? Systems.RoundManager.Instance : CreateRoundManager(p1, p2);

			// Meter bars
			var p1MeterBarObj = new GameObject("P1MeterBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			p1MeterBarObj.transform.SetParent(_root, false);
			var p1MeterBarRect = p1MeterBarObj.GetComponent<RectTransform>();
			p1MeterBarRect.sizeDelta = new Vector2(300, 8);
			p1MeterBarRect.anchorMin = p1MeterBarRect.anchorMax = p1MeterBarRect.pivot = new Vector2(0.02f, 0.90f);
			var p1MeterBarView = p1MeterBarObj.AddComponent<UI.HUD.MeterBarView>();
			p1MeterPresenter.bar = p1MeterBarView; if (p1MeterBarView.fill != null) { p1MeterBarView.fill.color = new Color(0.35f, 0.7f, 1f, 0.95f); }

			var p2MeterBarObj = new GameObject("P2MeterBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			p2MeterBarObj.transform.SetParent(_root, false);
			var p2MeterBarRect = p2MeterBarObj.GetComponent<RectTransform>();
			p2MeterBarRect.sizeDelta = new Vector2(300, 8);
			p2MeterBarRect.anchorMin = p2MeterBarRect.anchorMax = p2MeterBarRect.pivot = new Vector2(0.98f, 0.90f);
			var p2MeterBarView = p2MeterBarObj.AddComponent<UI.HUD.MeterBarView>();
			p2MeterPresenter.bar = p2MeterBarView; if (p2MeterBarView.fill != null) { p2MeterBarView.fill.color = new Color(1f, 0.5f, 0.5f, 0.95f); }

			// Floating damage text spawner
			var floatTextObj = new GameObject("FloatingDamageText");
			floatTextObj.transform.SetParent(gameObject.transform, false);
			floatTextObj.AddComponent<UI.HUD.FloatingDamageText>();

			// Result panel（預設隱藏，等回合結束顯示）
			var resultPanelObj = new GameObject("ResultPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			resultPanelObj.transform.SetParent(gameObject.transform, false);
			var resultRect = resultPanelObj.GetComponent<RectTransform>();
			resultRect.sizeDelta = new Vector2(600, 180);
			resultRect.anchorMin = resultRect.anchorMax = resultRect.pivot = new Vector2(0.5f, 0.6f);
			var bg = resultPanelObj.GetComponent<UnityEngine.UI.Image>();
			bg.color = new Color(0f, 0f, 0f, 0.6f);
			var resultTextObj = new GameObject("ResultText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			resultTextObj.transform.SetParent(resultPanelObj.transform, false);
			var resultTextRect = resultTextObj.GetComponent<RectTransform>();
			resultTextRect.sizeDelta = new Vector2(560, 140);
			resultTextRect.anchorMin = resultTextRect.anchorMax = resultTextRect.pivot = new Vector2(0.5f, 0.5f);
			var resultText = resultTextObj.GetComponent<UnityEngine.UI.Text>();
			resultText.font = GetDefaultUIFont(36); resultText.fontSize = 36; resultText.alignment = TextAnchor.MiddleCenter; resultText.color = Color.white;
			var resultPresenter = resultPanelObj.AddComponent<UI.HUD.ResultPresenter>();
			resultPresenter.panel = resultPanelObj; resultPresenter.resultText = resultText;
			if (round != null) { round.resultPanel = resultPanelObj; round.resultText = resultText; }
		}

		Systems.RoundManager CreateRoundManager(FightingGame.Combat.Actors.FighterActor a, FightingGame.Combat.Actors.FighterActor b) {
			var roundManagerObject = new GameObject("RoundManager"); var roundManager = roundManagerObject.AddComponent<Systems.RoundManager>();
			roundManager.p1 = a; roundManager.p2 = b; return roundManager;
		}

		Font GetDefaultUIFont(int size) {
			Font f = null;
			try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
			catch { f = null; }
			if (f == null) {
				try { string[] candidates = new string[] { "Arial", "Noto Sans CJK SC", "Microsoft YaHei", "PingFang SC", "Heiti SC" }; f = Font.CreateDynamicFontFromOSFont(candidates, size); }
				catch { f = null; }
			}
			return f;
		}
	}
}