#nullable enable

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a compact stage-complete performance summary, holds at full opacity, then auto-fades.
/// Portal dismiss starts a fade without blocking scene flow; the map scene snaps it off via combat-HUD hide.
/// </summary>
public class StageCompleteStateController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? visibilityChannel;
    [SerializeField] private ComboPerformanceEventChannelSO? performanceChannel;
    [Tooltip("When the combat HUD is hidden (e.g. on the Map scene), stage complete is snapped off.")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    [Header("Timing")]
    [SerializeField] private float displayHoldSeconds = 4f;
    [SerializeField] private float fadeDurationSeconds = 1f;

    [Header("Scene References")]
    [SerializeField] private GameObject? view;
    [SerializeField] private CanvasGroup? canvasGroup;
    [SerializeField] private TMP_Text? pointsText;
    [SerializeField] private TMP_Text? maxComboText;

    private Coroutine? _autoHideRoutine;
    private bool _isShowing;

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

        HideImmediately();
        visibilityChannel.OnDataChanged += HandleVisibilityChanged;

        if (performanceChannel != null)
        {
            performanceChannel.OnDataChanged += HandlePerformanceChanged;
        }

        if (combatHudVisibilityChannel != null)
        {
            combatHudVisibilityChannel.OnDataChanged += HandleCombatHudVisibilityChanged;
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

        if (combatHudVisibilityChannel != null)
        {
            combatHudVisibilityChannel.OnDataChanged -= HandleCombatHudVisibilityChanged;
        }
    }

    private void HandleCombatHudVisibilityChanged(bool isVisible)
    {
        if (!isVisible)
        {
            HideImmediately();
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (!isVisible)
        {
            DismissWithFade();
            return;
        }

        Show();
    }

    private void DismissWithFade()
    {
        StopAutoHideRoutine();

        if (!_isShowing)
        {
            return;
        }

        _autoHideRoutine = StartCoroutine(PortalDismissFade());
    }

    private IEnumerator PortalDismissFade()
    {
        yield return FadeOut();
        _autoHideRoutine = null;
    }

    private void Show()
    {
        StopAutoHideRoutine();

        if (view == null || canvasGroup == null)
        {
            return;
        }

        view.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        _isShowing = true;
        _autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    private void HideImmediately()
    {
        StopAutoHideRoutine();

        if (view == null || canvasGroup == null)
        {
            return;
        }

        _isShowing = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void StopAutoHideRoutine()
    {
        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }
    }

    private IEnumerator AutoHideAfterDelay()
    {
        if (displayHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(displayHoldSeconds);
        }

        if (!_isShowing)
        {
            yield break;
        }

        yield return FadeOut();
        _autoHideRoutine = null;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (fadeDurationSeconds > 0f)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDurationSeconds);
                yield return null;
            }
        }

        HideImmediately();
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
