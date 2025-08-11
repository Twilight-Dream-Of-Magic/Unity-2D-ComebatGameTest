using UnityEngine;
using UnityEngine.UI;
using Fighter;

namespace UI {
    /// <summary>
    /// Factory to construct a complete battle HUD (Canvas, HP/Meter bars, timer, hints, debug HUD, result text).
    /// Returns references for binding with RoundManager and other systems.
    /// </summary>
    public static class HUDFactory {
        public class HUDRefs {
            public Canvas canvas;
            public Slider hp1, hp2;
            public Slider meter1, meter2;
            public Text timer;
            public Text hint;
            public Text resultText;
            public Text stateText;
            public Text comboText;
        }

        public static HUDRefs Create(Transform parent, FighterController p1, FighterController p2) {
            var refs = new HUDRefs();
            refs.canvas = CreateCanvas(parent, out var _);
            var root = refs.canvas.transform;

            refs.hp1 = CreateHpSlider(root, true);
            refs.hp2 = CreateHpSlider(root, false);
            refs.meter1 = CreateMeterSlider(root, true);
            refs.meter2 = CreateMeterSlider(root, false);
            refs.timer = CreateTimer(root);
            refs.hint = CreateHint(root);

            // Bind HP text + flash + meter
            BindHpAndMeter(p1, p2, refs.hp1, refs.hp2, refs.meter1, refs.meter2);

            // per-fighter state texts near corners
            CreateStateText(root, p1, true);
            CreateStateText(root, p2, false);

            // numeric values near bars
            CreateValueTexts(root, refs.hp1, p1, true);
            CreateValueTexts(root, refs.hp2, p2, false);

            // Debug HUD (central summary + combo)
            CreateDualDebugHud(root, p1, p2, out refs.stateText, out refs.comboText);

            // Result overlay text only; panel optional
            refs.resultText = CreateResultOverlay(root);

            return refs;
        }

        static Canvas CreateCanvas(Transform parent, out CanvasScaler scaler) {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent) canvasGO.transform.SetParent(parent, false);
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // balanced scaling across aspect ratios
            return canvas;
        }

        static Slider CreateHpSlider(Transform root, bool left) {
            var pos = left ? new Vector2(260, -40) : new Vector2(-260, -40);
            var anchor = left ? new Vector2(0,1) : new Vector2(1,1);
            var color = left ? new Color(0.2f,0.6f,1f,0.95f) : new Color(1f,0.35f,0.35f,0.95f);
            var s = CreateSlider(root, pos, anchor, color);
            s.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            if (!left) s.direction = Slider.Direction.RightToLeft;
            return s;
        }

        static Slider CreateMeterSlider(Transform root, bool left) {
            var pos = left ? new Vector2(260, -80) : new Vector2(-260, -80);
            var anchor = left ? new Vector2(0,1) : new Vector2(1,1);
            var s = CreateSlider(root, pos, anchor, new Color(0.2f,0.8f,0.2f,0.9f));
            s.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            if (!left) s.direction = Slider.Direction.RightToLeft;
            return s;
        }

        static Text CreateTimer(Transform root) {
            var t = CreateText(root, "60", new Vector2(0, -40), new Vector2(0.5f,1), 28, TextAnchor.UpperCenter);
            t.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            return t;
        }

        static Text CreateHint(Transform root) {
            var t = CreateText(root, "Move: A/D or Arrow Keys  Jump: Space/W/Up  Crouch: S/Down  Block: Left Shift  Dodge: L/Ctrl  Light: J  Heavy: K", new Vector2(0, 30), new Vector2(0.5f,0), 20, TextAnchor.LowerCenter);
            t.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            return t;
        }

        static void BindHpAndMeter(FighterController p1, FighterController p2, Slider hp1, Slider hp2, Slider m1, Slider m2) {
            // Removed numeric HP texts, keep flash and meter binders
            var p1Flash = hp1.gameObject.AddComponent<HpFlashOnDamage>(); p1Flash.fighter = p1; p1Flash.slider = hp1; p1Flash.flashColor = Color.white;
            var p2Flash = hp2.gameObject.AddComponent<HpFlashOnDamage>(); p2Flash.fighter = p2; p2Flash.slider = hp2; p2Flash.flashColor = Color.white;
            var eb1 = hp1.gameObject.AddComponent<EnergyBarBinder>(); eb1.fighter = p1; eb1.slider = m1;
            var eb2 = hp2.gameObject.AddComponent<EnergyBarBinder>(); eb2.fighter = p2; eb2.slider = m2;
        }

        static void CreateStateText(Transform root, FighterController f, bool left) {
            var anchor = new Vector2(left ? 0f : 1f, 1f);
            var pos = new Vector2(left ? 120f : -120f, -110f);
            var t = CreateText(root, "", pos, anchor, 18, left ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
            t.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            var bind = t.gameObject.AddComponent<StateTextBinder>(); bind.fighter = f; bind.text = t; bind.format = left ? "P1 {state} {move}" : "P2 {state} {move}";
        }

        static void CreateValueTexts(Transform root, Slider bar, FighterController f, bool left) {
            var anchor = new Vector2(left ? 0f : 1f, 1f);
            var pos = new Vector2(left ? 370f : -370f, -72f);
            var t = CreateText(root, "", pos, anchor, 16, left ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
            t.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            var binder = t.gameObject.AddComponent<HpTextBinder>(); binder.fighter = f; binder.text = t;
        }

        static void CreateDualDebugHud(Transform root, FighterController p1, FighterController p2, out Text stateText, out Text comboText) {
            var hudGO = new GameObject("DebugHUD"); var hud = hudGO.AddComponent<DebugHUD>(); hud.showDetails = false;
            stateText = CreateText(root, "", new Vector2(0, -160), new Vector2(0.5f, 1), 22, TextAnchor.UpperCenter);
            stateText.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            hud.stateText = stateText;
            hud.fighterP1 = p1;
            hud.fighterP2 = p2;
            comboText = CreateText(root, "", new Vector2(0, -130), new Vector2(0.5f,1), 34, TextAnchor.UpperCenter);
            comboText.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            var comboBinder = comboText.gameObject.AddComponent<ComboCounterBinder>(); comboBinder.text = comboText;
        }

        static Text CreateResultOverlay(Transform root) {
            var txt = CreateText(root, "", new Vector2(0, -220), new Vector2(0.5f,0.5f), 48, TextAnchor.MiddleCenter);
            txt.gameObject.AddComponent<SafeAreaClamp>().margin = new Vector2(24,24);
            return txt;
        }

        static Slider CreateSlider(Transform parent, Vector2 anchoredPos, Vector2 anchor, Color fillColor) {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(320, 22); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = anchoredPos;
            var bg = go.GetComponent<Image>(); bg.color = new Color(0,0,0,0.5f);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            var fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = new Vector2(0,0.25f); fa.anchorMax = new Vector2(1,0.75f); fa.offsetMin = fa.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = fillColor;
            var slider = go.GetComponent<Slider>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            return slider;
        }

        static Text CreateText(Transform parent, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize = 18, TextAnchor align = TextAnchor.UpperCenter) {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(800, 60); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = anchoredPos;
            var t = go.GetComponent<Text>(); t.text = content; t.font = GetDefaultFont(); t.alignment = align; t.color = Color.white; t.fontSize = fontSize;
            var outline = go.GetComponent<Outline>(); outline.effectColor = new Color(0,0,0,0.8f); outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        static Font GetDefaultFont() {
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