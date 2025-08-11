using UnityEngine;
using System.Collections.Generic;

namespace Systems {
    public class HitEffectManager : MonoBehaviour {
        public static HitEffectManager Instance { get; private set; }

        readonly Queue<GameObject> pool = new Queue<GameObject>();
        const int Prewarm = 24;

        readonly Queue<GameObject> textPool = new Queue<GameObject>();

        private void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            // prewarm pool
            for (int i = 0; i < Prewarm; i++) pool.Enqueue(CreatePooledDot());
            for (int i = 0; i < 12; i++) textPool.Enqueue(CreatePooledText());
        }

        public void SpawnHit(Vector3 position, bool isPlayerHit) {
            // Spawn small burst of colored quads
            int count = Random.Range(6, 10);
            for (int i = 0; i < count; i++) SpawnDot(position, isPlayerHit);
            SpawnFloatingText(position + Vector3.up * 0.2f, isPlayerHit ? "-" : "+");
        }

        void SpawnDot(Vector3 pos, bool isPlayerHit) {
            var go = GetPooledDot();
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 1000;
            // if player was hit -> reddish; if enemy hit -> bluish
            sr.color = isPlayerHit ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 0.8f, 1f, 0.9f);
            go.transform.position = pos;
            float scale = Random.Range(0.35f, 0.6f);
            go.transform.localScale = Vector3.one * scale;
            var drift = go.GetComponent<BurstDrift>();
            if (!drift) drift = go.AddComponent<BurstDrift>();
            drift.life = Random.Range(0.18f, 0.32f);
            drift.velocity = Random.insideUnitCircle * Random.Range(1.0f, 2.0f);
            go.SetActive(true);
        }

        void SpawnFloatingText(Vector3 pos, string symbol) {
            var go = GetPooledText();
            go.transform.position = pos;
            var tm = go.GetComponent<TextMesh>();
            tm.text = symbol;
            tm.color = new Color(1f, 1f, 1f, 0.95f);
            var drift = go.GetComponent<BurstDriftText>();
            if (!drift) drift = go.AddComponent<BurstDriftText>();
            drift.life = 0.4f; drift.velocity = new Vector2(0f, Random.Range(0.8f, 1.2f));
            go.SetActive(true);
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
                if (t >= life) { Recycle(gameObject); t = 0f; }
            }
        }

        class BurstDriftText : MonoBehaviour {
            public float life = 0.4f; float t;
            public Vector2 velocity;
            void Update() {
                t += Time.unscaledDeltaTime;
                transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
                var tm = GetComponent<TextMesh>();
                if (tm) tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, Mathf.Max(0, 1f - t / life));
                if (t >= life) { RecycleText(gameObject); t = 0f; }
            }
        }

        GameObject CreatePooledDot() {
            var go = new GameObject("HitFX");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDot();
            go.SetActive(false);
            go.hideFlags = HideFlags.HideInHierarchy;
            return go;
        }

        GameObject GetPooledDot() {
            if (pool.Count == 0) pool.Enqueue(CreatePooledDot());
            return pool.Dequeue();
        }

        static void Recycle(GameObject go) {
            if (!Instance) { Destroy(go); return; }
            go.SetActive(false);
            Instance.pool.Enqueue(go);
        }

        GameObject CreatePooledText() {
            var go = new GameObject("HitText");
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.1f; tm.fontSize = 64;
            go.SetActive(false); go.hideFlags = HideFlags.HideInHierarchy;
            return go;
        }
        GameObject GetPooledText() { if (textPool.Count == 0) textPool.Enqueue(CreatePooledText()); return textPool.Dequeue(); }
        static void RecycleText(GameObject go) { if (!Instance) { Destroy(go); return; } go.SetActive(false); Instance.textPool.Enqueue(go); }

        Sprite CreateDot() {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}