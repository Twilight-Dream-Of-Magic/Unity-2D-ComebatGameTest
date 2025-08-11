using UnityEngine;

namespace Systems {
    public class HitEffectManager : MonoBehaviour {
        public static HitEffectManager Instance { get; private set; }

        private void Awake() {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public void SpawnHit(Vector3 position, bool isPlayerHit) {
            var go = new GameObject("HitEffect");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDot();
            sr.sortingOrder = 1000;
            sr.color = isPlayerHit ? new Color(1f, 0.3f, 0.3f, 0.85f) : new Color(0.3f, 0.8f, 1f, 0.85f);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.6f;
            go.AddComponent<AutoDestroy>().life = 0.25f;
        }

        class AutoDestroy : MonoBehaviour {
            public float life = 0.25f; float t;
            void Update() { t += Time.unscaledDeltaTime; if (t >= life) Destroy(gameObject); }
        }

        Sprite CreateDot() {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}