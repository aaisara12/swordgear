#nullable enable

using UnityEngine;
using DG.Tweening;

public class LoadingScreenAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup? canvasGroup;
    [SerializeField] private LoadingDotsText? loadingDots;
    private Tween? fadeTween;

    [SerializeField] private float durationOfFadeAnimation = 0.5f;

    // aisara => Lets boot-time systems (e.g. VfxPrewarmer) confirm the overlay fully hides the screen
    // before drawing hidden warmup VFX.
    public bool IsFullyOpaque => canvasGroup != null && canvasGroup.alpha >= 0.99f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is not assigned in LoadingScreenAnimator!");
            return;
        }

        // aisara => Start opaque so boot shows black immediately, even before the first channel event arrives.
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // aisara => Boot label is shown explicitly by VfxPrewarmer; scene transitions stay black-only.
        HideBootLoadingLabel();
    }

    /// <summary>
    /// Shows the animated "Loading..." label for initial boot warmup only.
    /// </summary>
    public void ShowBootLoadingLabel()
    {
        if (loadingDots == null)
            return;

        loadingDots.gameObject.SetActive(true);
        loadingDots.Restart();
    }

    public void HideBootLoadingLabel()
    {
        if (loadingDots == null)
            return;

        loadingDots.gameObject.SetActive(false);
    }

    /// <summary>
    /// Debug/test helper: fade the overlay in and show the boot Loading... label.
    /// </summary>
    public void DebugShowBootLoadingOverlay()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        ShowBootLoadingLabel();
        StartFade(1f, durationOfFadeAnimation);
    }

    public void FadeInLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        // Scene transitions use the black overlay only — no Loading... text.
        HideBootLoadingLabel();
        StartFade(1f, durationOfFadeAnimation);
    }

    public void FadeOutLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        HideBootLoadingLabel();
        StartFade(0f, durationOfFadeAnimation);
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
            fadeTween = null;
        }

        var cg = canvasGroup!;
        if (targetAlpha > 0f)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // Use DOTween.To to tween the CanvasGroup alpha (works even if DOFade extension isn't available)
        fadeTween = DOTween.To(() => cg.alpha, x => cg.alpha = x, targetAlpha, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                cg.alpha = targetAlpha;
                if (targetAlpha <= 0f)
                {
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
                fadeTween = null;
            });
    }

    private void OnDisable()
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
            fadeTween = null;
        }
    }
}
