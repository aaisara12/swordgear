// csharp
#nullable enable

using UnityEngine;
using DG.Tweening;

public class LoadingScreenAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup? canvasGroup;
    private Tween? fadeTween;

    private const float DefaultDuration = 0.5f;

    public void FadeInLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        StartFade(1f, DefaultDuration);
    }

    public void FadeOutLoadingScreen()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null! Can't animate loading screen.");
            return;
        }

        StartFade(0f, DefaultDuration);
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