using UnityEngine;
using System.Collections;

public class ShopAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private RectTransform panelRectTransform;

    [Header("Settings")]
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private float targetAlpha = 1.0f;
    [SerializeField] private Vector3 targetScale = Vector3.one;

    private Vector2 originalPosition;
    private Vector2 offscreenPosition;
    private Coroutine animationRoutine;

    // Use Awake to capture the permanent "Home" position of the UI
    void Awake()
    {
        originalPosition = panelRectTransform.anchoredPosition;
        offscreenPosition = new Vector2(0, -Screen.height);
    }

    void OnEnable()
    {
        // 1. Reset state immediately
        ResetUI();

        // 2. Stop any previous animation that might still be running
        if (animationRoutine != null) StopCoroutine(animationRoutine);

        // 3. Start the animation
        animationRoutine = StartCoroutine(AnimateIntro());
    }

    private void ResetUI()
    {
        panelRectTransform.localScale = Vector3.zero;
        panelRectTransform.anchoredPosition = offscreenPosition;
        if (backgroundCanvasGroup != null) backgroundCanvasGroup.alpha = 0;
    }

    private IEnumerator AnimateIntro()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            // Use SmoothStep for a more professional "Slow down at the end" feel
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration);

            if (backgroundCanvasGroup != null)
                backgroundCanvasGroup.alpha = Mathf.Lerp(0, targetAlpha, t);

            panelRectTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            panelRectTransform.anchoredPosition = Vector2.Lerp(offscreenPosition, originalPosition, t);

            yield return null;
        }

        // Ensure final values
        panelRectTransform.localScale = targetScale;
        panelRectTransform.anchoredPosition = originalPosition;
        if (backgroundCanvasGroup != null) backgroundCanvasGroup.alpha = targetAlpha;
    }
}