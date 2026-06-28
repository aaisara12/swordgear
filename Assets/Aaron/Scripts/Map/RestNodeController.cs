#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rest node overlay (shown on the Map scene). Visibility is driven by a Bool channel; confirming
/// fully heals the player and completes the node via <see cref="RunManager.ConfirmRest"/>.
/// </summary>
public class RestNodeController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? visibilityChannel;

    [Header("Scene References")]
    [SerializeField] private GameObject? view;
    [SerializeField] private Button? confirmButton;

    private void Awake()
    {
        if (visibilityChannel == null)
        {
            Debug.LogError("RestNodeController: visibilityChannel is null");
            return;
        }

        if (view == null)
        {
            Debug.LogError("RestNodeController: view is null");
            return;
        }

        view.SetActive(false);
        visibilityChannel.OnDataChanged += HandleVisibilityChanged;

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmPressed);
        }
    }

    private void OnDestroy()
    {
        if (visibilityChannel != null)
        {
            visibilityChannel.OnDataChanged -= HandleVisibilityChanged;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmPressed);
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (view != null)
        {
            view.SetActive(isVisible);
        }
    }

    /// <summary>Hooked to the Rest overlay's "Rest" / confirm button.</summary>
    public void OnConfirmPressed()
    {
        RunManager.Instance?.ConfirmRest();
    }
}
