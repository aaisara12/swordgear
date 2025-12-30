#nullable enable

using UnityEngine;

/// <summary>
/// Plays the defeated outro and transitions back to a specified scene when player is defeated.
/// </summary>
public class DefeatedOutroPlayer : MonoBehaviour
{
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private SceneReference sceneToReturnTo = new SceneReference();
    
    [Header("Input")] [SerializeField] private TriggerEventChannelSO? onDefeatedEventChannel;

    private void Awake()
    {
        if (onDefeatedEventChannel == null)
        {
            Debug.LogError($"{nameof(onDefeatedEventChannel)} is null");
            return;
        }

        if (sceneTransitioner == null)
        {
            Debug.LogError($"{nameof(sceneTransitioner)} is null");
            return;
        }

        onDefeatedEventChannel.OnEventTriggered += HandleOnDefeated;
    }

    private void HandleOnDefeated()
    {
        // TODO: aisara => Perhaps show some defeated outro animation or screen here before transitioning
        sceneTransitioner?.TryChangeScene(sceneToReturnTo.sceneName);
    }

    private void OnDestroy()
    {
        if (onDefeatedEventChannel != null)
        {
            onDefeatedEventChannel.OnEventTriggered -= HandleOnDefeated;
        }
    }
}
