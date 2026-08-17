// Generic MonoBehaviour singleton base with an inspector flag for cross-scene persistence.
using UnityEngine;

namespace WanderersGuild
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        // Tick in the inspector for singletons that must survive scene loads
        [SerializeField] private bool dontDestroyOnLoad;

        private static T _instance;

        // Global access point. Logs an error (and returns null) if accessed before it exists.
        public static T Instance
        {
            get
            {
                if (!_instance)
                    Debug.LogError($"[Singleton] {typeof(T).Name} accessed before it existed in the scene.");
                return _instance;
            }
        }
        
        // Safe existence check that never logs — use before Instance when unsure.
        public static bool Exists => _instance;
        
        // Claims the instance slot and applies persistence. Override, but call base.Awake() first.
        protected virtual void Awake()
        {
            if (_instance && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            
            // DontDestroyOnLoad only works on root objects; detach if nested so persistence is reliable.
            if (dontDestroyOnLoad)
            {
                if (transform.parent)
                    transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
        
        // Clears the static ref so scene changes / play-exit don't leave a stale pointer.
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}