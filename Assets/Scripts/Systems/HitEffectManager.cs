using UnityEngine;
using System.Collections.Generic;
using Framework;

namespace Systems {
    /// <summary>
    /// Spawns hit dots and floating damage numbers with simple pooling and drift behaviours.
    /// 命中特效与飘字管理：带对象池与漂移效果。
    /// </summary>
    public class HitEffectManager : MonoSingleton<HitEffectManager> {
        readonly Queue<GameObject> pool = new Queue<GameObject>();
        const int Prewarm = 24;

        readonly Queue<GameObject> textPool = new Queue<GameObject>();

        protected override void DoAwake() {
            // prewarm pool
            for (int i = 0; i < Prewarm; i++) pool.Enqueue(CreatePooledDot());
            for (int i = 0; i < 12; i++) textPool.Enqueue(CreatePooledText());
        }

        public void SpawnHit(Vector3 position, bool isPlayerHit) {
            int count = Random.Range(6, 10);
            for (int i = 0; i < count; i++) SpawnDot(position, isPlayerHit);
        }

        public void SpawnDamageNumber(Vector3 position, int amount, bool victimIsPlayer) {
            if (amount <= 0) return;
            var textObject = GetPooledText();
            textObject.transform.position = position;
            var textMesh = textObject.GetComponent<TextMesh>();
            textMesh.text = $"-{amount}";
            textMesh.color = victimIsPlayer ? new Color(1f, 0.35f, 0.35f, 0.98f) : new Color(0.35f, 0.8f, 1f, 0.98f);
            var driftText = textObject.GetComponent<BurstDriftText>();
            if (!driftText) driftText = textObject.AddComponent<BurstDriftText>();
            driftText.life = 0.6f; driftText.velocity = new Vector2(Random.Range(-0.1f,0.1f), Random.Range(1.0f, 1.6f));
            textObject.SetActive(true);
        }

        void SpawnDot(Vector3 position, bool isPlayerHit) {
            var dotObject = GetPooledDot();
            var spriteRenderer = dotObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1000;
            spriteRenderer.color = isPlayerHit ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 0.8f, 1f, 0.9f);
            dotObject.transform.position = position;
            float scale = Random.Range(0.35f, 0.6f);
            dotObject.transform.localScale = Vector3.one * scale;
            var drift = dotObject.GetComponent<BurstDrift>();
            if (!drift) drift = dotObject.AddComponent<BurstDrift>();
            drift.life = Random.Range(0.18f, 0.32f);
            drift.velocity = Random.insideUnitCircle * Random.Range(1.0f, 2.0f);
            dotObject.SetActive(true);
        }

        class BurstDrift : MonoBehaviour {
            public float life = 0.25f; float elapsed;
            public Vector2 velocity;
            void Update() {
                elapsed += Time.unscaledDeltaTime;
                transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer) spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, Mathf.Max(0, 1f - elapsed / life));
                if (elapsed >= life) { Recycle(gameObject); elapsed = 0f; }
            }
        }

        class BurstDriftText : MonoBehaviour {
            public float life = 0.4f; float elapsed;
            public Vector2 velocity;
            void Update() {
                elapsed += Time.unscaledDeltaTime;
                transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
                var textMesh = GetComponent<TextMesh>();
                if (textMesh) textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, Mathf.Max(0, 1f - elapsed / life));
                if (elapsed >= life) { RecycleText(gameObject); elapsed = 0f; }
            }
        }

        GameObject CreatePooledDot() {
            var dotObject = new GameObject("HitFX");
            var spriteRenderer = dotObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateDot();
            dotObject.SetActive(false);
            dotObject.hideFlags = HideFlags.HideInHierarchy;
            return dotObject;
        }

        GameObject GetPooledDot() {
            if (pool.Count == 0) pool.Enqueue(CreatePooledDot());
            return pool.Dequeue();
        }

        static void Recycle(GameObject instance) {
            if (!Instance) { Destroy(instance); return; }
            instance.SetActive(false);
            Instance.pool.Enqueue(instance);
        }

        GameObject CreatePooledText() {
            var textObject = new GameObject("HitText");
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.characterSize = 0.12f; textMesh.fontSize = 72;
            textObject.SetActive(false); textObject.hideFlags = HideFlags.HideInHierarchy;
            return textObject;
        }
        GameObject GetPooledText() { if (textPool.Count == 0) textPool.Enqueue(CreatePooledText()); return textPool.Dequeue(); }
        static void RecycleText(GameObject instance) { if (!Instance) { Destroy(instance); return; } instance.SetActive(false); Instance.textPool.Enqueue(instance); }

        Sprite CreateDot() {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}