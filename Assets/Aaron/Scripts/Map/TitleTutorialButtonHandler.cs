#nullable enable

using UnityEngine;

/// <summary>
/// Title screen Tutorial button: transitions to the tutorial scene.
/// </summary>
public class TitleTutorialButtonHandler : MonoBehaviour
{
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;
    [SerializeField] private string tutorialSceneName = "Tutorial";

    private void Awake()
    {
        if (sceneChangeRequestChannel == null)
        {
            Debug.LogError($"{nameof(TitleTutorialButtonHandler)}: {nameof(sceneChangeRequestChannel)} is null");
        }
    }

    public void OnTutorialClicked()
    {
        if (sceneChangeRequestChannel == null)
        {
            return;
        }

        sceneChangeRequestChannel.RaiseDataChanged(tutorialSceneName);
    }
}
