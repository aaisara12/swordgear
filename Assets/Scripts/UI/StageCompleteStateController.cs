#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the "Stage Complete" overlay with a performance summary and a Continue button.
/// Visibility and data are driven by event channels (mirrors the shop UI state controller pattern);
/// Continue raises a trigger channel that the run/node flow listens to in order to return to the map.
/// </summary>
public class StageCompleteStateController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? visibilityChannel;
    [SerializeField] private ComboPerformanceEventChannelSO? performanceChannel;

    [Header("Output")]
    [SerializeField] private TriggerEventChannelSO? continueChannel;

    [Header("Scene References")]
    [SerializeField] private GameObject? view;
    [SerializeField] private TMP_Text? pointsText;
    [SerializeField] private TMP_Text? maxComboText;
    [SerializeField] private Button? continueButton;

    private void Awake()
    {
        if (view == null)
        {
            Debug.LogError("StageCompleteStateController: view is null");
            return;
        }

        if (visibilityChannel == null)
        {
            Debug.LogError("StageCompleteStateController: visibilityChannel is null");
            return;
        }

        view.SetActive(false);

        visibilityChannel.OnDataChanged += HandleVisibilityChanged;

        if (performanceChannel != null)
        {
            performanceChannel.OnDataChanged += HandlePerformanceChanged;
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void OnDestroy()
    {
        if (visibilityChannel != null)
        {
            visibilityChannel.OnDataChanged -= HandleVisibilityChanged;
        }

        if (performanceChannel != null)
        {
            performanceChannel.OnDataChanged -= HandlePerformanceChanged;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (view != null)
        {
            view.SetActive(isVisible);
        }
    }

    private void HandlePerformanceChanged(ComboPerformance performance)
    {
        if (pointsText != null)
        {
            pointsText.text = performance.TotalPointsThisLevel.ToString();
        }

        if (maxComboText != null)
        {
            maxComboText.text = $"{performance.MaxComboThisLevel}x";
        }
    }

    /// <summary>
    /// Hooked to the Continue button. Hides the overlay and signals the run flow to return to the map.
    /// </summary>
    public void OnContinuePressed()
    {
        if (view != null)
        {
            view.SetActive(false);
        }

        continueChannel?.RaiseEventTriggered();
    }
}
