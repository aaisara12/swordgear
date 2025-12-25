#nullable enable

using UnityEngine;

/// <summary>
/// Entry point to jump start the game loop
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private ElementManager? elementManager;
    [SerializeField] private PlayerBlobLoaderSO? playerBlobLoader;
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private string startSceneName = string.Empty;

    private GameStateSynchronizer? gameStateSynchronizer;
    
    public void Awake()
    {
        if (elementManager == null)
        {
            Debug.LogError($"{nameof(elementManager)} is null");
            return;
        }

        if (playerBlobLoader == null)
        {
            Debug.LogError($"{nameof(playerBlobLoader)} is null");
            return;
        }

        if (sceneTransitioner == null)
        {
            Debug.LogError($"{nameof(sceneTransitioner)} is null");
            return;
        }

        if (playerBlobLoader.TryLoad(out var playerBlob) == false)
        {
            Debug.LogWarning("Player blob could not be loaded at this time. Using empty player blob.");
            playerBlob = new PlayerBlob();
        }
        
        playerBlob.ThrowIfNull(nameof(playerBlob));
        
        gameStateSynchronizer = new GameStateSynchronizer(playerBlob, elementManager);
        gameStateSynchronizer.Start();

        if (sceneTransitioner.TryChangeScene(startSceneName, out _) == false)
        {
            Debug.LogError("Failed to load start scene: " + startSceneName + ". Perhaps it's not registered in the build settings?");
            
            // aisara => Quit the application if we can't load the start scene because the game is in an undefined state
            Application.Quit();
        }
    }
    
    public void OnDestroy()
    {
        gameStateSynchronizer?.Dispose();
    }
}
