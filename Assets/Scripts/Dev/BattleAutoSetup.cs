using UnityEngine;
using UnityEngine.UI;
using Fighter;
using Fighter.AI;
using Systems;
using Combat;
using Data;

namespace Dev {
    public class BattleAutoSetup : MonoBehaviour {
        public bool createManagers = true;
        public bool createUI = true;
        public bool createGround = true;
        public Vector2 arenaHalfExtents = new Vector2(8f, 3f);

        private void Start() {
            if (createManagers) EnsureManagers();
            if (createGround) CreateGround();
            var p1 = CreatePlayerFighter(new Vector3(-1.6f, -1f, 0f));
            var p2 = CreateAIFighter(new Vector3(1.6f, -1f, 0f));
            LinkOpponents(p1, p2);
            if (createUI) CreateUI(p1, p2);
            Debug.Log("BattleAutoSetup ready: A/D move, Space jump, S crouch, J/K attack, Shift block, L dodge");
        }

        void EnsureManagers() {
            if (!FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();
            if (!FindObjectOfType<FrameClock>()) new GameObject("FrameClock").AddComponent<FrameClock>();
            if (!FindObjectOfType<AudioManager>()) {
                var go = new GameObject("AudioManager");
                var am = go.AddComponent<AudioManager>();
                am.bgmSource = go.AddComponent<AudioSource>();
                am.sfxSource = go.AddComponent<AudioSource>();
            }
            if (!FindObjectOfType<CameraShaker>()) {
                var cam = Camera.main ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
                cam.tag = "MainCamera";
                if (!cam.GetComponent<CameraShaker>()) cam.AddComponent<CameraShaker>();
                var c = cam.GetComponent<Camera>(); c.orthographic = true; c.orthographicSize = 3.5f;
                cam.transform.position = new Vector3(0, 0, -10);
            }
            if (!FindObjectOfType<HitEffectManager>()) new GameObject("HitEffectManager").AddComponent<HitEffectManager>();
            if (!FindObjectOfType<ComboCounter>()) new GameObject("ComboCounter").AddComponent<ComboCounter>();
        }

        void CreateGround() {
            var g = new GameObject("Ground");
            var col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(arenaHalfExtents.x * 2f, 0.5f);
            g.transform.position = new Vector3(0f, -1.5f, 0f);
            g.layer = LayerMask.NameToLayer("Default");
            var vis = new GameObject("Visual"); vis.transform.SetParent(g.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>(); sr.sprite = CreateSolidSprite(new Color(0.15f, 0.6f, 0.15f, 1f));
            vis.transform.localScale = new Vector3(col.size.x, col.size.y, 1f);

            // add side walls to prevent falling when pushed to corners
            float wallX = arenaHalfExtents.x;
            float wallHeight = 6f;
            float wallWidth = 0.5f;
            var left = new GameObject("WallLeft"); left.transform.position = new Vector3(-wallX - wallWidth * 0.5f, -1.0f, 0f);
            var leftCol = left.AddComponent<BoxCollider2D>(); leftCol.size = new Vector2(wallWidth, wallHeight);
            var right = new GameObject("WallRight"); right.transform.position = new Vector3(wallX + wallWidth * 0.5f, -1.0f, 0f);
            var rightCol = right.AddComponent<BoxCollider2D>(); rightCol.size = new Vector2(wallWidth, wallHeight);
        }

        FighterController CreatePlayerFighter(Vector3 pos) {
            var fc = CreateFighterCore("Player", pos, new Color(0.2f, 0.6f, 1f, 1f));
            fc.team = FighterTeam.Player;
            var ib = fc.gameObject.AddComponent<PlayerController>();
            ib.fighter = fc;
            AttachSpecials(fc);
            return fc;
        }

        FighterController CreateAIFighter(Vector3 pos) {
            var fc = CreateFighterCore("Enemy(AI)", pos, new Color(1f, 0.4f, 0.3f, 1f));
            fc.team = FighterTeam.AI;
            var ai = fc.gameObject.AddComponent<OpponentAIController>();
            ai.fighter = fc;
            var normal = ScriptableObject.CreateInstance<AIConfig>();
            normal.blockProbability = 0.2f; normal.attackCooldownRange = new Vector2(0.6f, 1.2f); normal.approachDistance = 2.2f; normal.retreatDistance = 1.0f;
            ai.normal = normal; ai.easy = normal; ai.hard = normal;
            AttachSpecials(fc);
            return fc;
        }

        FighterController CreateFighterCore(string name, Vector3 pos, Color color) {
            var go = new GameObject(name);
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            var cap = go.AddComponent<CapsuleCollider2D>(); cap.direction = CapsuleDirection2D.Vertical; cap.size = new Vector2(0.6f, 1.8f);

            var fc = go.AddComponent<FighterController>();
            var stats = ScriptableObject.CreateInstance<FighterStats>();
            stats.maxHealth = 100;
            stats.walkSpeed = 6f; stats.jumpForce = 12f; stats.gravityScale = 4f;
            stats.blockDamageRatio = 0.2f; stats.dodgeDuration = 0.25f; stats.dodgeInvuln = 0.2f; stats.hitStop = 0.06f;
            fc.stats = stats;

            // Ensure Animator exists
            if (!go.TryGetComponent<Animator>(out var anim)) anim = go.AddComponent<Animator>();

            var vis = new GameObject("Visual"); vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>(); sr.sprite = CreateSolidSprite(color);
            vis.transform.localScale = new Vector3(0.8f, 1.8f, 1f);

            // Moves (tuning: faster startup/active for snappier response)
            var light = ScriptableObject.CreateInstance<MoveData>();
            light.moveId = "5L"; light.triggerName = "Light"; light.startup = 0.05f; light.active = 0.04f; light.recovery = 0.12f;
            light.damage = 8; light.hitstun = 0.12f; light.blockstun = 0.08f; light.hitstopOnHit = 0.06f; light.hitstopOnBlock = 0.04f;
            light.knockback = new Vector2(2.2f, 1.8f); light.pushbackOnHit = 0.35f; light.pushbackOnBlock = 0.5f; light.meterOnHit = 50; light.meterOnBlock = 20;

            var heavy = ScriptableObject.CreateInstance<MoveData>();
            heavy.moveId = "5H"; heavy.triggerName = "Heavy"; heavy.startup = 0.12f; heavy.active = 0.05f; heavy.recovery = 0.22f;
            heavy.damage = 18; heavy.hitstun = 0.2f; heavy.blockstun = 0.12f; heavy.hitstopOnHit = 0.1f; heavy.hitstopOnBlock = 0.06f;
            heavy.knockback = new Vector2(3.2f, 2.2f); heavy.pushbackOnHit = 0.9f; heavy.pushbackOnBlock = 1.0f; heavy.meterOnHit = 90; heavy.meterOnBlock = 40;

            var set = ScriptableObject.CreateInstance<MoveSet>();
            set.entries = new MoveSet.Entry[] {
                new MoveSet.Entry { triggerName = "Light", move = light },
                new MoveSet.Entry { triggerName = "Heavy", move = heavy },
                new MoveSet.Entry { triggerName = "Super", move = CreateSuper() },
                new MoveSet.Entry { triggerName = "Heal", move = CreateHeal() },
            };
            fc.moveSet = set;

            var hurtRoot = new GameObject("Hurtboxes"); hurtRoot.transform.SetParent(go.transform, false);
            fc.hurtboxes = new Hurtbox[3];
            fc.hurtboxes[0] = CreateHurtbox(hurtRoot.transform, "Head", HurtRegion.Head, new Vector2(0.4f, 0.5f), new Vector2(0, 0.9f));
            fc.hurtboxes[1] = CreateHurtbox(hurtRoot.transform, "Torso", HurtRegion.Torso, new Vector2(0.5f, 0.8f), new Vector2(0, 0.3f));
            fc.hurtboxes[2] = CreateHurtbox(hurtRoot.transform, "Legs", HurtRegion.Legs, new Vector2(0.5f, 0.6f), new Vector2(0, -0.4f));
            foreach (var hb in fc.hurtboxes) hb.owner = fc;

            // Hitboxes (larger and further reach to ensure contact)
            var hitRoot = new GameObject("Hitboxes"); hitRoot.transform.SetParent(go.transform, false);
            fc.hitboxes = new Hitbox[2];
            fc.hitboxes[0] = CreateHitbox(hitRoot.transform, "Light1", new Vector2(1.3f, 0.6f), new Vector2(1.4f, 0.6f));
            fc.hitboxes[1] = CreateHitbox(hitRoot.transform, "Heavy1", new Vector2(1.5f, 0.7f), new Vector2(1.7f, 0.7f));
            foreach (var hx in fc.hitboxes) { hx.owner = fc; hx.active = false; }

            return fc;
        }

        void LinkOpponents(FighterController p1, FighterController p2) {
            p1.opponent = p2.transform; p2.opponent = p1.transform;
        }

        Sprite CreateSolidSprite(Color c) {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, c); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        Hurtbox CreateHurtbox(Transform parent, string name, HurtRegion region, Vector2 size, Vector2 offset) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = size; col.offset = offset;
            var hb = go.AddComponent<Hurtbox>(); hb.region = region;
            return hb;
        }

        Hitbox CreateHitbox(Transform parent, string name, Vector2 size, Vector2 offset) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = size; col.offset = offset;
            var hb = go.AddComponent<Hitbox>(); hb.active = false;
            return hb;
        }

        void CreateUI(FighterController p1, FighterController p2) {
            var canvas = CreateCanvas(out var scaler);
            var hp1 = CreateHpSlider(canvas.transform, true);
            var hp2 = CreateHpSlider(canvas.transform, false);
            var meter1 = CreateMeterSlider(canvas.transform, true);
            var meter2 = CreateMeterSlider(canvas.transform, false);
            var timer = CreateTimer(canvas.transform);
            var hint = CreateHint(canvas.transform);
            BindHpAndMeter(p1, p2, hp1, hp2, meter1, meter2);
            CreateDualDebugHud(canvas.transform, p1, p2);
            CreateResultOverlay(canvas.transform);
            CreateRoundManager(p1, p2, hp1, hp2, timer);
        }

        Canvas CreateCanvas(out CanvasScaler scaler) {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            return canvas;
        }

        Slider CreateHpSlider(Transform root, bool left) {
            var pos = left ? new Vector2(260, -40) : new Vector2(-260, -40);
            var anchor = left ? new Vector2(0,1) : new Vector2(1,1);
            var color = left ? new Color(0.2f,0.6f,1f,0.95f) : new Color(1f,0.35f,0.35f,0.95f);
            var s = CreateSlider(root, pos, anchor, color);
            if (!left) s.direction = Slider.Direction.RightToLeft;
            return s;
        }

        Slider CreateMeterSlider(Transform root, bool left) {
            var pos = left ? new Vector2(260, -80) : new Vector2(-260, -80);
            var anchor = left ? new Vector2(0,1) : new Vector2(1,1);
            var s = CreateSlider(root, pos, anchor, new Color(0.2f,0.8f,0.2f,0.9f));
            if (!left) s.direction = Slider.Direction.RightToLeft;
            return s;
        }

        Text CreateTimer(Transform root) => CreateText(root, "60", new Vector2(0, -40), new Vector2(0.5f,1), 28, TextAnchor.UpperCenter);
        Text CreateHint(Transform root) => CreateText(root, "Move: A/D  Jump: Space  Crouch: S  Block: Left Shift  Dodge: L  Light: J  Heavy: K", new Vector2(0, 30), new Vector2(0.5f,0), 20, TextAnchor.LowerCenter);

        void BindHpAndMeter(FighterController p1, FighterController p2, Slider hp1, Slider hp2, Slider m1, Slider m2) {
            var p1Val = CreateText(hp1.transform.parent, "100", new Vector2(370, -72), new Vector2(0,1), 16, TextAnchor.UpperLeft);
            var p2Val = CreateText(hp2.transform.parent, "100", new Vector2(-370, -72), new Vector2(1,1), 16, TextAnchor.UpperRight);
            var p1Binder = p1Val.gameObject.AddComponent<UI.HpTextBinder>(); p1Binder.fighter = p1; p1Binder.text = p1Val;
            var p2Binder = p2Val.gameObject.AddComponent<UI.HpTextBinder>(); p2Binder.fighter = p2; p2Binder.text = p2Val;
            var p1Flash = hp1.gameObject.AddComponent<UI.HpFlashOnDamage>(); p1Flash.fighter = p1; p1Flash.slider = hp1; p1Flash.flashColor = Color.white;
            var p2Flash = hp2.gameObject.AddComponent<UI.HpFlashOnDamage>(); p2Flash.fighter = p2; p2Flash.slider = hp2; p2Flash.flashColor = Color.white;
            var eb1 = hp1.gameObject.AddComponent<UI.EnergyBarBinder>(); eb1.fighter = p1; eb1.slider = m1;
            var eb2 = hp2.gameObject.AddComponent<UI.EnergyBarBinder>(); eb2.fighter = p2; eb2.slider = m2;
        }

        void CreateDualDebugHud(Transform root, FighterController p1, FighterController p2) {
            var hudGO = new GameObject("DebugHUD"); var hud = hudGO.AddComponent<UI.DebugHUD>(); hud.showDetails = false;
            var st = CreateText(root, "", new Vector2(0, -160), new Vector2(0.5f, 1), 22, TextAnchor.UpperCenter);
            hud.stateText = st;
            hud.fighterP1 = p1;
            hud.fighterP2 = p2;
            var comboText = CreateText(root, "", new Vector2(0, -130), new Vector2(0.5f,1), 34, TextAnchor.UpperCenter);
            var comboBinder = comboText.gameObject.AddComponent<UI.ComboCounterBinder>(); comboBinder.text = comboText;
        }

        // removed old per-player status texts; merged into DebugHUD center

        void CreateResultOverlay(Transform root) {
            var txt = CreateText(root, "", new Vector2(0, -220), new Vector2(0.5f,0.5f), 48, TextAnchor.MiddleCenter);
            var rm = FindObjectOfType<RoundManager>();
            if (rm) rm.resultText = txt;
        }

        void CreateRoundManager(FighterController p1, FighterController p2, Slider hp1, Slider hp2, Text timer) {
            var rmGO = new GameObject("RoundManager"); var rm = rmGO.AddComponent<RoundManager>();
            rm.p1 = p1; rm.p2 = p2; rm.p1Hp = hp1; rm.p2Hp = hp2; rm.timerText = timer;
        }

        Slider CreateSlider(Transform parent, Vector2 anchoredPos, Vector2 anchor, Color fillColor) {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(320, 22); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.anchoredPosition = anchoredPos;
            var bg = go.GetComponent<Image>(); bg.color = new Color(0,0,0,0.5f);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            var fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = new Vector2(0,0.25f); fa.anchorMax = new Vector2(1,0.75f); fa.offsetMin = fa.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = fillColor;
            var slider = go.GetComponent<Slider>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            return slider;
        }

        Text CreateText(Transform parent, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize = 18, TextAnchor align = TextAnchor.UpperCenter) {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(800, 60); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.anchoredPosition = anchoredPos;
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

        MoveData CreateSuper() {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.moveId = "Super"; m.triggerName = "Super";
            m.startup = 0.14f; m.active = 0.08f; m.recovery = 0.28f;
            m.damage = 28; m.hitstun = 0.28f; m.blockstun = 0.16f;
            m.knockback = new Vector2(4.2f, 2.8f); m.pushbackOnHit = 1.4f; m.pushbackOnBlock = 1.6f;
            m.hitstopOnHit = 0.12f; m.hitstopOnBlock = 0.08f;
            m.meterOnHit = 120; m.meterOnBlock = 50; m.meterCost = 500;
            return m;
        }

        MoveData CreateHeal() {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.moveId = "Heal"; m.triggerName = "Heal";
            m.startup = 0.08f; m.active = 0.00f; m.recovery = 0.22f;
            m.damage = 0; m.hitstun = 0f; m.blockstun = 0f;
            m.knockback = Vector2.zero; m.pushbackOnHit = 0f; m.pushbackOnBlock = 0f;
            m.hitstopOnHit = 0f; m.hitstopOnBlock = 0f;
            m.meterOnHit = 0; m.meterOnBlock = 0; m.meterCost = 300; m.healAmount = 20;
            return m;
        }

        void AttachSpecials(FighterController fc) {
            var set = ScriptableObject.CreateInstance<Data.SpecialMoveSet>();
            set.specials = new Data.SpecialMoveSet.SpecialEntry[] {
                new Data.SpecialMoveSet.SpecialEntry { name = "Super", sequence = new Combat.CommandToken[]{ Combat.CommandToken.Down, Combat.CommandToken.Forward, Combat.CommandToken.Heavy }, maxWindowSeconds = 0.6f, triggerName = "Super" },
                new Data.SpecialMoveSet.SpecialEntry { name = "Heal", sequence = new Combat.CommandToken[]{ Combat.CommandToken.Down, Combat.CommandToken.Down, Combat.CommandToken.Light }, maxWindowSeconds = 0.6f, triggerName = "Heal" },
            };
            var resolver = fc.gameObject.AddComponent<Fighter.SpecialInputResolver>();
            resolver.fighter = fc; resolver.specialSet = set;
        }
    }
}