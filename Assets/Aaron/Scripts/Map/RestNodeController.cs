#nullable enable

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DEPRECATED — Rest overlay for the branching map; not used by the linear rail flow.
/// </summary>
[Obsolete("DEPRECATED: Branching map Rest overlay. Retained for reference.")]
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
