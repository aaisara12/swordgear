#nullable enable

using UnityEngine;

/// <summary>
/// Title screen Play button: transitions to the linear map scene.
/// Commit 02: scene load only — RunManager queue wiring comes in commit 03.
/// </summary>
public class TitlePlayButtonHandler : MonoBehaviour
{
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;
    [SerializeField] private string mapSceneName = "Map";

    private void Awake()
    {
        if (sceneChangeRequestChannel == null)
        {
            Debug.LogError($"{nameof(TitlePlayButtonHandler)}: {nameof(sceneChangeRequestChannel)} is null");
        }
    }

    public void OnPlayClicked()
    {
        if (sceneChangeRequestChannel == null)
        {
            return;
        }

        sceneChangeRequestChannel.RaiseDataChanged(mapSceneName);
    }
}
