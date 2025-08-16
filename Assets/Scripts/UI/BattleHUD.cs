using UnityEngine;
using UnityEngine.UI;

namespace UI {
	/// <summary>
	/// BattleHUD 僅負責「組裝」：在 Canvas 上拼裝各個 HUD 區塊。
	/// - 條視圖使用：HealthBarView / MeterBarView
	/// - 數值文字使用：HealthPresenter / MeterPresenter
	/// - 狀態文字使用：StatePresenter
	/// - 本類不創建 RoundManager（由 ManagersBootstrapper 保證存在）
	/// </summary>
	[DefaultExecutionOrder(50)]
	public class BattleHUD : MonoBehaviour {
		RectTransform _root;

		void Awake()
		{
			var canvasRoot = GetComponent<UI.CanvasRoot>();
			_root = canvasRoot != null ? canvasRoot.Rect : GetComponent<RectTransform>();
			if (_root == null)
			{
				_root = gameObject.AddComponent<RectTransform>();
			}
			Build();
		}

		void Build()
		{
			CreateHealthSection(isP1: true);
			CreateHealthSection(isP1: false);
			CreateMeterSection(isP1: true);
			CreateMeterSection(isP1: false);
			CreateTimer();
			// world-space state text is created in FightLinker
			CreateFloatingDamageText();
			CreateResultPanel();
		}

		void CreateHealthSection(bool isP1)
		{
			// 條（BarView 在條物件上）
			Vector2 barAnchor = isP1 ? new Vector2(0.02f, 0.95f) : new Vector2(0.98f, 0.95f);
			Vector2 barPivot = isP1 ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
			var barObj = CreateBarContainer(isP1 ? "P1Bar" : "P2Bar", barAnchor, new Vector2(500, 10), barPivot);
			var barView = barObj.gameObject.AddComponent<UI.HUD.HealthBarView>();
			if (barView.slider != null)
			{
				barView.slider.direction = isP1 ? Slider.Direction.LeftToRight : Slider.Direction.RightToLeft;
			}
			if (barView.fill != null)
			{
				barView.fill.color = isP1 ? new Color(0.2f, 0.6f, 1f, 0.95f) : new Color(1f, 0.35f, 0.35f, 0.95f);
			}

			// 文字（Presenter 掛在文字物件上）
			Vector2 textAnchor = isP1 ? new Vector2(0.02f, 0.942f) : new Vector2(0.98f, 0.942f);
			Vector2 textPivot = isP1 ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
			var text = CreateText(isP1 ? "P1HpText" : "P2HpText", textAnchor, new Vector2(200, 40), textPivot,
				isP1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, 20);
			var presenter = text.gameObject.AddComponent<UI.HUD.HealthPresenter>();
			presenter.isP1 = isP1;
			presenter.bar = barView;
			presenter.hpText = text;
			presenter.ForceRebind();
		}

		void CreateMeterSection(bool isP1)
		{
			// 需求：Meter 相對 HP 下移 9f（約 0.09 的 Anchor Y 偏移）
			// 條（BarView 在條物件上）
			Vector2 barAnchor = isP1 ? new Vector2(0.02f, 0.90f - 0.09f) : new Vector2(0.98f, 0.90f - 0.09f);
			Vector2 barPivot = isP1 ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
			var barObj = CreateBarContainer(isP1 ? "P1MeterBar" : "P2MeterBar", barAnchor, new Vector2(500, 10), barPivot);
			var barView = barObj.gameObject.AddComponent<UI.HUD.MeterBarView>();
			if (barView.slider != null)
			{
				barView.slider.direction = isP1 ? Slider.Direction.LeftToRight : Slider.Direction.RightToLeft;
			}
			if (barView.fill != null)
			{
				barView.fill.color = isP1 ? new Color(0.35f, 0.7f, 1f, 0.95f) : new Color(1f, 0.5f, 0.5f, 0.95f);
			}

			// 文字（Presenter 掛在文字物件上）
			Vector2 textAnchor = isP1 ? new Vector2(0.02f, 0.95f - 0.18f) : new Vector2(0.98f, 0.95f - 0.18f);
			Vector2 textPivot = isP1 ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
			var text = CreateText(isP1 ? "P1MeterText" : "P2MeterText", textAnchor, new Vector2(200, 36), textPivot,
				isP1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, 18);
			var presenter = text.gameObject.AddComponent<UI.HUD.MeterPresenter>();
			presenter.isP1 = isP1;
			presenter.meterText = text;
			presenter.bar = barView;
			presenter.ForceRebind();
		}

		void CreateHeadState(bool isP1)
		{
			var text = CreateText(isP1 ? "P1HeadState" : "P2HeadState", new Vector2(0.5f, 0.5f), new Vector2(240, 28), new Vector2(0.5f, 0.5f),
				TextAnchor.MiddleCenter, 16);
			var billboard = text.gameObject.AddComponent<UI.HUD.HeadStatePresenter>();
			billboard.isP1 = isP1;
			var round = Systems.RoundManager.Instance;
			var f = isP1 ? (round != null ? round.p1 : null) : (round != null ? round.p2 : null);
			Transform anchor = null;
			if (f != null)
			{
				anchor = f.GetComponentInChildren<Fighter.Core.HeadStateAnchor>() != null
					? f.GetComponentInChildren<Fighter.Core.HeadStateAnchor>().transform
					: f.transform.Find("HeadStateAnchor");
			}
			billboard.ForceBind(f, text, anchor);
			billboard.offset = new Vector3(0f, 2.2f, 0f);
		}

		void CreateTimer()
		{
			var text = CreateText("TimerText", new Vector2(0.5f, 0.86f), new Vector2(200, 40), new Vector2(0.5f, 0.5f),
				TextAnchor.MiddleCenter, 26);
			text.gameObject.AddComponent<UI.HUD.TimerPresenter>().timerText = text;
		}

		void CreateStateText() { }

		void CreateFloatingDamageText()
		{
			var go = new GameObject("FloatingDamageText");
			go.transform.SetParent(_root, false);
			go.AddComponent<UI.HUD.FloatingDamageText>();
		}

		void CreateResultPanel()
		{
			var panelObj = new GameObject("ResultPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			panelObj.transform.SetParent(_root, false);
			var panelRect = panelObj.GetComponent<RectTransform>();
			panelRect.sizeDelta = new Vector2(600, 180);
			panelRect.anchorMin = new Vector2(0.5f, 0.6f);
			panelRect.anchorMax = new Vector2(0.5f, 0.6f);
			panelRect.pivot = new Vector2(0.5f, 0.5f);
			var bg = panelObj.GetComponent<UnityEngine.UI.Image>();
			bg.color = new Color(0f, 0f, 0f, 0.6f);

			var resultText = CreateText("ResultText", new Vector2(0.5f, 0.5f), new Vector2(560, 140), new Vector2(0.5f, 0.5f),
				TextAnchor.MiddleCenter, 36, panelObj.transform);
			var presenter = panelObj.AddComponent<UI.HUD.ResultPresenter>();
			presenter.panel = panelObj;
			presenter.resultText = resultText;

			var round = Systems.RoundManager.Instance;
			if (round != null)
			{
				round.resultPanel = panelObj;
				round.resultText = resultText;
			}
		}

		RectTransform CreateBarContainer(string name, Vector2 anchor, Vector2 size, Vector2 pivot)
		{
			var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
			obj.transform.SetParent(_root, false);
			var rect = obj.GetComponent<RectTransform>();
			rect.sizeDelta = size;
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = pivot;
			rect.anchoredPosition = Vector2.zero;
			var bg = obj.GetComponent<UnityEngine.UI.Image>();
			bg.color = new Color(0f, 0f, 0f, 0.5f);
			return rect;
		}

		Text CreateText(string name, Vector2 anchor, Vector2 size, Vector2 pivot, TextAnchor align, int fontSize, Transform parentOverride = null)
		{
			var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
			obj.transform.SetParent(parentOverride == null ? _root : parentOverride, false);
			var rect = obj.GetComponent<RectTransform>();
			rect.sizeDelta = size;
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = pivot;
			rect.anchoredPosition = Vector2.zero;
			var text = obj.GetComponent<UnityEngine.UI.Text>();
			text.alignment = align;
			text.fontSize = fontSize;
			text.color = Color.white;
			text.font = GetDefaultUIFont(fontSize);
			return text;
		}

		Font GetDefaultUIFont(int size)
		{
			Font f = null;
			try
			{
				f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			}
			catch
			{
				f = null;
			}
			if (f == null)
			{
				try
				{
					string[] candidates = new string[] { "Arial", "Noto Sans CJK SC", "Microsoft YaHei", "PingFang SC", "Heiti SC" };
					f = Font.CreateDynamicFontFromOSFont(candidates, size);
				}
				catch
				{
					f = null;
				}
			}
			return f;
		}
	}
}