using System;

namespace Framework {
    public abstract class LazySingleton<T> where T : class, new() {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());
        public static T Instance => _instance.Value;
        // Prevent public construction
        protected LazySingleton() {}
    }
}