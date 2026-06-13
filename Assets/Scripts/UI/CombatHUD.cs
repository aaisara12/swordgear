#nullable enable annotations

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles on-screen display for the combo meter and ultimate bar,
/// and provides input hooks for triggering the ultimate.
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [Header("Combo UI")]
    [SerializeField] private Slider? comboTimerSlider;
    [SerializeField] private TMP_Text? comboCountText;
    [SerializeField] private TMP_Text? multiplierText;
    [SerializeField] private CanvasGroup? comboGroup;
    
    [Header("Combo Fade")]
    [SerializeField] private float comboFadeInDuration = 0.15f;
    [SerializeField] private float comboFadeOutDuration = 0.4f;
    
    [Header("Combo Number Pop")]
    [SerializeField] private float comboPopScale = 1.4f;
    [SerializeField] private float comboPopDuration = 0.15f;
    
    [Header("Total Points UI")]
    [SerializeField] private TMP_Text? totalPointsText;

    [Header("Ultimate UI")]
    [SerializeField] private Slider? ultimateSlider;
    [SerializeField] private GameObject? ultimateReadyIndicator;

    [Header("Input")]
    [SerializeField] private KeyCode ultimateKey = KeyCode.Q;
    [SerializeField] private UltimateMeter? ultimateMeter;

    private ComboSystem? _comboSystem;
    private bool _isComboActive;
    private Coroutine? _comboFadeRoutine;
    private Coroutine? _comboPopRoutine;
    private int _lastComboCount;

    private void Start()
    {
        _comboSystem = ComboSystem.Instance;

        if (_comboSystem != null)
        {
            _comboSystem.OnComboChanged += HandleComboChanged;
            _comboSystem.OnComboTimerChanged += HandleComboTimerChanged;
            _comboSystem.OnMultiplierChanged += HandleMultiplierChanged;
            _comboSystem.OnComboBroken += HandleComboBroken;
            _comboSystem.OnLevelPointsChanged += HandleLevelPointsChanged;
            
            // Initialize total points display
            if (totalPointsText != null)
            {
                totalPointsText.text = "0";
            }
        }

        if (ultimateMeter == null)
        {
            ultimateMeter = FindFirstObjectByType<UltimateMeter>();
        }

        if (ultimateMeter != null)
        {
            ultimateMeter.OnUltimateChargeChanged += HandleUltimateChargeChanged;
            ultimateMeter.OnUltimateReady += HandleUltimateReady;
        }

        UpdateComboVisibility(false);
        UpdateUltimateReady(false);
    }

    private void OnDestroy()
    {
        if (_comboSystem != null)
        {
            _comboSystem.OnComboChanged -= HandleComboChanged;
            _comboSystem.OnComboTimerChanged -= HandleComboTimerChanged;
            _comboSystem.OnMultiplierChanged -= HandleMultiplierChanged;
            _comboSystem.OnComboBroken -= HandleComboBroken;
            _comboSystem.OnLevelPointsChanged -= HandleLevelPointsChanged;
        }

        if (ultimateMeter != null)
        {
            ultimateMeter.OnUltimateChargeChanged -= HandleUltimateChargeChanged;
            ultimateMeter.OnUltimateReady -= HandleUltimateReady;
        }
    }

    private void Update()
    {
        if (ultimateMeter == null)
        {
            return;
        }

        if (Input.GetKeyDown(ultimateKey))
        {
            TryUseUltimate();
        }
    }

    private void HandleComboChanged(int comboCount, int multiplier)
    {
        _isComboActive = comboCount > 0;
        
        if (comboCountText != null)
        {
            comboCountText.text = comboCount > 0 ? comboCount.ToString() : string.Empty;
            
            // Pop animation when combo count increases (not on first hit from 0)
            if (comboCount > 0 && comboCount > _lastComboCount)
            {
                if (_comboPopRoutine != null)
                {
                    StopCoroutine(_comboPopRoutine);
                }
                _comboPopRoutine = StartCoroutine(ComboPopRoutine());
            }
        }
        _lastComboCount = comboCount;

        if (multiplierText != null)
        {
            // Show multiplier even when it's 1x, as long as combo is active
            multiplierText.text = _isComboActive ? $"{multiplier}x" : string.Empty;
        }

        UpdateComboVisibility(_isComboActive);
    }

    private void HandleComboTimerChanged(float currentTime, float duration)
    {
        if (comboTimerSlider == null || duration <= 0f)
        {
            return;
        }

        comboTimerSlider.value = Mathf.Clamp01(currentTime / duration);
    }

    private void HandleMultiplierChanged(int multiplier)
    {
        if (multiplierText != null)
        {
            // Show multiplier even when it's 1x, as long as combo is active
            multiplierText.text = _isComboActive ? $"{multiplier}x" : string.Empty;
        }
    }

    private void HandleComboBroken()
    {
        _isComboActive = false;
        UpdateComboVisibility(false);
    }

    private void HandleLevelPointsChanged(int totalPoints)
    {
        if (totalPointsText != null)
        {
            totalPointsText.text = totalPoints.ToString();
        }
    }

    private void HandleUltimateChargeChanged(float normalizedCharge)
    {
        if (ultimateSlider != null)
        {
            ultimateSlider.value = Mathf.Clamp01(normalizedCharge);
        }

        if (ultimateReadyIndicator != null && ultimateMeter != null)
        {
            UpdateUltimateReady(ultimateMeter.IsReady);
        }
    }

    private void HandleUltimateReady()
    {
        UpdateUltimateReady(true);
    }

    private void UpdateComboVisibility(bool visible)
    {
        if (comboGroup == null) return;
        
        if (_comboFadeRoutine != null)
        {
            StopCoroutine(_comboFadeRoutine);
        }
        
        float duration = visible ? comboFadeInDuration : comboFadeOutDuration;
        _comboFadeRoutine = StartCoroutine(ComboFadeRoutine(visible, duration));
        
        // Ensure total points text is always visible (not affected by comboGroup)
        if (totalPointsText != null)
        {
            totalPointsText.gameObject.SetActive(true);
            CanvasGroup? totalPointsGroup = totalPointsText.GetComponent<CanvasGroup>();
            if (totalPointsGroup != null)
            {
                totalPointsGroup.alpha = 1f;
            }
        }
    }

    private IEnumerator ComboFadeRoutine(bool visible, float duration)
    {
        if (comboGroup == null)
        {
            _comboFadeRoutine = null;
            yield break;
        }
        
        float startAlpha = comboGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        float elapsed = 0f;
        
        if (duration <= 0f)
        {
            comboGroup.alpha = targetAlpha;
            comboGroup.interactable = visible;
            comboGroup.blocksRaycasts = visible;
            _comboFadeRoutine = null;
            yield break;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            comboGroup.alpha = currentAlpha;
            
            if (t >= 1f)
            {
                comboGroup.interactable = visible;
                comboGroup.blocksRaycasts = visible;
            }
            yield return null;
        }
        
        comboGroup.alpha = targetAlpha;
        comboGroup.interactable = visible;
        comboGroup.blocksRaycasts = visible;
        _comboFadeRoutine = null;
    }

    private IEnumerator ComboPopRoutine()
    {
        if (comboCountText == null)
        {
            _comboPopRoutine = null;
            yield break;
        }
        
        RectTransform? rect = comboCountText.rectTransform;
        if (rect == null)
        {
            _comboPopRoutine = null;
            yield break;
        }
        
        Vector3 baseScale = Vector3.one;
        Vector3 popScale = baseScale * comboPopScale;
        float halfDuration = comboPopDuration * 0.5f;
        float elapsed = 0f;
        
        // Scale up to pop size
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rect.localScale = Vector3.Lerp(baseScale, popScale, t);
            yield return null;
        }
        
        // Scale back down to normal
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rect.localScale = Vector3.Lerp(popScale, baseScale, t);
            yield return null;
        }
        
        rect.localScale = baseScale;
        _comboPopRoutine = null;
    }

    private void UpdateUltimateReady(bool isReady)
    {
        if (ultimateReadyIndicator != null)
        {
            ultimateReadyIndicator.SetActive(isReady);
        }
    }

    private void TryUseUltimate()
    {
        if (ultimateMeter == null)
        {
            return;
        }

        bool used = ultimateMeter.TryUseUltimate();

        if (used)
        {
            UpdateUltimateReady(false);
        }
    }

    /// <summary>
    /// Optional hook for mobile/on-screen button.
    /// </summary>
    public void OnUltimateButtonPressed()
    {
        TryUseUltimate();
    }
}

