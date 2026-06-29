#nullable enable

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a compact stage-complete performance summary, then auto-fades after a short hold.
/// </summary>
public class StageCompleteStateController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? visibilityChannel;
    [SerializeField] private ComboPerformanceEventChannelSO? performanceChannel;

    [Header("Timing")]
    [SerializeField] private float displayHoldSeconds = 2f;
    [SerializeField] private float fadeDurationSeconds = 1f;

    [Header("Scene References")]
    [SerializeField] private GameObject? view;
    [SerializeField] private CanvasGroup? canvasGroup;
    [SerializeField] private TMP_Text? pointsText;
    [SerializeField] private TMP_Text? maxComboText;

    private Coroutine? _autoHideRoutine;

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

        if (canvasGroup == null)
        {
            canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogError("StageCompleteStateController: canvasGroup is null");
                return;
            }
        }

        view.SetActive(false);
        canvasGroup.alpha = 1f;

        visibilityChannel.OnDataChanged += HandleVisibilityChanged;

        if (performanceChannel != null)
        {
            performanceChannel.OnDataChanged += HandlePerformanceChanged;
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
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }

        if (view == null || canvasGroup == null)
        {
            return;
        }

        if (!isVisible)
        {
            view.SetActive(false);
            canvasGroup.alpha = 1f;
            return;
        }

        view.SetActive(true);
        canvasGroup.alpha = 1f;
        _autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    private IEnumerator AutoHideAfterDelay()
    {
        if (displayHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(displayHoldSeconds);
        }

        if (canvasGroup != null && fadeDurationSeconds > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDurationSeconds);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        if (view != null)
        {
            view.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        visibilityChannel?.RaiseDataChanged(false);
        _autoHideRoutine = null;
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
}
