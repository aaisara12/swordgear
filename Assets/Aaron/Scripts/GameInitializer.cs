#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entry point to jump start the game loop
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private PlayerBlobLoaderSO? playerBlobLoader;
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private SceneReference startScene = new SceneReference();
    [SerializeField] private VfxPrewarmer? vfxPrewarmer;

    [Header("Loading")]
    [SerializeField] private BoolEventChannelSO? toggleLoadingScreenChannel;
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    [SerializeField] private List<InitializeableGameComponent> gameComponents = new List<InitializeableGameComponent>();
    [SerializeField] private List<InitializeableUnrestrictedGameComponent> unrestrictedGameComponents = new List<InitializeableUnrestrictedGameComponent>();
    [SerializeField] private List<InitializeableObject> standaloneManagers = new ();

    void Awake()
    {
        StartCoroutine(BootRoutine());
    }

    IEnumerator BootRoutine()
    {
        if (playerBlobLoader == null)
        {
            Debug.LogError($"{nameof(playerBlobLoader)} is null");
            yield break;
        }

        if (sceneTransitioner == null)
        {
            Debug.LogError($"{nameof(sceneTransitioner)} is null");
            yield break;
        }

        toggleLoadingScreenChannel?.RaiseDataChanged(true);
        combatHudVisibilityChannel?.RaiseDataChanged(false);

        if (playerBlobLoader.TryLoad(out var playerBlob) == false)
        {
            Debug.LogWarning("Player blob could not be loaded at this time. Using empty player blob.");
            playerBlob = new PlayerBlob();
        }

        playerBlob.ThrowIfNull(nameof(playerBlob));

        foreach (var gameComponent in gameComponents)
            gameComponent.InitializeOnGameStart(playerBlob);

        foreach (var unrestrictedGameComponent in unrestrictedGameComponents)
            unrestrictedGameComponent.InitializeOnGameStart_Dangerous(playerBlob);

        foreach (var standaloneManager in standaloneManagers)
            standaloneManager.InitializeOnGameStart_Dangerous(playerBlob);

        yield return null;

        if (vfxPrewarmer != null)
            yield return vfxPrewarmer.RunWarmup();

        if (sceneTransitioner.TryChangeScene(startScene.sceneName) == false)
        {
            Debug.LogError("Failed to load start scene: " + startScene + ". Perhaps it's not registered in the build settings?");
            Application.Quit();
        }
    }
}
