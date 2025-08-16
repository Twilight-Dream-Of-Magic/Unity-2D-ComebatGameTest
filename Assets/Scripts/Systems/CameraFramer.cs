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

        Vector3 smoothDampVelocity;
        Camera cameraComponent;
        Vector3 basePosition; // default z

        void TryAutoBind() {
            if (targetA) return;
            var fighters = FindObjectsOfType<FighterActor>();
            FighterActor player = null;
            for (int i = 0; i < fighters.Length; i++) {
                if (fighters[i] != null && fighters[i].team == FighterTeam.Player) { player = fighters[i]; break; }
            }
            if (player == null && fighters.Length > 0) player = fighters[0];
            if (player != null) {
                targetA = player.transform;
                #if UNITY_EDITOR
                Debug.Log($"[CameraFramer] Auto-bound targetA to {targetA.name}");
                #endif
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
            Vector3 targetPosition = new Vector3(targetA.position.x, 0f, basePosition.z);
            float halfWidth = arenaHalfExtents.x;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -halfWidth + 2.5f, halfWidth - 2.5f);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref smoothDampVelocity, 1f / Mathf.Max(0.01f, smooth));
        }
    }
}