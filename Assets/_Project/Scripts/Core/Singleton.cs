using UnityEngine;

namespace WanderersGuild
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        [Tooltip("Detach to root and survive scene loads.")]
        [SerializeField] private bool persistAcrossScenes = true;
        
        public static T Instance { get; private set; }
        
        public static bool HasInstance => Instance;

        // NOTE: Non-virtual on purpose. Derived classes must NOT declare Awake; override OnSingletonAwake instead.
        protected void Awake()
        {
            if (HasInstance && Instance != this)
            {
                gameObject.SetActive(false); // Prevents OnEnable/Start on the duplicate (Destroy is deferred).
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;

            if (persistAcrossScenes)
            {
                transform.SetParent(null); // DontDestroyOnLoad only works on root objects.
                DontDestroyOnLoad(gameObject);
            }
            
            OnSingletonAwake();
        }
        
        // Runs once, only on the surviving instance.
        protected virtual void OnSingletonAwake() { }
        
        // NOTE: Overrides must call base.OnDestroy().
        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}