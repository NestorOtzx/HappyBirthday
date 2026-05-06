using UnityEngine;


namespace Nivelo.Singletons
{
  
  /// <summary>
  /// Generic singleton base class for MonoBehaviours.
  /// <para></para>
  /// Inherit from this class to easily turn a component into a singleton.
  /// Ensures a single instance and optionally persists between scenes.
  /// </summary>
  public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
  {
    protected static T instance;

    public static T Instance
    {
      get
      {
        if (instance == null)
        {
          instance = FindObjectOfType<T>();

          if (instance == null)
          {
            GameObject singletonObject = new GameObject(typeof(T).Name);
            instance = singletonObject.AddComponent<T>();
          }
        }

        return instance;
      }
    }

    protected virtual void Awake()
    {
      if (instance == null)
      {
        instance = this as T;
        DontDestroyOnLoad(gameObject);
      }
      else if (instance != this)
      {
        Destroy(gameObject);
      }
    }
  }

}