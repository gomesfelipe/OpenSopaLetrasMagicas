using UnityEngine;

/// <summary>
/// Generic base class for singletons that inherit from MonoBehaviour.
/// </summary>
/// <typeparam name="T">The type of the singleton class.</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static object _lock = new object();

    private static bool applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton<{typeof(T)}>] Instance won't be returned because the app is quitting.");
                return null;
            }

            lock (_lock)
            {
                if (instance == null)
                {
                    T[] instances = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

                    if (instances.Length > 1)
                    {
                        Debug.LogError($"[Singleton<{typeof(T)}>] Multiple instances detected!");
                        instance = instances[0]; // still use the first one
                    }
                    else if (instances.Length == 1)
                    {
                        instance = instances[0];
                    }
                    else
                    {
                        GameObject singletonObject = new GameObject($"(singleton) {typeof(T)}");
                        instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);

                        Debug.Log($"[Singleton<{typeof(T)}>] Instance created automatically.");
                    }
                }

                return instance;
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
            applicationIsQuitting = true;
    }
}
