#nullable enable

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the defeat overlay ("DEFEATED" + subtitle), hides gameplay HUD, ducks BGM,
/// auto-returns to Title after a short hold (skippable via overlay tap).
/// </summary>
public class DefeatStateController : MonoBehaviour
{
    private const float DefaultHoldSeconds = 3f;
    private const float DefaultFadeInSeconds = 0.4f;
    private const float BgmDuckVolume = 0.2f;
    private const float BgmDuckDuration = 0.5f;

    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? visibilityChannel;
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    [Header("Output")]
    [SerializeField] private TriggerEventChannelSO? continueChannel;

    [Header("Scene References")]
    [SerializeField] private GameObject? view;
    [SerializeField] private CanvasGroup? overlayGroup;
    [SerializeField] private TMP_Text? headlineText;
    [SerializeField] private TMP_Text? subtitleText;
    [SerializeField] private TMP_Text? skipHintText;
    [SerializeField] private Button? skipButton;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = DefaultFadeInSeconds;
    [SerializeField] private float holdDuration = DefaultHoldSeconds;
    [SerializeField] private float skipHintDelay = 1f;

    private Coroutine? _sequenceRoutine;
    private bool _sequenceActive;

    private void Awake()
    {
        if (view == null)
        {
            Debug.LogError("DefeatStateController: view is null");
            return;
        }

        if (visibilityChannel == null)
        {
            Debug.LogError("DefeatStateController: visibilityChannel is null");
            return;
        }

        // Subscribe before hiding view — this component must live on an active GameObject at scene load
        // or Awake never runs and defeat events are never received.
        visibilityChannel.OnDataChanged += HandleVisibilityChanged;

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipPressed);
        }

        view.SetActive(false);
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
        }

        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (visibilityChannel != null)
        {
            visibilityChannel.OnDataChanged -= HandleVisibilityChanged;
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipPressed);
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        if (isVisible)
        {
            BeginSequence();
            return;
        }

        CancelSequence(resetOverlay: true);
        AudioSystem.RestoreLoopVolume(AudioSystem.Sound.BGM, BgmDuckDuration);
    }

    private void BeginSequence()
    {
        CancelSequence(resetOverlay: false);

        if (view != null)
        {
            view.SetActive(true);
        }

        if (headlineText != null)
        {
            headlineText.text = "DEFEATED";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Your run is over.";
        }

        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(false);
        }

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
        }

        combatHudVisibilityChannel?.RaiseDataChanged(false);
        AudioSystem.FadeLoopVolume(AudioSystem.Sound.BGM, BgmDuckVolume, BgmDuckDuration);

        _sequenceActive = true;
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        if (overlayGroup != null && fadeInDuration > 0f)
        {
            yield return FadeOverlay(1f, fadeInDuration);
        }
        else if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
        }

        if (skipHintText != null && skipHintDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(skipHintDelay);
            skipHintText.gameObject.SetActive(true);
        }

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        CompleteSequence();
    }

    private void OnSkipPressed()
    {
        if (!_sequenceActive)
        {
            return;
        }

        CompleteSequence();
    }

    private void CompleteSequence()
    {
        if (!_sequenceActive)
        {
            return;
        }

        _sequenceActive = false;

        if (view != null)
        {
            view.SetActive(false);
        }

        visibilityChannel?.RaiseDataChanged(false);
        continueChannel?.RaiseEventTriggered();

        CancelSequence(resetOverlay: false);
    }

    private void CancelSequence(bool resetOverlay)
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        _sequenceActive = false;

        if (!resetOverlay)
        {
            return;
        }

        if (view != null)
        {
            view.SetActive(false);
        }

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
        }

        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (overlayGroup == null)
        {
            yield break;
        }

        float startAlpha = overlayGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        overlayGroup.alpha = targetAlpha;
    }
}
