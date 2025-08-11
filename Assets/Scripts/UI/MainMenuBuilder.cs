using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Systems;

namespace UI {
    /// <summary>
    /// Runtime main menu builder. Drop this in an empty scene (named MainMenu).
    /// Creates a canvas with: Title, Start Game, Difficulty dropdown, Master/BGM/SFX sliders, Quit.
    /// If defaultBgm is assigned, it will play on start; otherwise it does nothing.
    /// </summary>
    public class MainMenuBuilder : MonoBehaviour {
        public string battleSceneName = "Battle";
        public AudioClip defaultBgm;

        void Start() {
            EnsureEventSystem();
            EnsureManagers();
            if (defaultBgm) AudioManager.Instance?.PlayBGM(defaultBgm, true);

            var canvas = CreateCanvas(out var scaler);
            var root = canvas.transform;

            CreateTitle(root, "KOF-like 2D Fighter");
            var startBtn = CreateButton(root, new Vector2(0, -60), "Start Game", () => {
                var mm = CreateOrGet<MainMenuController>();
                mm.battleSceneName = battleSceneName; mm.StartGame();
            });

            CreateDifficulty(root, new Vector2(0, -120));
            CreateVolumeSliders(root, new Vector2(0, -220));

            CreateButton(root, new Vector2(0, -320), "Quit", () => {
                var mm = CreateOrGet<MainMenuController>();
                mm.Quit();
            });
        }

        T CreateOrGet<T>() where T : Component {
            var existed = FindObjectOfType<T>();
            if (existed) return existed;
            return new GameObject(typeof(T).Name).AddComponent<T>();
        }

        void EnsureManagers() {
            if (!FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();
            if (!FindObjectOfType<AudioManager>()) {
                var go = new GameObject("AudioManager");
                var am = go.AddComponent<AudioManager>();
                am.bgmSource = go.AddComponent<AudioSource>();
                am.sfxSource = go.AddComponent<AudioSource>();
            }
        }

        void EnsureEventSystem() {
            if (!FindObjectOfType<EventSystem>()) {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        Canvas CreateCanvas(out CanvasScaler scaler) {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        void CreateTitle(Transform root, string title) {
            var t = CreateText(root, title, new Vector2(0, -40), new Vector2(0.5f, 1f), 40, TextAnchor.UpperCenter);
            t.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24, 24);
        }

        void CreateDifficulty(Transform root, Vector2 pos) {
            var label = CreateText(root, "Difficulty", pos + new Vector2(-180, 0), new Vector2(0.5f, 1f), 22, TextAnchor.MiddleRight);
            var go = new GameObject("Difficulty", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(240, 36); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos + new Vector2(80, 0);
            var dd = go.GetComponent<Dropdown>();
            dd.options.Clear(); dd.options.Add(new Dropdown.OptionData("Easy")); dd.options.Add(new Dropdown.OptionData("Normal")); dd.options.Add(new Dropdown.OptionData("Hard"));
            dd.value = (int)(GameManager.Instance ? GameManager.Instance.difficulty : Difficulty.Normal);
            dd.onValueChanged.AddListener(i => GameManager.Instance?.SetDifficulty(i));
        }

        void CreateVolumeSliders(Transform root, Vector2 startPos) {
            CreateSliderWithLabel(root, startPos + new Vector2(0, 0), "Master", GameManager.Instance != null ? GameManager.Instance.SetMasterVolume : (System.Action<float>)null, 1f);
            CreateSliderWithLabel(root, startPos + new Vector2(0, -60), "BGM", GameManager.Instance != null ? GameManager.Instance.SetBgmVolume : (System.Action<float>)null, 0.7f);
            CreateSliderWithLabel(root, startPos + new Vector2(0, -120), "SFX", GameManager.Instance != null ? GameManager.Instance.SetSfxVolume : (System.Action<float>)null, 1f);
        }

        void CreateSliderWithLabel(Transform root, Vector2 pos, string label, System.Action<float> onValue, float defaultValue) {
            var lab = CreateText(root, label, pos + new Vector2(-180, 0), new Vector2(0.5f, 1f), 22, TextAnchor.MiddleRight);
            var go = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(320, 22); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos + new Vector2(80, 0);
            var bg = go.GetComponent<Image>(); bg.color = new Color(0,0,0,0.4f);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            var fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = new Vector2(0,0.25f); fa.anchorMax = new Vector2(1,0.75f); fa.offsetMin = fa.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.2f,0.8f,1f,0.9f);
            var slider = go.GetComponent<Slider>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.minValue = 0; slider.maxValue = 1; slider.value = defaultValue;
            if (onValue != null) slider.onValueChanged.AddListener(onValue.Invoke);
        }

        Button CreateButton(Transform root, Vector2 pos, string text, UnityEngine.Events.UnityAction onClick) {
            var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(240, 44); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.color = new Color(0.1f,0.5f,0.9f,0.9f);
            var t = CreateText(go.transform, text, Vector2.zero, new Vector2(0.5f,0.5f), 20, TextAnchor.MiddleCenter);
            var btn = go.GetComponent<Button>(); btn.onClick.AddListener(onClick);
            return btn;
        }

        Text CreateText(Transform parent, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize = 18, TextAnchor align = TextAnchor.UpperCenter) {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(600, 40); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = anchoredPos;
            var t = go.GetComponent<Text>(); t.text = content; t.font = GetDefaultFont(); t.alignment = align; t.color = Color.white; t.fontSize = fontSize;
            var outline = go.GetComponent<Outline>(); outline.effectColor = new Color(0,0,0,0.8f); outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        Font GetDefaultFont() {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
            if (f == null) {
                try {
                    var names = Font.GetOSInstalledFontNames();
                    if (names != null && names.Length > 0) f = Font.CreateDynamicFontFromOSFont(names[0], 18);
                } catch {}
            }
            return f;
        }
    }
}