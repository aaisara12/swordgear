#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entry point to jump start the game loop
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private PlayerBlobLoaderSO? playerBlobLoader;
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField, Scene] private string startSceneName = string.Empty;

    [SerializeField] private List<InitializeableGameComponent> gameComponents = new List<InitializeableGameComponent>();
    [SerializeField] private List<InitializeableUnrestrictedGameComponent> unrestrictedGameComponents = new List<InitializeableUnrestrictedGameComponent>();
    
    public void Awake()
    {
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

        foreach (var gameComponent in gameComponents)
        {
            gameComponent.InitializeOnGameStart(playerBlob);
        }

        foreach (var unrestrictedGameComponent in unrestrictedGameComponents)
        {
            unrestrictedGameComponent.InitializeOnGameStart_Dangerous(playerBlob);
        }

        if (sceneTransitioner.TryChangeScene(startSceneName) == false)
        {
            Debug.LogError("Failed to load start scene: " + startSceneName + ". Perhaps it's not registered in the build settings?");
            
            // aisara => Quit the application if we can't load the start scene because the game is in an undefined state
            Application.Quit();
        }
    }
}

