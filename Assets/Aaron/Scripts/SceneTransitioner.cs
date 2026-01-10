#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Wrapper around Unity's scene management API to provide support for smooth transition animations between scenes
/// and ensure consistent handling of scene loading and unloading.
/// </summary>

// aisara => MonoBehaviour because we may need to deal with animations or coroutines for transitions
public class SceneTransitioner : MonoBehaviour
{
    [SerializeField] private UnityEvent<string> onSceneTransitionFinished = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> onSceneTransitionStarted = new UnityEvent<string>();
    
    [Header("Input")]
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;
    
    [Header("Output")]
    [SerializeField] private BoolEventChannelSO? toggleLoadingScreenChannel;
    [SerializeField] private float delayBeforeTogglingLoadingScreen = 0.5f;
    
    private Task? ongoingSceneTransitionTask;

    private string? lastSceneLoaded;

    // aisara => Don't return bool because it's our responsibility to figure out what to do in a failed scene transition,
    // not the caller's (for example, we could pop a UI that says scene transition failed or raise some events on our end).
    private async Task TransitionToScene(string sceneName)
    {
        if (toggleLoadingScreenChannel == null)
        {
            return;
        }
        
        toggleLoadingScreenChannel.RaiseDataChanged(true);
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        var loadNewSceneTask = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)?.AsTask();

        if (loadNewSceneTask == null)
        {
            Debug.LogError($"Failed to load new scene '{sceneName}'");
            return;
        }
        
        onSceneTransitionStarted.Invoke(sceneName);
        
        if (lastSceneLoaded == null)
        {
            await loadNewSceneTask;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            lastSceneLoaded = sceneName;
            onSceneTransitionFinished.Invoke(sceneName);
            return;
        }
        
        var unloadOldSceneTask = SceneManager.UnloadSceneAsync(lastSceneLoaded)?.AsTask();
        
        if (unloadOldSceneTask == null)
        {
            Debug.LogError($"Failed to unload old scene '{lastSceneLoaded}'");
            return;
        }
        
        await Task.WhenAll(new Task[] { loadNewSceneTask, unloadOldSceneTask });
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        
        toggleLoadingScreenChannel.RaiseDataChanged(false);
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        lastSceneLoaded = sceneName;
        onSceneTransitionFinished.Invoke(sceneName);
    }
    
    public bool TryChangeScene(string sceneName)
    {
        if (ongoingSceneTransitionTask is { IsCompleted: false })
        {
            return false;
        }
        
        ongoingSceneTransitionTask = TransitionToScene(sceneName);
        
        return true;
    }

    /// <summary>
    /// Add in a scene that's not intended to replace the current scene, e.g., a UI overlay scene.
    /// </summary>
    /// <param name="sceneName"></param>
    public void AddAuxiliaryScene(string sceneName)
    {
        var loadSceneOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (loadSceneOperation == null)
        {
            Debug.LogError($"Failed to load scene '{sceneName}'");
        }
    }

    private void Awake()
    {
        if (sceneChangeRequestChannel != null)
        {
            sceneChangeRequestChannel.OnDataChanged += HandleSceneChangeRequested;
        }
    }

    private void HandleSceneChangeRequested(string requestedScene)
    {
        if (TryChangeScene(requestedScene) == false)
        {
            Debug.LogError("Requested scene change failed and will not transition to scene: " + requestedScene);
        }
    }

    private void OnDestroy()
    {
        if (sceneChangeRequestChannel != null)
        {
            sceneChangeRequestChannel.OnDataChanged -= HandleSceneChangeRequested;
        }
    }
}
