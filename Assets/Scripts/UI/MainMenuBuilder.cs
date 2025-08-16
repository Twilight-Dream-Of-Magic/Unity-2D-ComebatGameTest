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
            var startButton = CreateButton(root, new Vector2(0, -60), "Start Game", () => {
                var menu = CreateOrGet<MainMenuController>();
                menu.battleSceneName = battleSceneName; menu.StartGame();
            });

            CreateDifficulty(root, new Vector2(0, -120));
            CreateVolumeSliders(root, new Vector2(0, -220));
            CreateUIMode(root, new Vector2(0, -280));

            CreateButton(root, new Vector2(0, -340), "Quit", () => {
                var menu = CreateOrGet<MainMenuController>();
                menu.Quit();
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
                var audioManagerObject = new GameObject("AudioManager");
                var audioManager = audioManagerObject.AddComponent<AudioManager>();
                audioManager.bgmSource = audioManagerObject.AddComponent<AudioSource>();
                audioManager.sfxSource = audioManagerObject.AddComponent<AudioSource>();
            }
            if (!FindObjectOfType<RuntimeConfig>()) new GameObject("RuntimeConfig").AddComponent<RuntimeConfig>();
        }

        void EnsureEventSystem() {
            if (!FindObjectOfType<EventSystem>()) {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        Canvas CreateCanvas(out CanvasScaler scaler) {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        void CreateTitle(Transform root, string title) {
            var titleText = CreateText(root, title, new Vector2(0, -40), new Vector2(0.5f, 1f), 40, TextAnchor.UpperCenter);
            titleText.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24, 24);
        }

        void CreateDifficulty(Transform root, Vector2 position) {
            var label = CreateText(root, "Difficulty", position + new Vector2(-180, 0), new Vector2(0.5f, 1f), 22, TextAnchor.MiddleRight);
            var dropdownObject = new GameObject("Difficulty", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownObject.transform.SetParent(root, false);
            var rectTransform = dropdownObject.GetComponent<RectTransform>(); rectTransform.sizeDelta = new Vector2(240, 36); rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1f); rectTransform.pivot = new Vector2(0.5f, 0.5f); rectTransform.anchoredPosition = position + new Vector2(80, 0);
            var dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.options.Clear(); dropdown.options.Add(new Dropdown.OptionData("Easy")); dropdown.options.Add(new Dropdown.OptionData("Normal")); dropdown.options.Add(new Dropdown.OptionData("Hard"));
            dropdown.value = (int)(GameManager.Instance ? GameManager.Instance.difficulty : Difficulty.Normal);
            dropdown.onValueChanged.AddListener(i => GameManager.Instance?.SetDifficulty(i));
        }

        void CreateUIMode(Transform root, Vector2 position) {
            var label = CreateText(root, "UI Mode", position + new Vector2(-180, 0), new Vector2(0.5f, 1f), 22, TextAnchor.MiddleRight);
            var dropdownObject = new GameObject("UIMode", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownObject.transform.SetParent(root, false);
            var rectTransform = dropdownObject.GetComponent<RectTransform>(); rectTransform.sizeDelta = new Vector2(240, 36); rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1f); rectTransform.pivot = new Vector2(0.5f, 0.5f); rectTransform.anchoredPosition = position + new Vector2(80, 0);
            var dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.options.Clear(); dropdown.options.Add(new Dropdown.OptionData("Release")); dropdown.options.Add(new Dropdown.OptionData("Debug"));
            var runtimeConfig = RuntimeConfig.Instance;
            dropdown.value = runtimeConfig && runtimeConfig.uiMode == Systems.UIMode.Debug ? 1 : 0;
            dropdown.onValueChanged.AddListener(i => RuntimeConfig.Instance?.SetUIMode(i == 1 ? Systems.UIMode.Debug : Systems.UIMode.Release));
        }

        void CreateVolumeSliders(Transform root, Vector2 startPosition) {
            CreateSliderWithLabel(root, startPosition + new Vector2(0, 0), "Master", GameManager.Instance != null ? GameManager.Instance.SetMasterVolume : (System.Action<float>)null, 1f);
            CreateSliderWithLabel(root, startPosition + new Vector2(0, -60), "BGM", GameManager.Instance != null ? GameManager.Instance.SetBgmVolume : (System.Action<float>)null, 0.7f);
            CreateSliderWithLabel(root, startPosition + new Vector2(0, -120), "SFX", GameManager.Instance != null ? GameManager.Instance.SetSfxVolume : (System.Action<float>)null, 1f);
        }

        void CreateSliderWithLabel(Transform root, Vector2 position, string label, System.Action<float> onValue, float defaultValue) {
            var labelText = CreateText(root, label, position + new Vector2(-180, 0), new Vector2(0.5f, 1f), 22, TextAnchor.MiddleRight);
            var sliderObject = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            sliderObject.transform.SetParent(root, false);
            var rectTransform = sliderObject.GetComponent<RectTransform>(); rectTransform.sizeDelta = new Vector2(320, 22); rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1f); rectTransform.pivot = new Vector2(0.5f, 0.5f); rectTransform.anchoredPosition = position + new Vector2(80, 0);
            var backgroundImage = sliderObject.GetComponent<Image>(); backgroundImage.color = new Color(0,0,0,0.4f);
            var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform)); fillAreaObject.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillAreaObject.GetComponent<RectTransform>(); fillAreaRect.anchorMin = new Vector2(0,0.25f); fillAreaRect.anchorMax = new Vector2(1,0.75f); fillAreaRect.offsetMin = fillAreaRect.offsetMax = Vector2.zero;
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fillObject.transform.SetParent(fillAreaObject.transform, false);
            fillObject.GetComponent<Image>().color = new Color(0.2f,0.8f,1f,0.9f);
            var slider = sliderObject.GetComponent<Slider>(); slider.fillRect = fillObject.GetComponent<RectTransform>(); slider.minValue = 0; slider.maxValue = 1; slider.value = defaultValue;
            if (onValue != null) slider.onValueChanged.AddListener(onValue.Invoke);
        }

        Button CreateButton(Transform root, Vector2 position, string text, UnityEngine.Events.UnityAction onClick) {
            var buttonObject = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root, false);
            var rectTransform = buttonObject.GetComponent<RectTransform>(); rectTransform.sizeDelta = new Vector2(240, 44); rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 1f); rectTransform.pivot = new Vector2(0.5f, 0.5f); rectTransform.anchoredPosition = position;
            var image = buttonObject.GetComponent<Image>(); image.color = new Color(0.1f,0.5f,0.9f,0.9f);
            var textComponent = CreateText(buttonObject.transform, text, Vector2.zero, new Vector2(0.5f,0.5f), 20, TextAnchor.MiddleCenter);
            var button = buttonObject.GetComponent<Button>(); button.onClick.AddListener(onClick);
            return button;
        }

        Text CreateText(Transform parent, string content, Vector2 anchoredPosition, Vector2 anchor, int fontSize = 18, TextAnchor alignment = TextAnchor.UpperCenter) {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>(); rectTransform.sizeDelta = new Vector2(600, 40); rectTransform.anchorMin = anchor; rectTransform.anchorMax = anchor; rectTransform.pivot = anchor; rectTransform.anchoredPosition = anchoredPosition;
            var textComponent = textObject.GetComponent<Text>(); textComponent.text = content; textComponent.font = GetDefaultFont(); textComponent.alignment = alignment; textComponent.color = Color.white; textComponent.fontSize = fontSize;
            var outline = textObject.GetComponent<Outline>(); outline.effectColor = new Color(0,0,0,0.8f); outline.effectDistance = new Vector2(1.5f, -1.5f);
            return textComponent;
        }

        Font GetDefaultFont() {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
            if (font == null) {
                try {
                    var names = Font.GetOSInstalledFontNames();
                    if (names != null && names.Length > 0) font = Font.CreateDynamicFontFromOSFont(names[0], 18);
                } catch {}
            }
            return font;
        }
    }
}