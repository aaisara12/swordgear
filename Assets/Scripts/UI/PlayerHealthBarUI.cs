#nullable enable

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime logic for the PlayerHealthBar prefab. Layout, colors, and sprites are authored on the prefab Images.
/// Fill uses its Image color at full health; low-health tint lerps toward DamageFill's Image color.
/// </summary>
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Bar")]
    [SerializeField] private CanvasGroup? canvasGroup;
    [SerializeField] private RectTransform? barContainer;
    [SerializeField] private Image? damageFillImage;
    [SerializeField] private Image? fillImage;
    [SerializeField] private TMP_Text? hpText;

    [Header("Low Health")]
    [SerializeField]
    [Range(0.05f, 0.5f)]
    private float lowHealthThreshold = 0.3f;

    [Header("Animation")]
    [SerializeField] private float damageChunkDelay = 0.35f;
    [SerializeField] private float healLerpSpeed = 6f;
    [SerializeField] private float damageFlashDuration = 0.12f;
    [SerializeField] private float damageShakeDuration = 0.18f;
    [SerializeField] private float damageShakeStrength = 6f;
    [SerializeField] private float damagePopScale = 1.08f;

    private Color _fullFillColor;
    private Color _lowFillColor;
    private float _currentFill;
    private float _damageFill;
    private float _targetFill;
    private float _targetDamageFill;
    private bool _isVisible;
    private Coroutine? _damageChunkRoutine;
    private Coroutine? _damageFeedbackRoutine;
    private Vector3 _barBaseScale = Vector3.one;
    private Vector2 _barBaseAnchoredPosition;

    private void Awake()
    {
        if (barContainer == null || fillImage == null || damageFillImage == null)
        {
            Debug.LogError("PlayerHealthBarUI: required references are missing. Check the PlayerHealthBar prefab.");
            enabled = false;
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        _fullFillColor = fillImage.color;
        _lowFillColor = damageFillImage.color;
        _barBaseScale = barContainer.localScale;
        _barBaseAnchoredPosition = barContainer.anchoredPosition;

        SetVisible(false);
        ApplyFillImmediate(0f, 0f);
    }

    private void OnEnable()
    {
        PlayerGameplayManager.OnHealthChanged += HandleHealthChanged;

        if (PlayerGameplayManager.Instance != null && PlayerGameplayManager.Instance.IsPawnActive)
        {
            ApplySnapshot(new PlayerHealthSnapshot(
                PlayerGameplayManager.Instance.CurrentHp,
                PlayerGameplayManager.Instance.MaxHp,
                0f));
        }
    }

    private void OnDisable()
    {
        PlayerGameplayManager.OnHealthChanged -= HandleHealthChanged;

        if (_damageChunkRoutine != null)
        {
            StopCoroutine(_damageChunkRoutine);
            _damageChunkRoutine = null;
        }

        if (_damageFeedbackRoutine != null)
        {
            StopCoroutine(_damageFeedbackRoutine);
            _damageFeedbackRoutine = null;
        }

        ResetBarContainerTransform();
    }

    private void Update()
    {
        if (!_isVisible)
        {
            return;
        }

        _currentFill = Mathf.MoveTowards(_currentFill, _targetFill, healLerpSpeed * Time.deltaTime);
        _damageFill = Mathf.MoveTowards(_damageFill, _targetDamageFill, healLerpSpeed * Time.deltaTime);
        ApplyFillVisuals();
    }

    private void HandleHealthChanged(PlayerHealthSnapshot snapshot)
    {
        if (snapshot.Max <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(PlayerHealthSnapshot snapshot)
    {
        float normalized = snapshot.Max > 0f ? snapshot.Current / snapshot.Max : 0f;
        normalized = Mathf.Clamp01(normalized);

        if (snapshot.Delta < 0f)
        {
            _targetFill = normalized;
            _currentFill = normalized;

            if (_damageChunkRoutine != null)
            {
                StopCoroutine(_damageChunkRoutine);
            }

            _damageChunkRoutine = StartCoroutine(DamageChunkRoutine(normalized));
            PlayDamageFeedback();
        }
        else
        {
            _targetFill = normalized;
            _targetDamageFill = normalized;
        }

        UpdateHpText(snapshot.Current, snapshot.Max);
        ApplyFillVisuals();
    }

    private IEnumerator DamageChunkRoutine(float normalizedCurrent)
    {
        yield return new WaitForSeconds(damageChunkDelay);
        _targetDamageFill = normalizedCurrent;
        _damageChunkRoutine = null;
    }

    private void PlayDamageFeedback()
    {
        if (barContainer == null)
        {
            return;
        }

        if (_damageFeedbackRoutine != null)
        {
            StopCoroutine(_damageFeedbackRoutine);
        }

        ResetBarContainerTransform();
        _damageFeedbackRoutine = StartCoroutine(DamageFeedbackRoutine());
    }

    private IEnumerator DamageFeedbackRoutine()
    {
        if (barContainer == null)
        {
            _damageFeedbackRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        Vector3 popScale = _barBaseScale * damagePopScale;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / damageFlashDuration);
            barContainer.localScale = Vector3.Lerp(popScale, _barBaseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < damageShakeDuration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / damageShakeDuration);
            float offsetX = Mathf.Sin(elapsed * 60f) * damageShakeStrength * decay;
            barContainer.anchoredPosition = _barBaseAnchoredPosition + new Vector2(offsetX, 0f);
            yield return null;
        }

        ResetBarContainerTransform();
        _damageFeedbackRoutine = null;
    }

    private void ResetBarContainerTransform()
    {
        if (barContainer == null)
        {
            return;
        }

        barContainer.localScale = _barBaseScale;
        barContainer.anchoredPosition = _barBaseAnchoredPosition;
    }

    private void ApplyFillImmediate(float current, float damage)
    {
        _currentFill = current;
        _damageFill = damage;
        _targetFill = current;
        _targetDamageFill = damage;
        ApplyFillVisuals();
    }

    private void ApplyFillVisuals()
    {
        SetHorizontalFill(damageFillImage?.rectTransform, _damageFill);
        SetHorizontalFill(fillImage?.rectTransform, _currentFill);

        if (fillImage != null)
        {
            float colorT = Mathf.Clamp01(_currentFill / lowHealthThreshold);
            fillImage.color = Color.Lerp(_lowFillColor, _fullFillColor, colorT);
        }
    }

    private static void SetHorizontalFill(RectTransform? rect, float normalized)
    {
        if (rect == null)
        {
            return;
        }

        normalized = Mathf.Clamp01(normalized);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(normalized, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void UpdateHpText(float current, float max)
    {
        if (hpText == null)
        {
            return;
        }

        hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void SetVisible(bool visible)
    {
        _isVisible = visible;

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
