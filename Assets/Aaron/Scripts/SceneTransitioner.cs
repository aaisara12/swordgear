#nullable enable

using UnityEngine;
using UnityEngine.Events;
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
    
    private AsyncOperation? currentSceneTransitionTask;
    private string? sceneBeingLoaded;
    
    public bool TryChangeScene(string sceneName, out AsyncOperation? sceneTransitionTask)
    {
        sceneTransitionTask = null;
        
        if (currentSceneTransitionTask != null)
        {
            return false;
        }
        
        currentSceneTransitionTask = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        if (currentSceneTransitionTask == null)
        {
            return false;
        }
        
        sceneTransitionTask = currentSceneTransitionTask;

        currentSceneTransitionTask.completed += HandleSceneLoaded;
        sceneBeingLoaded = sceneName;
        onSceneTransitionStarted.Invoke(sceneBeingLoaded);
        
        return true;
    }

    private void HandleSceneLoaded(AsyncOperation obj)
    {
        // aisara => I actually have no idea how currentSceneTransitionTask or sceneBeingLoaded could be null here
        currentSceneTransitionTask.ThrowIfNull(nameof(currentSceneTransitionTask));
        sceneBeingLoaded.ThrowIfNull(nameof(sceneBeingLoaded));
        
        onSceneTransitionFinished.Invoke(sceneBeingLoaded);
        currentSceneTransitionTask.completed -= HandleSceneLoaded;
        
        currentSceneTransitionTask = null;
        sceneBeingLoaded = null;
    }
}
