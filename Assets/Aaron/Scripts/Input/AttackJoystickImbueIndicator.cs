#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attack mode: Knob shows shadedDark49 (pad + glyph); range ring tints from the active element;
/// imbue border uses full thickness. Imbues don't time out, so the imbue border is a full ring in the element's
/// colour while one is active, and hidden with none.
/// Dash mode: Knob keeps a plain dark pad with DashIcon on top; range-ring tint is suppressed
/// (Physical / gray); imbue border stays visible at half thickness and half opacity until the sword is caught.
/// </summary>
public class AttackJoystickImbueIndicator : MonoBehaviour
{
    private const float DashBorderThicknessScale = 0.5f;
    private const float DashBorderOpacityScale = 0.5f;

    [SerializeField] private Image? iconImage;
    [SerializeField] private Sprite? attackIconSprite;
    [SerializeField] private Sprite? dashIconSprite;
    [SerializeField] private Sprite? knobBackingSprite;
    [SerializeField] private Image? knobImage;
    [SerializeField] private RadialBorderTimer? imbueBorder;
    [SerializeField] private Color inactiveBorderColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color inactiveKnobColor = new(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private float dashIconScale = 0.55f;

    private Element _activeElement = Element.Physical;
    private Image? _modeKnobImage;
    private RectTransform? _dashOverlayRect;
    private bool _inDashMode;
    private float _knobAlpha = 1f;
    private float _normalOuterRadius;
    private float _normalInnerRadius;

    private void Awake()
    {
        Transform? knob = transform.Find("Knob");
        _modeKnobImage = knob != null ? knob.GetComponent<Image>() : null;

        if (knobImage != null)
        {
            _knobAlpha = knobImage.color.a;
        }

        if (iconImage != null)
        {
            _dashOverlayRect = iconImage.rectTransform;
            iconImage.enabled = false;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
        }

        if (imbueBorder != null)
        {
            _normalOuterRadius = imbueBorder.OuterRadius;
            _normalInnerRadius = imbueBorder.InnerRadius;
        }
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += HandleActiveElementChanged;
        if (ElementManager.Instance != null)
        {
            _activeElement = ElementManager.Instance.ActiveElement;
        }

        UpdateModeVisuals();
        RefreshColors();
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= HandleActiveElementChanged;
    }

    private void LateUpdate()
    {
        UpdateModeVisuals();
    }

    private void UpdateModeVisuals()
    {
        if (_modeKnobImage == null) return;

        // GameManager.playerController is only derived once in Awake and goes stale once
        // PlayerGameplayManager reassigns GameManager.player at runtime, so resolve live instead
        // (matches ElementManager.ResolvePlayer's workaround for the same staleness issue).
        PlayerController? player = GameManager.Instance != null && GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;
        bool swordOut = player != null && player.IsSwordOut;

        if (swordOut)
        {
            ApplyDashMode();
        }
        else
        {
            ApplyAttackMode();
        }
    }

    private void ApplyAttackMode()
    {
        Sprite? attackSprite = attackIconSprite;
        if (attackSprite != null && _modeKnobImage!.sprite != attackSprite)
        {
            _modeKnobImage.sprite = attackSprite;
        }
        _modeKnobImage!.enabled = attackSprite != null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        ApplyImbueBorderThickness(fullThickness: true);

        if (_inDashMode)
        {
            _inDashMode = false;
            RefreshColors();
        }
    }

    private void ApplyDashMode()
    {
        // Keep the same circular pad family under DashIcon.
        Sprite? backing = knobBackingSprite != null ? knobBackingSprite : attackIconSprite;
        if (backing != null && _modeKnobImage!.sprite != backing)
        {
            _modeKnobImage.sprite = backing;
        }
        _modeKnobImage!.enabled = backing != null;

        if (iconImage != null && dashIconSprite != null)
        {
            EnsureDashOverlayOnKnob();
            if (iconImage.sprite != dashIconSprite)
            {
                iconImage.sprite = dashIconSprite;
            }
            iconImage.enabled = true;
        }

        ApplyImbueBorderThickness(fullThickness: false);

        if (!_inDashMode)
        {
            _inDashMode = true;
            RefreshColors();
        }
    }

    private void ApplyImbueBorderThickness(bool fullThickness)
    {
        if (imbueBorder == null) return;

        float thickness = _normalOuterRadius - _normalInnerRadius;
        float targetThickness = fullThickness ? thickness : thickness * DashBorderThicknessScale;
        // Keep the inner edge flush with the joystick; thickness shrinks by pulling the outer radius in.
        imbueBorder.InnerRadius = _normalInnerRadius;
        imbueBorder.OuterRadius = _normalInnerRadius + targetThickness;
    }

    private void EnsureDashOverlayOnKnob()
    {
        if (iconImage == null || _modeKnobImage == null || _dashOverlayRect == null) return;

        if (_dashOverlayRect.parent != _modeKnobImage.transform)
        {
            _dashOverlayRect.SetParent(_modeKnobImage.transform, worldPositionStays: false);
        }

        _dashOverlayRect.SetAsLastSibling();
        _dashOverlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        _dashOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        _dashOverlayRect.pivot = new Vector2(0.5f, 0.5f);
        _dashOverlayRect.anchoredPosition = Vector2.zero;
        _dashOverlayRect.localRotation = Quaternion.identity;
        _dashOverlayRect.localScale = Vector3.one;

        Vector2 knobSize = _modeKnobImage.rectTransform.sizeDelta;
        _dashOverlayRect.sizeDelta = knobSize * dashIconScale;
    }

    private void HandleActiveElementChanged(Element element)
    {
        _activeElement = element;
        RefreshColors();
    }

    private void RefreshColors()
    {
        bool physical = _activeElement == Element.Physical;

        if (imbueBorder != null)
        {
            if (physical)
            {
                imbueBorder.color = inactiveBorderColor;
                imbueBorder.Progress = 0f;
            }
            else
            {
                Color borderColor = ElementVisualUtility.GetAccentColor(_activeElement);
                if (_inDashMode)
                {
                    borderColor.a *= DashBorderOpacityScale;
                }
                imbueBorder.color = borderColor;
                imbueBorder.Progress = 1f;
            }
        }

        // knobImage is the Range Indicator ring (element accent / gray when Physical or dash).
        if (knobImage != null)
        {
            Color tint = physical || _inDashMode
                ? inactiveKnobColor
                : ElementVisualUtility.GetAccentColor(_activeElement);
            tint.a = _knobAlpha;
            knobImage.color = tint;
        }
    }
}
