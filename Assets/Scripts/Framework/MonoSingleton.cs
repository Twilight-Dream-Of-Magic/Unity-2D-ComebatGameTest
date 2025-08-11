using UnityEngine;

namespace Framework {
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> {
        public static T Instance { get; private set; }
        [Tooltip("If true, this instance persists across scenes")] public bool dontDestroyOnLoad = true;

        protected virtual void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = (T)this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            OnSingletonInit();
        }

        protected virtual void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Optional hook for subclasses to run one-time initialization after Instance assignment.
        /// </summary>
        protected virtual void OnSingletonInit() {}
    }
}