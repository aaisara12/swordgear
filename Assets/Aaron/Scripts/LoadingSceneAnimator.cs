// csharp
#nullable enable

using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class LoadingScreenAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup? canvasGroup;
    private Tween? fadeTween;

    [SerializeField] private float durationOfFadeAnimation = 0.5f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is not assigned in LoadingScreenAnimator!");
            return;
        }
        
        // aisara => Starting state is fully transparent and non-interactable (don't want to have to send an initialization signal to get it in this state)
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeInLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        StartFade(1f, durationOfFadeAnimation);
    }

    public void FadeOutLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

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