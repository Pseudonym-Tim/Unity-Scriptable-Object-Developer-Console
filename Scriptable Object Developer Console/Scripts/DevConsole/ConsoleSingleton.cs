using UnityEngine;

public abstract class ConsoleSingleton<T> : ConsoleSingleton where T : MonoBehaviour
{
    #region  Fields
    private static T _instance;

    private static readonly object Lock = new object();

    [SerializeField]
    private bool _persistBetweenScenes = true;
    #endregion

    #region  Properties
    public static T Instance
    {
        get
        {
            if(Quitting)
            {
                Debug.LogWarning($"[{nameof(ConsoleSingleton)}<{typeof(T)}>] Instance will not be returned because the application is quitting.");
                // ReSharper disable once AssignNullToNotNullAttribute
                return null;
            }
            lock(Lock)
            {
                if(_instance != null)
                    return _instance;
                var instances = FindObjectsOfType<T>();
                var count = instances.Length;
                if(count > 0)
                {
                    if(count == 1)
                        return _instance = instances[0];
                    Debug.LogWarning($"[{nameof(ConsoleSingleton)}<{typeof(T)}>] There should never be more than one {nameof(ConsoleSingleton)} of type {typeof(T)} in the scene, but {count} were found. The first instance found will be used, and all others will be destroyed.");
                    for(var i = 1; i < instances.Length; i++)
                        Destroy(instances[i]);
                    return _instance = instances[0];
                }

                Debug.Log($"[{nameof(ConsoleSingleton)}<{typeof(T)}>] An instance is needed in the scene and no existing instances were found, so a new instance will be created.");

                string consolePrefabFullPath = "Prefabs/Developer Console";

                bool existsInResources = Resources.Load(consolePrefabFullPath);

                if(!existsInResources) { Debug.LogError("Console not found in Resources folder! Can't make in scene!"); return null; }

                return Instantiate(Resources.Load(consolePrefabFullPath) as GameObject) as T;
            }
        }
    }
    #endregion

    #region  Methods
    private void Awake()
    {
        if(_persistBetweenScenes)
            DontDestroyOnLoad(gameObject);
        OnAwake();
    }

    protected virtual void OnAwake() { }
    #endregion
}

public abstract class ConsoleSingleton : MonoBehaviour
{
    #region  Properties
    public static bool Quitting { get; private set; }
    #endregion

    #region  Methods
    private void OnApplicationQuit()
    {
        Quitting = true;
    }
    #endregion
}
