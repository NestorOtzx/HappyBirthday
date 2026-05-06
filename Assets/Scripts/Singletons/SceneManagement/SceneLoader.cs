using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace Nivelo.Singletons.SceneManagement
{
  /// <summary>
  /// Centralized scene loading service.
  /// <para></para>
  /// Handles all scene transitions using strongly-typed GameScenes values,
  /// supports loading screens, forced minimum loading times, and exposes
  /// loading progress for UI consumption.
  /// </summary>
  public class SceneLoader : Singleton<SceneLoader>
  {
    // INTERNAL CONFIGURATION FIELDS //
    [SerializeField]
    [Tooltip("If true, logs scene loading operations to the console.")]
    bool logSceneLoads = true;

    // INTERNAL LOADING STATE //
    AsyncOperation currentAsyncOperation;
    float forcedLoadingTime;
    float loadingStartTime;
    bool isLoading;

    GameScenes currentScene;

    // UNITY LIFECYCLE METHODS //
    protected override void Awake()
    {
      base.Awake();
      Instance.currentScene = SceneLoader.Instance.CalculateCurrentScene();
      SceneManager.sceneUnloaded += HandleSceneUnloaded;
      SceneManager.sceneLoaded += HandleSceneLoaded;
      SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    void OnDestroy()
    {
      SceneManager.sceneUnloaded -= HandleSceneUnloaded;
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    // PUBLIC API METHODS //

    /// <summary>
    /// Loads a scene immediately, replacing the current one.
    /// </summary>
    public void LoadScene(GameScenes scene)
    {
      Log($"Loading scene: {scene}");
      SceneManager.LoadScene(scene.ToString(), LoadSceneMode.Single);
    }

    /// <summary>
    /// Reloads the currently active scene.
    /// </summary>
    public void ReloadCurrentScene()
    {
      Scene activeScene = SceneManager.GetActiveScene();
      Log($"Reloading scene: {activeScene.name}");
      SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
    }

    /// <summary>
    /// Loads a target scene using an intermediate loading scene.
    /// <para></para>
    /// An optional minimum loading time can be specified to guarantee
    /// the loading screen remains visible for a given duration.
    /// </summary>
    public void LoadSceneWithLoadingScreen(
      GameScenes loadingScene,
      GameScenes targetScene,
      float minimumLoadingTime = 0f)
    {
      if (isLoading)
      {
        Log("SceneLoader is already loading a scene.");
        return;
      }

      forcedLoadingTime = Mathf.Max(0f, minimumLoadingTime);
      loadingStartTime = Time.time;
      isLoading = true;

      Log($"Loading loading scene: {loadingScene}");
      StartCoroutine(
        LoadSceneWithLoadingScreenRoutine(
          loadingScene,
          targetScene));
    }

    /// <summary>
    /// Returns the currently active scene as a GameScenes enum value.
    /// <para></para>
    /// The scene name must exactly match one of the GameScenes enum entries.
    /// An exception is thrown if no valid match is found.
    /// </summary>
    GameScenes CalculateCurrentScene()
    {
      Scene activeScene = SceneManager.GetActiveScene();

      if (Enum.TryParse(
        activeScene.name,
        out GameScenes parsedScene))
      {
        return parsedScene;
      }

      throw new InvalidOperationException(
        $"Active scene '{activeScene.name}' does not match any GameScenes enum value.");
    }

    // PUBLIC LOADING STATE GETTERS //

    /// <summary>
    /// Returns whether a scene is currently being loaded asynchronously.
    /// </summary>
    public bool IsLoading
    {
      get
      {
        return isLoading;
      }
    }

    public GameScenes GetCurrentScene()
    {
      return currentScene;
    }

    /// <summary>
    /// Returns the current loading progress (0–1).
    /// <para></para>
    /// This value accounts for both the async loading progress and
    /// the forced minimum loading time.
    /// </summary>
    public float LoadingProgress
    {
      get
      {
        if (!isLoading)
        {
          return 1f;
        }

        float asyncProgress = currentAsyncOperation != null
          ? Mathf.Clamp01(currentAsyncOperation.progress / 0.9f)
          : 0f;

        if (forcedLoadingTime <= 0f)
        {
          return asyncProgress;
        }

        float timeProgress =
          Mathf.Clamp01((Time.time - loadingStartTime) / forcedLoadingTime);

        return Mathf.Min(asyncProgress, timeProgress);
      }
    }

    // COROUTINES //

    IEnumerator LoadSceneWithLoadingScreenRoutine(
      GameScenes loadingScene,
      GameScenes targetScene)
    {
      SceneManager.LoadScene(
        loadingScene.ToString(),
        LoadSceneMode.Single);

      yield return null;

      Log($"Async loading target scene: {targetScene}");

      currentAsyncOperation =
        SceneManager.LoadSceneAsync(
          targetScene.ToString(),
          LoadSceneMode.Single);

      currentAsyncOperation.allowSceneActivation = false;

      while (LoadingProgress < 1f)
      {
        yield return null;
      }

      Log($"Activating target scene: {targetScene}");
      currentAsyncOperation.allowSceneActivation = true;

      currentAsyncOperation = null;
      isLoading = false;
    }

    void HandleSceneUnloaded(Scene scene)
    {
      Log($"[CombatSpritesLoader] Scene unloaded: {scene.name}");
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      Log($"[CombatSpritesLoader] Scene loaded: {scene.name} (mode={mode})");
    }

    void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
      Log($"[CombatSpritesLoader] Active scene changed: {previousScene.name} -> {nextScene.name}");

      if (Enum.TryParse(nextScene.name, out GameScenes parsedScene))
      {
        currentScene = parsedScene;
      }
    }

    // INNER HELPER METHODS //
    void Log(string message)
    {
      if (!logSceneLoads)
      {
        return;
      }

      Debug.Log($"[SceneLoader] {message}");
    }
  }
}
