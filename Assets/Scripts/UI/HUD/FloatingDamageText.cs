using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI.HUD {
	/// <summary>
	/// Spawns floating damage texts on DamageBus events. Simple pool to avoid GC.
	/// </summary>
	public class FloatingDamageText : MonoBehaviour {
		public UI.CanvasRoot canvasRoot;
		public int poolSize = 10;
		public float floatDistance = 40f;
		public float lifetime = 0.8f;
		public Color hitColor = new Color(1f, 0.95f, 0.3f, 1f);
		public Color blockColor = new Color(0.6f, 0.8f, 1f, 1f);

		Text[] pool;
		int nextIndex;

		void Awake() {
			if (!canvasRoot)
			{
				var rootGo = GameObject.Find("Canvas");
				if (rootGo)
				{
					canvasRoot = rootGo.GetComponent<UI.CanvasRoot>();
				}
			}
			EnsurePool();
		}
		void OnEnable() {
			Systems.DamageBus.OnDamage += OnDamage;
		}
		void OnDisable() {
			Systems.DamageBus.OnDamage -= OnDamage;
		}

		void EnsurePool() {
			if (canvasRoot == null)
			{
				return;
			}
			pool = new Text[poolSize];
			for (int i = 0; i < poolSize; i++)
			{
				pool[i] = CreateText(canvasRoot.transform);
				pool[i].gameObject.SetActive(false);
			}
		}

		Text CreateText(Transform parent) {
			var go = new GameObject("DamageText", typeof(RectTransform), typeof(Text));
			go.transform.SetParent(parent, false);
			var rect = go.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(240, 64);
			var txt = go.GetComponent<Text>();
			txt.alignment = TextAnchor.MiddleCenter;
			txt.fontSize = 30;
			Font f = null;
			try {
				f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			} catch {
				f = null;
			}
			if (f == null) {
				try {
					string[] c = new string[]{"Arial","Noto Sans CJK SC","Microsoft YaHei","PingFang SC","Heiti SC"};
					f = Font.CreateDynamicFontFromOSFont(c, 22);
				} catch {
					f = null;
				}
			}
			txt.font = f;
			return txt;
		}

		void OnDamage(int amount, Vector3 worldPos, bool blocked, FightingGame.Combat.Actors.FighterActor attacker, FightingGame.Combat.Actors.FighterActor victim) {
			if (canvasRoot == null || pool == null || pool.Length == 0)
			{
				return;
			}
			Text t = pool[nextIndex];
			nextIndex = (nextIndex + 1) % pool.Length;
			if (t == null)
			{
				return;
			}
			t.gameObject.SetActive(true);
			t.text = blocked ? "Block" : amount.ToString();
			t.color = blocked ? blockColor : hitColor;
			Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(worldPos) : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			t.rectTransform.position = screen;
			StartCoroutine(Animate(t));
		}

		IEnumerator Animate(Text t) {
			float time = 0f;
			Color c = t.color;
			Vector3 start = t.rectTransform.position;
			Vector3 end = start + new Vector3(0f, floatDistance, 0f);
			while (time < lifetime)
			{
				time += Time.deltaTime;
				float k = Mathf.Clamp01(time / lifetime);
				t.rectTransform.position = Vector3.Lerp(start, end, k);
				Color nc = c;
				nc.a = 1f - k;
				t.color = nc;
				yield return null;
			}
			t.gameObject.SetActive(false);
		}
	}
}