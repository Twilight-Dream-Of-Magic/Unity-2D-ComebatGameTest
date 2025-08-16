using UnityEngine;
using FightingGame.Combat.Actors;

namespace Dev {
    /// <summary>
    /// Factory for creating a fully playable fighter GameObject with the essential components attached.
    /// 职责：只负责“创建与装配”角色本体（物理、渲染、受击/命中盒、动作数据、输入大脑）。
    /// 注意：资源（HP/Meter）归 RoundManager 统一管理，本类不附加 FighterResources。
    /// </summary>
    public static class FighterFactory {
        /// <summary>
        /// Creates a fighter at a position with color and brain type.
        /// 仅用于开发搭建：正式关卡应改为读取关卡配置/预制体。
        /// </summary>
        public static FighterActor CreateFighter(string name, Vector3 position, Color color, bool isPlayer, Data.InputTuningConfig inputTuning) {
            // Root object
            var fighterObject = new GameObject(name);
            fighterObject.transform.position = position;

            // Physics capsule
            var rigidbody2D = fighterObject.AddComponent<Rigidbody2D>();
            rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
            var capsuleCollider = fighterObject.AddComponent<CapsuleCollider2D>();
            capsuleCollider.direction = CapsuleDirection2D.Vertical;
            capsuleCollider.size = new Vector2(0.6f, 1.8f);

            // Core actor and required modules
            var controller = fighterObject.AddComponent<FighterActor>();
            controller.team = isPlayer ? FighterTeam.Player : FighterTeam.AI;
            fighterObject.AddComponent<Fighter.Core.FighterLocomotion>();
            fighterObject.AddComponent<Fighter.Core.CriticalAttackExecutor>();
            fighterObject.AddComponent<Fighter.Core.DamageReceiver>();
            // FighterResources 由 RoundManager 统一管理/持有，不在此附加。

            // Base stats（数值仅用于演示，真实项目应由数据表驱动）
            var stats = ScriptableObject.CreateInstance<Fighter.FighterStats>();
            stats.maxHealth = 20000;
            stats.walkSpeed = 6f;
            stats.jumpForce = 12f;
            stats.gravityScale = 4f;
            stats.blockDamageRatio = 0.2f;
            stats.dodgeDuration = 0.25f;
            stats.dodgeInvuln = 0.2f;
            stats.hitStop = 0.06f;
            controller.stats = stats;
            controller.maxMeter = 2000;

            // Animator（若不存在则补齐）
            Animator animator;
            if (!fighterObject.TryGetComponent<Animator>(out animator))
            {
                animator = fighterObject.AddComponent<Animator>();
            }

            // Simple visual (solid color sprite) 仅供白盒调试
            var visual = new GameObject("Visual");
            visual.transform.SetParent(fighterObject.transform, false);
            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSolidSprite(color);
            visual.transform.localScale = new Vector3(0.8f, 1.8f, 1f);
            // Anchor for head state label (top of visual)
            var headAnchor = new GameObject("HeadStateAnchor");
            headAnchor.transform.SetParent(fighterObject.transform, false);
            headAnchor.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            headAnchor.AddComponent<Fighter.Core.HeadStateAnchor>();

            // Minimal move set（便于验证状态机/命中/受击全链路）
            var light = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
            light.moveId = "Light";
            light.triggerName = "Light";
            light.startup = 0.05f;    // 3f 活动前摇
            light.active = 0.04f;     // 2-3f 活动
            light.recovery = 0.12f;   // 收招
            light.damage = 8;
            light.hitstun = 0.12f;
            light.blockstun = 0.08f;
            light.hitstopOnHit = 0.06f;
            light.hitstopOnBlock = 0.04f;
            light.knockback = new Vector2(2.2f, 1.8f);
            light.pushbackOnHit = 0.35f;
            light.pushbackOnBlock = 0.5f;
            light.meterOnHit = 50;
            light.meterOnBlock = 20;
            light.canCancelOnHit = true;
            light.canCancelOnBlock = true;
            light.canCancelOnWhiff = true;
            light.onWhiffCancelWindow = new Vector2(0.0f, 0.12f);
            light.onHitCancelWindow = new Vector2(0.0f, 0.25f);
            light.onBlockCancelWindow = new Vector2(0.0f, 0.18f);
            light.cancelIntoTriggers = new[] { "Light", "Heavy", "Super" };

            var heavy = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
            heavy.moveId = "Heavy";
            heavy.triggerName = "Heavy";
            heavy.startup = 0.12f;
            heavy.active = 0.05f;
            heavy.recovery = 0.22f;
            heavy.damage = 18;
            heavy.hitstun = 0.2f;
            heavy.blockstun = 0.12f;
            heavy.hitstopOnHit = 0.1f;
            heavy.hitstopOnBlock = 0.06f;
            heavy.knockback = new Vector2(3.2f, 2.2f);
            heavy.pushbackOnHit = 0.9f;
            heavy.pushbackOnBlock = 1.0f;
            heavy.meterOnHit = 90;
            heavy.meterOnBlock = 40;
            heavy.canCancelOnHit = true;
            heavy.canCancelOnBlock = false;
            heavy.knockdownKind = FightingGame.Combat.KnockdownKind.Soft;
            heavy.cancelIntoTriggers = new[] { "Super" };

            var moveSet = ScriptableObject.CreateInstance<Data.CombatActionSet>();
            moveSet.entries = new Data.CombatActionSet.Entry[]
            {
                new Data.CombatActionSet.Entry { triggerName = "Light", actionDefinition = light },
                new Data.CombatActionSet.Entry { triggerName = "Heavy", actionDefinition = heavy },
                new Data.CombatActionSet.Entry { triggerName = "Super", actionDefinition = CreateSuper() },
                new Data.CombatActionSet.Entry { triggerName = "Heal",  actionDefinition = CreateHeal()  },
            };
            controller.actionSet = moveSet;

            // Hurtboxes（站/蹲/空中启用由 DamageReceiver/状态控制）
            var hurtboxesRoot = new GameObject("Hurtboxes");
            hurtboxesRoot.transform.SetParent(fighterObject.transform, false);
            controller.hurtboxes = new FightingGame.Combat.Hurtbox[3];
            controller.hurtboxes[0] = CreateHurtbox(hurtboxesRoot.transform, "Head",  FightingGame.Combat.HurtRegion.Head,  new Vector2(0.6f, 0.7f), new Vector2(0,  1.0f));
            controller.hurtboxes[1] = CreateHurtbox(hurtboxesRoot.transform, "Torso", FightingGame.Combat.HurtRegion.Torso, new Vector2(0.7f, 1.0f), new Vector2(0,  0.3f));
            controller.hurtboxes[2] = CreateHurtbox(hurtboxesRoot.transform, "Legs",  FightingGame.Combat.HurtRegion.Legs,  new Vector2(0.7f, 0.8f), new Vector2(0, -0.5f));
            for (int i = 0; i < controller.hurtboxes.Length; i++)
            {
                var hurtbox = controller.hurtboxes[i];
                if (hurtbox != null)
                {
                    hurtbox.owner = controller;
                }
            }

            // Hitboxes（基于 Actor 身高设定默认高度），实际项目应和动画帧精确对齐
            var hitboxesRoot = new GameObject("Hitboxes");
            hitboxesRoot.transform.SetParent(fighterObject.transform, false);
            controller.hitboxes = new FightingGame.Combat.Hitbox[2];
            controller.hitboxes[0] = CreateHitbox(hitboxesRoot.transform, "Light1", new Vector2(1.5f, 1.1f), new Vector2(1.25f, 0.5f));
            controller.hitboxes[1] = CreateHitbox(hitboxesRoot.transform, "Heavy1", new Vector2(1.8f, 1.1f), new Vector2(1.6f, 0.5f));
            for (int i = 0; i < controller.hitboxes.Length; i++)
            {
                var hb = controller.hitboxes[i];
                if (hb != null)
                {
                    hb.owner = controller;
                    hb.active = false;
                }
            }

            // Attach brain（AI/玩家）
            if (isPlayer)
            {
                var brain = controller.gameObject.AddComponent<Fighter.InputSystem.PlayerBrain>();
                brain.fighter = controller;
                brain.inputTuning = inputTuning;
            }
            else
            {
                var brain = controller.gameObject.AddComponent<Fighter.InputSystem.AIBrain>();
                brain.fighter = controller;
                brain.inputTuning = inputTuning;
            }

            return controller;
        }

        /// <summary>
        /// Creates a hurtbox box collider and component under a parent.
        /// 说明：受击盒沿 X 放宽 35% 提高白盒调试的容错率。
        /// </summary>
        static FightingGame.Combat.Hurtbox CreateHurtbox(Transform parent, string name, FightingGame.Combat.HurtRegion region, Vector2 size, Vector2 offset) {
            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            var hurtbox = childObject.AddComponent<FightingGame.Combat.Hurtbox>();
            hurtbox.region = region;
            // 交由 Hurtbox 自管理 collider 尺寸/偏移
            hurtbox.desiredSize = new Vector2(size.x * 1.35f, size.y);
            hurtbox.desiredOffset = offset;
            hurtbox.ensureTrigger = true;
            return hurtbox;
        }

        /// <summary>
        /// Creates a hitbox with height aligned to character, X 长度由参数决定。
        /// </summary>
        static FightingGame.Combat.Hitbox CreateHitbox(Transform parent, string name, Vector2 size, Vector2 offset) {
            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            var hitbox = childObject.AddComponent<FightingGame.Combat.Hitbox>();
            hitbox.active = false;
            // 將尺寸/偏移交由 Hitbox 自己管理
            // 構建時將 X 長度縮短 3f
            hitbox.desiredSize = new Vector2(Mathf.Max(0.1f, size.x - 3f), size.y);
            hitbox.desiredOffset = new Vector2(offset.x, 0f);
            hitbox.autoHeightByCapsule = true;
            hitbox.heightFallback = size.y;
            return hitbox;
        }

        /// <summary>
        /// Creates a solid color 1x1 sprite（仅用于白盒调试）。
        /// </summary>
        static Sprite CreateSolidSprite(Color color) {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>
        /// Demo super move data.
        /// </summary>
        static Data.CombatActionDefinition CreateSuper() {
            var actionDefinition = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
            actionDefinition.moveId = "Super";
            actionDefinition.triggerName = "Super";
            actionDefinition.startup = 0.14f;
            actionDefinition.active = 0.08f;
            actionDefinition.recovery = 0.28f;
            actionDefinition.damage = 28;
            actionDefinition.hitstun = 0.28f;
            actionDefinition.blockstun = 0.16f;
            actionDefinition.knockback = new Vector2(4.2f, 2.8f);
            actionDefinition.pushbackOnHit = 1.4f;
            actionDefinition.pushbackOnBlock = 1.6f;
            actionDefinition.hitstopOnHit = 0.12f;
            actionDefinition.hitstopOnBlock = 0.08f;
            actionDefinition.meterOnHit = 120;
            actionDefinition.meterOnBlock = 50;
            actionDefinition.meterCost = 500;
            return actionDefinition;
        }

        /// <summary>
        /// Demo heal move data（用于验证治療執行器/資源增減流）。
        /// </summary>
        static Data.CombatActionDefinition CreateHeal() {
            var actionDefinition = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
            actionDefinition.moveId = "Heal";
            actionDefinition.triggerName = "Heal";
            actionDefinition.startup = 0.08f;
            actionDefinition.active = 0.00f;
            actionDefinition.recovery = 0.22f;
            actionDefinition.damage = 0;
            actionDefinition.hitstun = 0f;
            actionDefinition.blockstun = 0f;
            actionDefinition.knockback = Vector2.zero;
            actionDefinition.pushbackOnHit = 0f;
            actionDefinition.pushbackOnBlock = 0f;
            actionDefinition.hitstopOnHit = 0f;
            actionDefinition.hitstopOnBlock = 0f;
            actionDefinition.meterOnHit = 0;
            actionDefinition.meterOnBlock = 0;
            actionDefinition.meterCost = 300;
            actionDefinition.healAmount = 20;
            return actionDefinition;
        }
    }
}