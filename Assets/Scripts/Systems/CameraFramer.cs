using UnityEngine;

namespace Systems {
    public class CameraFramer : MonoBehaviour {
        public Transform targetA;
        public Transform targetB;
        public Vector2 arenaHalfExtents = new Vector2(8f, 3f);
        public float smooth = 6f;

        Vector3 velocity;
        Camera cam;
        Vector3 basePos; // default z

        void Awake() {
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        void LateUpdate() {
            if (!targetA || !targetB) return;
            // target center
            Vector3 center = (targetA.position + targetB.position) * 0.5f;
            Vector3 target = new Vector3(center.x, 0f, basePos.z);
            // clamp to arena
            float halfW = arenaHalfExtents.x;
            target.x = Mathf.Clamp(target.x, -halfW + 2.5f, halfW - 2.5f); // keep some margin
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 1f / Mathf.Max(0.01f, smooth));
        }
    }
}