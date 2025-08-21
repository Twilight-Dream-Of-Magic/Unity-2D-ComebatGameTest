using UnityEngine;
using FightingGame.Combat.Actors;

namespace Systems {
	/// <summary>
	/// Frames the arena by following the player (targetA) on X axis with smoothing and clamping.
	/// 摄像机取景：沿 X 轴平滑跟随玩家（targetA），并在场地范围内限位。
	/// </summary>
	public class CameraFramer : MonoBehaviour {
		/// <summary>Singleton instance. 单例。</summary>
		public static CameraFramer Instance { get; private set; }
		/// <summary>Primary follow target (player). 主要跟随目标（玩家）。</summary>
		public Transform targetA; // follow this (player)
		/// <summary>Secondary target (currently ignored). 次要目标（当前忽略）。</summary>
		public Transform targetB; // ignored for framing now
		/// <summary>Arena half extents (X,Y). 场地半宽与半高。</summary>
		public Vector2 arenaHalfExtents = new Vector2(8f, 3f);
		/// <summary>Smoothing factor (higher is snappier). 平滑因子（越大越快）。</summary>
		public float smooth = 6f;
		[Header("Advanced Follow")]
		public bool useMidpoint = true;
		public float smoothX = 14f; // 更快的X收敛，避免“顺移”感
		public float smoothY = 10f; // 稍快的Y收敛
		public float yDeadband = 0.06f; // 更小死区，提升贴合度但仍抑制抖动
		public float xClampMargin = 2.5f;
		public float yClampMargin = 1.0f;

		Vector3 smoothDampVelocity;
		float velX, velY;
		Camera cameraComponent;
		Vector3 basePosition; // default z

		void TryAutoBind() {
			if (targetA && targetB) return;
			var fighters = FindObjectsOfType<FighterActor>();
			FighterActor player = null;
			FighterActor ai = null;
			for (int i = 0; i < fighters.Length; i++) {
				if (fighters[i] == null) continue;
				if (fighters[i].team == FighterTeam.Player && player == null) player = fighters[i];
				if (fighters[i].team == FighterTeam.AI && ai == null) ai = fighters[i];
			}
			if (!targetA && player == null && fighters.Length > 0) player = fighters[0];
			if (!targetA && player != null) {
				targetA = player.transform;
				#if UNITY_EDITOR
				Debug.Log($"[CameraFramer] Auto-bound targetA to {targetA.name}");
				#endif
			}
			if (!targetB && ai != null) {
				targetB = ai.transform;
			}
		}

		void Awake() {
			if (Instance != null && Instance != this) { Destroy(gameObject); return; }
			Instance = this;
			cameraComponent = GetComponent<Camera>();
			basePosition = transform.position;
			TryAutoBind();
		}

		void LateUpdate() {
			if (!targetA) { TryAutoBind(); if (!targetA) return; }
			Vector3 a = targetA.position;
			Vector3 b = (useMidpoint && targetB) ? targetB.position : a;
			Vector3 mid = (a + b) * 0.5f;
			float targetX = mid.x;
			float targetY = mid.y;
			float halfWidth = arenaHalfExtents.x;
			float halfHeight = arenaHalfExtents.y;
			targetX = Mathf.Clamp(targetX, -halfWidth + xClampMargin, halfWidth - xClampMargin);
			targetY = Mathf.Clamp(targetY, -halfHeight + yClampMargin, halfHeight - yClampMargin);
			float curX = transform.position.x;
			float curY = transform.position.y;
			if (Mathf.Abs(targetY - curY) < yDeadband) targetY = curY;
			float newX = Mathf.SmoothDamp(curX, targetX, ref velX, 1f / Mathf.Max(0.01f, smoothX));
			float newY = Mathf.SmoothDamp(curY, targetY, ref velY, 1f / Mathf.Max(0.01f, smoothY));
			transform.position = new Vector3(newX, newY, basePosition.z);
		}
	}
}