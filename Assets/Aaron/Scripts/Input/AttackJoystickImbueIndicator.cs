#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attack mode: Knob shows shadedDark49 (pad + glyph); range ring tints from the active element.
/// Dash mode: Knob keeps a plain dark pad with DashIcon on top; imbue border and elemental
/// range-ring tint are suppressed (Physical / gray) until the sword is caught.
/// </summary>
public class AttackJoystickImbueIndicator : MonoBehaviour
{
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
    private bool _subscribedToGameManager;
    private Image? _modeKnobImage;
    private RectTransform? _dashOverlayRect;
    private bool _dashSuppressingImbue;
    private float _storedImbueProgress;
    private float _knobAlpha = 1f;

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
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += HandleActiveElementChanged;
        if (ElementManager.Instance != null)
        {
            _activeElement = ElementManager.Instance.ActiveElement;
        }

        TrySubscribeToGameManager();
        UpdateModeVisuals();
        RefreshColors();
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= HandleActiveElementChanged;
        if (_subscribedToGameManager && GameManager.Instance != null)
        {
            GameManager.Instance.OnEmpowermentTimerChanged -= HandleEmpowermentTimerChanged;
        }
        _subscribedToGameManager = false;
    }

    private void LateUpdate()
    {
        if (!_subscribedToGameManager)
        {
            TrySubscribeToGameManager();
        }

        UpdateModeVisuals();
    }

    private void TrySubscribeToGameManager()
    {
        if (_subscribedToGameManager || GameManager.Instance == null) return;

        GameManager.Instance.OnEmpowermentTimerChanged += HandleEmpowermentTimerChanged;
        _subscribedToGameManager = true;
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

        if (_dashSuppressingImbue)
        {
            _dashSuppressingImbue = false;
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

        if (!_dashSuppressingImbue)
        {
            _dashSuppressingImbue = true;
            if (imbueBorder != null)
            {
                _storedImbueProgress = imbueBorder.Progress;
            }
            RefreshColors();
        }
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

        if (element == Element.Physical)
        {
            _storedImbueProgress = 0f;
        }

        RefreshColors();
    }

    private void HandleEmpowermentTimerChanged(float remaining, float duration)
    {
        float progress = duration > 0f ? remaining / duration : 0f;
        _storedImbueProgress = progress;

        if (_dashSuppressingImbue || imbueBorder == null)
        {
            // Keep tracking duration while sword is out; only the visible border is suppressed.
            if (_dashSuppressingImbue && imbueBorder != null)
            {
                imbueBorder.Progress = 0f;
            }
            return;
        }

        imbueBorder.Progress = progress;
    }

    private void RefreshColors()
    {
        bool useInactive = _dashSuppressingImbue || _activeElement == Element.Physical;

        if (imbueBorder != null)
        {
            if (useInactive)
            {
                imbueBorder.color = inactiveBorderColor;
                imbueBorder.Progress = 0f;
            }
            else
            {
                imbueBorder.color = ElementVisualUtility.GetAccentColor(_activeElement);
                imbueBorder.Progress = _storedImbueProgress;
            }
        }

        // knobImage is the Range Indicator ring (element accent / gray when Physical or dash).
        if (knobImage != null)
        {
            Color tint = useInactive
                ? inactiveKnobColor
                : ElementVisualUtility.GetAccentColor(_activeElement);
            tint.a = _knobAlpha;
            knobImage.color = tint;
        }
    }
}
