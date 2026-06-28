#nullable enable

using UnityEngine;

/// <summary>
/// Activates/deactivates a target GameObject in response to a Bool event channel.
/// Used e.g. to hide the CombatHUD on the Map scene and show it in the Arena.
/// </summary>
public class ChannelDrivenVisibility : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO? visibilityChannel;
    [SerializeField] private GameObject? target;
    [SerializeField] private bool initiallyVisible = true;

    private void Awake()
    {
        if (target == null)
        {
            target = gameObject;
        }

        if (visibilityChannel == null)
        {
            Debug.LogError("ChannelDrivenVisibility: visibilityChannel is null");
            return;
        }

        target.SetActive(initiallyVisible);
        visibilityChannel.OnDataChanged += HandleVisibilityChanged;
    }

    private void OnDestroy()
    {
        if (visibilityChannel != null)
        {
            visibilityChannel.OnDataChanged -= HandleVisibilityChanged;
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (target != null)
        {
            target.SetActive(isVisible);
        }
    }
}
