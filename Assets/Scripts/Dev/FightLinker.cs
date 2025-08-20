using UnityEngine;
using FightingGame.Combat.Actors;

namespace Dev
{
	/// <summary>
	/// Links two fighters as opponents and configures camera, collisions, hurtboxes, and World Space HUD billboards.
	/// 聯結兩個角色為對手，並配置攝影機、碰撞忽略、受擊盒、世界空間 HUD 狀態看板。
	/// </summary>
	public static class FightLinker
	{
		/// <summary>
		/// Establish opponent links between two fighters.
		/// 建立兩名角色之間的對手關係。
		/// </summary>
		/// <param name="p1">Player 1 fighter actor / 玩家1角色</param>
		/// <param name="p2">Player 2 (or AI) fighter actor / 玩家2或AI角色</param>
		/// <param name="arenaHalfExtents">Arena half extents (x=half width, y=half height) / 場地半尺寸</param>
		public static void LinkOpponents(FighterActor p1, FighterActor p2, Vector2 arenaHalfExtents)
		{
			if (!p1 || !p2) return;

			// Link opponents
			p1.opponent = p2.transform;
			p2.opponent = p1.transform;

			// Camera framing setup
			var cameraFramer = Camera.main ? Camera.main.GetComponent<Systems.CameraFramer>() : null;
			if (cameraFramer)
			{
				cameraFramer.targetA = p1.transform;
				cameraFramer.targetB = p2.transform;
				cameraFramer.arenaHalfExtents = arenaHalfExtents;
			}

			// Ensure body colliders participate in physics
			if (p1.bodyCollider) p1.bodyCollider.isTrigger = false;
			if (p2.bodyCollider) p2.bodyCollider.isTrigger = false;

			// Ignore collisions between fighters' non-trigger colliders
			var all1 = p1.GetComponentsInChildren<Collider2D>(true);
			var all2 = p2.GetComponentsInChildren<Collider2D>(true);
			for (int i = 0; i < all1.Length; i++)
			{
				if (all1[i] == null || all1[i].isTrigger) continue;
				for (int j = 0; j < all2.Length; j++)
				{
					if (all2[j] == null || all2[j].isTrigger) continue;
					Physics2D.IgnoreCollision(all1[i], all2[j], true);
				}
			}

			// Configure hurtboxes
			foreach (var hurtbox in p1.GetComponentsInChildren<FightingGame.Combat.Hurtbox>(true))
			{
				hurtbox.owner = p1;
				hurtbox.activeStanding = true;
				hurtbox.activeCrouching = hurtbox.region != FightingGame.Combat.HurtRegion.Head;
				hurtbox.activeAirborne = hurtbox.region != FightingGame.Combat.HurtRegion.Legs;
			}
			foreach (var hurtbox in p2.GetComponentsInChildren<FightingGame.Combat.Hurtbox>(true))
			{
				hurtbox.owner = p2;
				hurtbox.activeStanding = true;
				hurtbox.activeCrouching = hurtbox.region != FightingGame.Combat.HurtRegion.Head;
				hurtbox.activeAirborne = hurtbox.region != FightingGame.Combat.HurtRegion.Legs;
			}

			// Create world-space state billboards with binder
			CreateWorldBillboardFor(p1, "P1", new Color(0.9f, 0.95f, 1f, 1f));
			CreateWorldBillboardFor(p2, "AI", new Color(1f, 0.85f, 0.85f, 1f));
		}

		/// <summary>
		/// Creates a world-space billboard (HUD) showing fighter state.
		/// 建立角色的世界空間 HUD 狀態看板。
		/// </summary>
		static void CreateWorldBillboardFor(FighterActor actor, string prefix, Color color)
		{
			if (!actor) return;

			var anchorComponent = actor.GetComponentInChildren<Fighter.Core.HeadStateAnchor>();
			var anchor = anchorComponent != null ? anchorComponent.transform : actor.transform;

			var go = new GameObject(prefix + "StateWS");
			go.transform.SetParent(anchor, false);
			go.transform.localPosition = Vector3.zero;

			var billboard = go.AddComponent<UI.HUD.WorldStateBillboard>();
			billboard.fighter = actor;
			billboard.anchor = anchor;
			billboard.SetText(prefix + ": --");
			billboard.SetColor(color);

			var binder = go.AddComponent<UI.HUD.WorldStateBillboardBinder>();
			binder.fighter = actor;
			binder.billboard = billboard;
			binder.labelPrefix = prefix;
			binder.labelColor = color;
		}
	}
}
