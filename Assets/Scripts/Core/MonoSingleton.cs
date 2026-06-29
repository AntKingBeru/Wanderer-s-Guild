// Generic MonoBehaviour Singleton base ensuring one persistent instance per type.

using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    // Cached single instance — accessed globally via the static Instance property.
    private static T _instance;
    
    public static T Instance
    {
        get
        {
            if (!_instance)
                _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }
    // True only once a live instance exists — use to guard early access.
    public static bool Exists => _instance;
    
    // Registers this as the singleton and destroys any duplicate.
    protected virtual void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        // DontDestroyOnLoad only works on root objects
        if (!transform.parent)
            DontDestroyOnLoad(gameObject);

        OnSingletonAwake();
    }
    // NOTE: Override this instead of Awake for subclass init that must run on registration.
    protected virtual void OnSingletonAwake() { }
    
    // Clears the static reference so a reloaded scene can re-register cleanly.
    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}