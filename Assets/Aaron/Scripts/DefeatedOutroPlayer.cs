#nullable enable

using UnityEngine;

/// <summary>
/// Plays the defeated outro and transitions back to a specified scene when player is defeated.
/// </summary>
public class DefeatedOutroPlayer : MonoBehaviour
{
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private SceneReference sceneToReturnTo = new SceneReference();
    
    [Header("Input")] [SerializeField] private TriggerEventChannelSO? defeatContinueChannel;

    private void Awake()
    {
        if (defeatContinueChannel == null)
        {
            Debug.LogError($"{nameof(defeatContinueChannel)} is null");
            return;
        }

        if (sceneTransitioner == null)
        {
            Debug.LogError($"{nameof(sceneTransitioner)} is null");
            return;
        }

        defeatContinueChannel.OnEventTriggered += HandleDefeatContinue;
    }

    private void HandleDefeatContinue()
    {
        if (sceneTransitioner == null)
        {
            return;
        }

        TriggerDefeatedOutro(sceneTransitioner);
    }
    
    private void TriggerDefeatedOutro(SceneTransitioner sceneTransitionerNonNull)
    {
        // Defeat outro timing is handled by DefeatStateController before this fires.
        if (sceneTransitionerNonNull.TryChangeScene(sceneToReturnTo.sceneName) == false)
        {
            Debug.LogError("Failed to transition to scene on player defeated.");
        }
    }

    private void OnDestroy()
    {
        if (defeatContinueChannel != null)
        {
            defeatContinueChannel.OnEventTriggered -= HandleDefeatContinue;
        }
    }
}
