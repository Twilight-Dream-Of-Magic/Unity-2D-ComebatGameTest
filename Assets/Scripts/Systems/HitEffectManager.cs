using UnityEngine;

namespace Systems {
    public class HitEffectManager : MonoBehaviour {
        public static HitEffectManager Instance { get; private set; }

        private void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public void SpawnHit(Vector3 position, bool isPlayerHit) {
            // Spawn small burst of colored quads
            int count = Random.Range(6, 10);
            for (int i = 0; i < count; i++) SpawnDot(position, isPlayerHit);
        }

        void SpawnDot(Vector3 pos, bool isPlayerHit) {
            var go = new GameObject("HitFX");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDot();
            sr.sortingOrder = 1000;
            // if player was hit -> reddish; if enemy hit -> bluish
            sr.color = isPlayerHit ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 0.8f, 1f, 0.9f);
            go.transform.position = pos;
            float scale = Random.Range(0.35f, 0.6f);
            go.transform.localScale = Vector3.one * scale;
            var drift = go.AddComponent<BurstDrift>();
            drift.life = Random.Range(0.18f, 0.32f);
            drift.velocity = Random.insideUnitCircle * Random.Range(1.0f, 2.0f);
        }

        class BurstDrift : MonoBehaviour {
            public float life = 0.25f; float t;
            public Vector2 velocity;
            void Update() {
                t += Time.unscaledDeltaTime;
                transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
                // fade out
                var sr = GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Max(0, 1f - t / life));
                if (t >= life) Destroy(gameObject);
            }
        }

        Sprite CreateDot() {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}