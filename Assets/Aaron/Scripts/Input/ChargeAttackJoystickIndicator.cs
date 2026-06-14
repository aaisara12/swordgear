#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Elemental charge overlay on the attack joystick range ring. Leaves the base joystick visuals intact.
/// </summary>
public class ChargeAttackJoystickIndicator : MonoBehaviour
{
    [SerializeField] private Image? rangeIndicatorImage;
    [SerializeField] private Image? chargeOverlayImage;
    [SerializeField] private Image? chargeGlowImage;
    [SerializeField] private float overlayAlpha = 0.72f;
    [SerializeField] private float glowAlpha = 0.38f;
    [SerializeField] private float minOverlayScale = 0.04f;
    [SerializeField] private float glowScaleMultiplier = 1.18f;
    [SerializeField] private float maxChargeGlowScaleMultiplier = 1.22f;
    [SerializeField] private float maxChargePulseDuration = 0.45f;
    [SerializeField] private float maxChargePulseScale = 0.06f;

    private RectTransform? _overlayRect;
    private RectTransform? _glowRect;
    private bool _isCharging;
    private MaxChargePulseTracker _maxChargePulse;

    private void Awake()
    {
        if (rangeIndicatorImage == null)
        {
            Transform? rangeIndicator = transform.Find("Range Indicator");
            rangeIndicatorImage = rangeIndicator != null ? rangeIndicator.GetComponent<Image>() : null;
        }

        if (rangeIndicatorImage == null)
        {
            Debug.LogError($"{nameof(ChargeAttackJoystickIndicator)}: Range Indicator Image is missing.");
            return;
        }

        EnsureOverlays();
    }

    private void LateUpdate()
    {
        if (chargeOverlayImage == null || _overlayRect == null)
        {
            return;
        }

        ElementManager? elementManager = ElementManager.Instance;
        if (elementManager == null
            || !elementManager.TryGetMeleeChargeDisplayState(out MeleeChargeDisplayState state))
        {
            if (_isCharging)
            {
                HideOverlays();
            }

            return;
        }

        ApplyOverlays(state);
    }

    private void EnsureOverlays()
    {
        if (rangeIndicatorImage == null)
        {
            return;
        }

        RectTransform rangeRect = rangeIndicatorImage.rectTransform;
        chargeGlowImage ??= FindOrCreateOverlay(rangeRect, "Charge Glow", insertFirst: true);
        chargeOverlayImage ??= FindOrCreateOverlay(rangeRect, "Charge Overlay", insertFirst: false);

        _glowRect = chargeGlowImage?.rectTransform;
        _overlayRect = chargeOverlayImage?.rectTransform;

        if (_glowRect != null)
        {
            _glowRect.localScale = Vector3.one * minOverlayScale;
        }

        if (_overlayRect != null)
        {
            _overlayRect.localScale = Vector3.one * minOverlayScale;
        }
    }

    private static Image? FindOrCreateOverlay(RectTransform rangeRect, string overlayName, bool insertFirst)
    {
        Transform? existing = rangeRect.Find(overlayName);
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            return existingImage;
        }

        Image? template = rangeRect.GetComponent<Image>();
        if (template == null)
        {
            return null;
        }

        var overlayObject = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var overlayRect = overlayObject.GetComponent<RectTransform>();

        overlayRect.SetParent(rangeRect, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.localRotation = Quaternion.identity;

        if (insertFirst)
        {
            overlayRect.SetAsFirstSibling();
        }
        else
        {
            overlayRect.SetAsLastSibling();
        }

        Image image = overlayObject.GetComponent<Image>();
        image.sprite = template.sprite;
        image.material = template.material;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.enabled = false;
        return image;
    }

    private void ApplyOverlays(MeleeChargeDisplayState state)
    {
        if (chargeOverlayImage == null || _overlayRect == null)
        {
            return;
        }

        _isCharging = true;

        float easedProgress = ElementVisualUtility.EaseOutQuad(state.Progress);
        float pulseEnvelope = ElementVisualUtility.StepMaxChargePulse(
            state.IsMaxCharge,
            ref _maxChargePulse,
            maxChargePulseDuration);

        float coreScale = Mathf.Lerp(minOverlayScale, 1f, easedProgress)
            * (1f + pulseEnvelope * maxChargePulseScale);
        _overlayRect.localScale = Vector3.one * coreScale;

        Color coreColor = ElementVisualUtility.GetChargeOverlayColor(state.Element, easedProgress);
        coreColor.a = overlayAlpha * Mathf.Lerp(0.45f, 1f, easedProgress);
        chargeOverlayImage.enabled = true;
        chargeOverlayImage.color = coreColor;

        if (chargeGlowImage == null || _glowRect == null)
        {
            return;
        }

        float glowMultiplier = Mathf.Lerp(glowScaleMultiplier, maxChargeGlowScaleMultiplier, easedProgress);
        _glowRect.localScale = Vector3.one * (coreScale * glowMultiplier);

        Color glowColor = ElementVisualUtility.GetChargeGlowColor(state.Element, easedProgress);
        glowColor.a = glowAlpha * Mathf.Lerp(0.25f, 1f, easedProgress);
        chargeGlowImage.enabled = true;
        chargeGlowImage.color = glowColor;
    }

    private void HideOverlays()
    {
        _isCharging = false;
        _maxChargePulse.Reset();

        if (chargeOverlayImage != null)
        {
            chargeOverlayImage.enabled = false;
        }

        if (chargeGlowImage != null)
        {
            chargeGlowImage.enabled = false;
        }

        if (_overlayRect != null)
        {
            _overlayRect.localScale = Vector3.one * minOverlayScale;
        }

        if (_glowRect != null)
        {
            _glowRect.localScale = Vector3.one * minOverlayScale;
        }
    }
}
