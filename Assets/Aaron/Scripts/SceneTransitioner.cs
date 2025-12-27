#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Wrapper around Unity's scene management API to provide support for smooth transition animations between scenes
/// and ensure consistent handling of scene loading and unloading.
/// </summary>

// aisara => MonoBehaviour because we may need to deal with animations or coroutines for transitions
public class SceneTransitioner : MonoBehaviour
{
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;
    [SerializeField] private UnityEvent<string> onSceneTransitionFinished = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> onSceneTransitionStarted = new UnityEvent<string>();
    
    private Task? ongoingSceneTransitionTask;

    private string? lastSceneLoaded;

    // aisara => Don't return bool because it's our responsibility to figure out what to do in a failed scene transition,
    // not the caller's (for example, we could pop a UI that says scene transition failed or raise some events on our end).
    private async Task TransitionToScene(string sceneName)
    {
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
