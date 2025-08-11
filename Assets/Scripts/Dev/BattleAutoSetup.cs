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
            CreateFighters(out var p1, out var p2);
            p1.opponent = p2.transform; p2.opponent = p1.transform;
            if (createUI) CreateUI(p1, p2);
            Debug.Log("BattleAutoSetup initialized test battle. Press Play and use: Move A/D, Jump Space/W, Crouch S, Light J, Heavy K, Block Shift, Dodge L/Ctrl");
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
            }
        }

        void CreateGround() {
            var g = new GameObject("Ground");
            var col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(arenaHalfExtents.x * 2f, 0.5f);
            g.transform.position = new Vector3(0f, -1.5f, 0f);
            g.layer = LayerMask.NameToLayer("Default");
        }

        void CreateFighters(out FighterController p1, out FighterController p2) {
            p1 = CreateFighter("Player", new Vector3(-2f, -1f, 0f), isPlayer: true);
            p2 = CreateFighter("Enemy", new Vector3(2f, -1f, 0f), isPlayer: false);
        }

        FighterController CreateFighter(string name, Vector3 pos, bool isPlayer) {
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

            // Moves
            var light = ScriptableObject.CreateInstance<MoveData>();
            light.moveId = "5L"; light.triggerName = "Light"; light.startup = 0.083f; light.active = 0.05f; light.recovery = 0.166f;
            light.damage = 8; light.hitstun = 0.1f; light.blockstun = 0.066f; light.hitstopOnHit = 0.1f; light.hitstopOnBlock = 0.066f;
            light.knockback = new Vector2(2f, 2f); light.pushbackOnHit = 0.4f; light.pushbackOnBlock = 0.6f; light.meterOnHit = 50; light.meterOnBlock = 20;

            var heavy = ScriptableObject.CreateInstance<MoveData>();
            heavy.moveId = "5H"; heavy.triggerName = "Heavy"; heavy.startup = 0.2f; heavy.active = 0.066f; heavy.recovery = 0.3f;
            heavy.damage = 18; heavy.hitstun = 0.233f; heavy.blockstun = 0.1f; heavy.hitstopOnHit = 0.166f; heavy.hitstopOnBlock = 0.1f;
            heavy.knockback = new Vector2(3f, 3f); heavy.pushbackOnHit = 1.0f; heavy.pushbackOnBlock = 1.2f; heavy.meterOnHit = 100; heavy.meterOnBlock = 40;

            var set = ScriptableObject.CreateInstance<MoveSet>();
            set.entries = new MoveSet.Entry[] {
                new MoveSet.Entry { triggerName = "Light", move = light },
                new MoveSet.Entry { triggerName = "Heavy", move = heavy },
            };
            fc.moveSet = set;

            // Add IO
            if (isPlayer) go.AddComponent<PlayerInputBridge>(); else {
                var ai = go.AddComponent<SimpleAIController>();
                var normal = ScriptableObject.CreateInstance<AIConfig>();
                normal.blockProbability = 0.2f; normal.attackCooldownRange = new Vector2(0.6f, 1.2f); normal.approachDistance = 2.2f; normal.retreatDistance = 1.0f;
                ai.normal = normal; ai.easy = normal; ai.hard = normal;
                go.name += "(AI)";
            }

            // Hurtboxes
            var hurtRoot = new GameObject("Hurtboxes"); hurtRoot.transform.SetParent(go.transform, false);
            fc.hurtboxes = new Hurtbox[3];
            fc.hurtboxes[0] = CreateHurtbox(hurtRoot.transform, "Head", HurtRegion.Head, new Vector2(0.4f, 0.5f), new Vector2(0, 0.9f));
            fc.hurtboxes[1] = CreateHurtbox(hurtRoot.transform, "Torso", HurtRegion.Torso, new Vector2(0.5f, 0.8f), new Vector2(0, 0.3f));
            fc.hurtboxes[2] = CreateHurtbox(hurtRoot.transform, "Legs", HurtRegion.Legs, new Vector2(0.5f, 0.6f), new Vector2(0, -0.4f));
            foreach (var hb in fc.hurtboxes) hb.owner = fc;

            // Hitboxes
            var hitRoot = new GameObject("Hitboxes"); hitRoot.transform.SetParent(go.transform, false);
            fc.hitboxes = new Hitbox[2];
            fc.hitboxes[0] = CreateHitbox(hitRoot.transform, "Light1", new Vector2(0.6f, 0.4f), new Vector2(0.8f, 0.4f));
            fc.hitboxes[1] = CreateHitbox(hitRoot.transform, "Heavy1", new Vector2(0.8f, 0.5f), new Vector2(1.0f, 0.5f));
            foreach (var hx in fc.hitboxes) { hx.owner = fc; hx.active = false; }

            return fc;
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
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var hp1 = CreateSlider(canvas.transform, new Vector2(200, -30), new Vector2(0, 1));
            var hp2 = CreateSlider(canvas.transform, new Vector2(-200, -30), new Vector2(1, 1)); hp2.direction = Slider.Direction.RightToLeft;
            var meter1 = CreateSlider(canvas.transform, new Vector2(200, -60), new Vector2(0, 1));
            var meter2 = CreateSlider(canvas.transform, new Vector2(-200, -60), new Vector2(1, 1)); meter2.direction = Slider.Direction.RightToLeft;
            var timer = CreateText(canvas.transform, "60", new Vector2(0, -30), new Vector2(0.5f, 1));
            var hint = CreateText(canvas.transform, "Move: A/D  Jump: Space/W  Crouch: S  Block: Left Shift  Dodge: L/Ctrl  Light: J  Heavy: K", new Vector2(10, 10), new Vector2(0, 0));
            hint.alignment = TextAnchor.LowerLeft;

            var rmGO = new GameObject("RoundManager"); var rm = rmGO.AddComponent<RoundManager>();
            rm.p1 = p1; rm.p2 = p2; rm.p1Hp = hp1; rm.p2Hp = hp2; rm.timerText = timer;

            var eb1 = hp1.gameObject.AddComponent<UI.EnergyBarBinder>(); eb1.fighter = p1; eb1.slider = meter1;
            var eb2 = hp2.gameObject.AddComponent<UI.EnergyBarBinder>(); eb2.fighter = p2; eb2.slider = meter2;

            var hudGO = new GameObject("DebugHUD"); var hud = hudGO.AddComponent<UI.DebugHUD>(); hud.fighter = p1;
            var st = CreateText(canvas.transform, "", new Vector2(10, -10), new Vector2(0, 1)); hud.stateText = st; // simple binding
        }

        Slider CreateSlider(Transform parent, Vector2 anchoredPos, Vector2 anchor) {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200, 16); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.anchoredPosition = anchoredPos;
            var bg = go.GetComponent<Image>(); bg.color = new Color(0,0,0,0.5f);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            var fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = new Vector2(0,0.25f); fa.anchorMax = new Vector2(1,0.75f); fa.offsetMin = fa.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
            var slider = go.GetComponent<Slider>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            return slider;
        }

        Text CreateText(Transform parent, string content, Vector2 anchoredPos, Vector2 anchor) {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(400, 40); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.anchoredPosition = anchoredPos;
            var t = go.GetComponent<Text>(); t.text = content; t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.alignment = TextAnchor.UpperCenter; t.color = Color.white;
            return t;
        }
    }
}