using UnityEngine;
using FightingGame.Combat.Actors;

namespace Dev
{
	/// <summary>
	/// Factory for creating a fully playable fighter GameObject with the essential components attached.
	/// 職責：只負責角色本體的創建與組裝（物理、渲染、受擊/命中盒、動作資料、輸入大腦）。
	/// 注意：資源（HP/Meter）由 RoundManager 統一管理，本類不附加 FighterResources。
	/// </summary>
	public static class FighterFactory
	{
		/// <summary>
		/// Creates a fighter at a position with color and brain type.
		/// 僅用於開發搭建：正式關卡應改為讀取關卡配置或預製體。
		/// </summary>
		public static FighterActor CreateFighter(string name, Vector3 position, Color color, bool isPlayer, Data.InputTuningConfig inputTuning)
		{
			// === Root object ===
			var fighterObject = new GameObject(name);
			fighterObject.transform.position = position;

			// === Physics capsule ===
			var rigidbody2D = fighterObject.AddComponent<Rigidbody2D>();
			rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
			var capsuleCollider = fighterObject.AddComponent<CapsuleCollider2D>();
			capsuleCollider.direction = CapsuleDirection2D.Vertical;
			capsuleCollider.size = new Vector2(0.6f, 1.8f);

			// === Core actor and modules ===
			var controller = fighterObject.AddComponent<FighterActor>();
			controller.team = isPlayer ? FighterTeam.Player : FighterTeam.AI;
			fighterObject.AddComponent<Fighter.Core.FighterLocomotion>();
			fighterObject.AddComponent<Fighter.Core.CriticalAttackExecutor>();
			fighterObject.AddComponent<Fighter.Core.DamageReceiver>();
			if (!fighterObject.GetComponent<FightingGame.Combat.SpecialInputResolver>())
			{
				fighterObject.AddComponent<FightingGame.Combat.SpecialInputResolver>();
			}

			// === Base stats (demo only, real project should use data-driven config) ===
			var stats = ScriptableObject.CreateInstance<Fighter.FighterStats>();
			stats.minHealth = 0;
			stats.maxHealth = 5000;
			stats.minMeter = 0;
			stats.maxMeter = 2000;
			stats.walkSpeed = 6f;
			stats.jumpForce = 12f;
			stats.gravityScale = 4f;
			stats.blockDamageRatio = 0.2f;
			stats.dodgeDuration = 0.25f;
			stats.dodgeInvulnerable = 0.2f;
			stats.hitStop = 0.06f;
			controller.stats = stats;
			controller.meter = stats.minMeter;

			// === Animator ===
			Animator animator;
			if (!fighterObject.TryGetComponent<Animator>(out animator))
			{
				animator = fighterObject.AddComponent<Animator>();
			}

			// === Simple visual (solid color sprite) for debug ===
			var visual = new GameObject("Visual");
			visual.transform.SetParent(fighterObject.transform, false);
			var spriteRenderer = visual.AddComponent<SpriteRenderer>();
			spriteRenderer.sprite = CreateSolidSprite(color);
			visual.transform.localScale = new Vector3(0.8f, 1.8f, 1f);

			var headAnchor = new GameObject("HeadStateAnchor");
			headAnchor.transform.SetParent(fighterObject.transform, false);
			headAnchor.transform.localPosition = new Vector3(0f, 1.2f, 0f);
			headAnchor.AddComponent<Fighter.Core.HeadStateAnchor>();

			// === Minimal move set ===
			var light = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
			light.nameId = "Light";
			light.triggerName = "Light";
			light.startup = 0.05f;
			light.active = 0.04f;
			light.recovery = 0.12f;
			light.damage = 8;
			light.hitstun = 0.12f;
			light.blockstun = 0.08f;
			light.hitstopOnHit = 0.04f;
			light.hitstopOnBlock = 0.03f;
			light.knockback = new Vector2(0.85f, 0.35f);
			light.pushbackOnHit = 0.22f;
			light.pushbackOnBlock = 0.35f;
			light.meterOnHit = 50;
			light.meterOnBlock = 20;
			light.canCancelOnHit = true;
			light.canCancelOnBlock = true;
			light.canCancelOnWhiff = true;
			light.onWhiffCancelWindow = new Vector2(0.0f, 0.12f);
			light.onHitCancelWindow = new Vector2(0.0f, 0.25f);
			light.onBlockCancelWindow = new Vector2(0.0f, 0.18f);
			light.cancelIntoTriggers = new[] { "Light", "Heavy", "Super" };
			light.knockdownKind = FightingGame.Combat.KnockdownKind.None;

			var heavy = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
			heavy.nameId = "Heavy";
			heavy.triggerName = "Heavy";
			heavy.startup = 0.12f;
			heavy.active = 0.05f;
			heavy.recovery = 0.22f;
			heavy.damage = 18;
			heavy.hitstun = 0.2f;
			heavy.blockstun = 0.12f;
			heavy.hitstopOnHit = 0.06f;
			heavy.hitstopOnBlock = 0.04f;
			heavy.knockback = new Vector2(1.25f, 0.6f);
			heavy.pushbackOnHit = 0.35f;
			heavy.pushbackOnBlock = 0.6f;
			heavy.meterOnHit = 90;
			heavy.meterOnBlock = 40;
			heavy.canCancelOnHit = true;
			heavy.canCancelOnBlock = false;
			heavy.knockdownKind = FightingGame.Combat.KnockdownKind.None;
			heavy.cancelIntoTriggers = new[] { "Super" };

			var combatActionSet = ScriptableObject.CreateInstance<Data.CombatActionSet>();
			combatActionSet.entries = new Data.CombatActionSet.Entry[] {
				new Data.CombatActionSet.Entry { triggerName = "Light", actionDefinition = light },
				new Data.CombatActionSet.Entry { triggerName = "Heavy", actionDefinition = heavy },
				new Data.CombatActionSet.Entry { triggerName = "Super", actionDefinition = CreateSuper() },
				new Data.CombatActionSet.Entry { triggerName = "Heal",  actionDefinition = CreateHeal()  },
			};
			controller.actionSet = combatActionSet;

			// === Hurtboxes ===
			var hurtboxesRoot = new GameObject("Hurtboxes");
			hurtboxesRoot.transform.SetParent(fighterObject.transform, false);
			controller.hurtboxes = new FightingGame.Combat.Hurtbox[3];
			controller.hurtboxes[0] = CreateHurtbox(hurtboxesRoot.transform, "Head", FightingGame.Combat.HurtRegion.Head, new Vector2(0.6f, 0.7f), new Vector2(0, 1.0f));
			controller.hurtboxes[1] = CreateHurtbox(hurtboxesRoot.transform, "Torso", FightingGame.Combat.HurtRegion.Torso, new Vector2(0.7f, 1.0f), new Vector2(0, 0.3f));
			controller.hurtboxes[2] = CreateHurtbox(hurtboxesRoot.transform, "Legs", FightingGame.Combat.HurtRegion.Legs, new Vector2(0.7f, 0.8f), new Vector2(0, -0.5f));
			for (int i = 0; i < controller.hurtboxes.Length; i++)
			{
				var hurtbox = controller.hurtboxes[i];
				if (hurtbox != null)
				{
					hurtbox.owner = controller;
				}
			}

			// === Hitboxes ===
			var hitboxesRoot = new GameObject("Hitboxes");
			hitboxesRoot.transform.SetParent(fighterObject.transform, false);
			controller.hitboxes = new FightingGame.Combat.Hitbox[2];
			controller.hitboxes[0] = CreateHitbox(hitboxesRoot.transform, "Light1", new Vector2(1.5f, 1.1f), new Vector2(1.25f, 0.5f));
			controller.hitboxes[1] = CreateHitbox(hitboxesRoot.transform, "Heavy1", new Vector2(0.7f, 1.1f), new Vector2(1.6f, 0.5f));
			for (int i = 0; i < controller.hitboxes.Length; i++)
			{
				var hb = controller.hitboxes[i];
				if (hb != null)
				{
					hb.owner = controller;
					hb.active = false;
				}
			}

			// === Brain (AI/Player) ===
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
		/// Creates a hurtbox collider under a parent.
		/// 受擊盒沿 X 放寬 35%，提高白盒調試的容錯率。
		/// </summary>
		static FightingGame.Combat.Hurtbox CreateHurtbox(Transform parent, string name, FightingGame.Combat.HurtRegion region, Vector2 size, Vector2 offset)
		{
			var childObject = new GameObject(name);
			childObject.transform.SetParent(parent, false);
			var hurtbox = childObject.AddComponent<FightingGame.Combat.Hurtbox>();
			hurtbox.region = region;
			var targetSize = new Vector2(size.x * 1.35f, size.y);
			hurtbox.ConfigureCollider(targetSize, offset, isTrigger: true);
			return hurtbox;
		}

		/// <summary>
		/// Creates a hitbox collider under a parent.
		/// 建立命中盒（高度對齊角色，X 長度由參數決定）。
		/// </summary>
		static FightingGame.Combat.Hitbox CreateHitbox(Transform parent, string name, Vector2 size, Vector2 offset)
		{
			var childObject = new GameObject(name);
			childObject.transform.SetParent(parent, false);
			var hitbox = childObject.AddComponent<FightingGame.Combat.Hitbox>();
			hitbox.active = false;
			hitbox.ConfigureCollider(size, offset, autoByCapsule: false, fallbackHeight: size.y, isTrigger: true);
			return hitbox;
		}

		/// <summary>
		/// Creates a solid color 1x1 sprite for debug only.
		/// 僅用於白盒調試。
		/// </summary>
		static Sprite CreateSolidSprite(Color color)
		{
			var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point
			};
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
		}

		/// <summary>
		/// Creates demo super move data.
		/// 建立示範用的超必殺招數資料。
		/// </summary>
		static Data.CombatActionDefinition CreateSuper()
		{
			var actionDefinition = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
			actionDefinition.nameId = "Super";
			actionDefinition.triggerName = "Super";
			actionDefinition.startup = 0.14f;
			actionDefinition.active = 0.08f;
			actionDefinition.recovery = 0.28f;
			actionDefinition.damage = 500;
			actionDefinition.hitstun = 0.28f;
			actionDefinition.blockstun = 0.16f;
			actionDefinition.knockback = new Vector2(4.2f, 2.8f);
			actionDefinition.pushbackOnHit = 1.4f;
			actionDefinition.pushbackOnBlock = 1.6f;
			actionDefinition.hitstopOnHit = 0.12f;
			actionDefinition.hitstopOnBlock = 0.08f;
			actionDefinition.meterOnHit = 120;
			actionDefinition.meterOnBlock = 50;
			actionDefinition.meterCost = 1000;
			return actionDefinition;
		}

		/// <summary>
		/// Creates demo heal move data (for testing heal executor / resource flow).
		/// 建立示範用的治療技能資料（用於驗證治療執行器/資源流）。
		/// </summary>
		static Data.CombatActionDefinition CreateHeal()
		{
			var actionDefinition = ScriptableObject.CreateInstance<Data.CombatActionDefinition>();
			actionDefinition.nameId = "Heal";
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
			actionDefinition.meterCost = 800;
			actionDefinition.healAmount = 1200;
			return actionDefinition;
		}
	}
}