#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Entry point to jump start the game loop
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private PlayerBlobLoaderSO? playerBlobLoader;
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private SceneReference startScene = new SceneReference();

    // TODO: aisara => "GameComponent" is probably not a good name because it's not very descriptive and doesn't capture the essence of being initialized with a player blob. Think of a better name later
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

        if (sceneTransitioner.TryChangeScene(startScene.sceneName) == false)
        {
            Debug.LogError("Failed to load start scene: " + startScene + ". Perhaps it's not registered in the build settings?");

            // aisara => Quit the application if we can't load the start scene because the game is in an undefined state
            Application.Quit();
        }
    }
}
