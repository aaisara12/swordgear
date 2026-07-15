#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the attack joystick's icon (sword vs. dash), range-knob tint, imbue tint, and
/// radial imbue-duration border. Lives alongside ChargeAttackJoystickIndicator on the
/// "Joystick" node.
/// </summary>
public class AttackJoystickImbueIndicator : MonoBehaviour
{
    [SerializeField] private Image? iconImage;
    [SerializeField] private Sprite? attackIconSprite;
    [SerializeField] private Sprite? dashIconSprite;
    [SerializeField] private Image? knobImage;
    [SerializeField] private RadialBorderTimer? imbueBorder;
    [SerializeField] private Color inactiveBorderColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color inactiveKnobColor = new(0.55f, 0.55f, 0.55f, 1f);

    private Element _activeElement = Element.Physical;
    private bool _subscribedToGameManager;
    private float _knobAlpha = 1f;

    private void Awake()
    {
        if (knobImage != null)
        {
            _knobAlpha = knobImage.color.a;
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
        UpdateIcon();
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

        UpdateIcon();
    }

    private void TrySubscribeToGameManager()
    {
        if (_subscribedToGameManager || GameManager.Instance == null) return;

        GameManager.Instance.OnEmpowermentTimerChanged += HandleEmpowermentTimerChanged;
        _subscribedToGameManager = true;
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;

        // GameManager.playerController is only derived once in Awake and goes stale once
        // PlayerGameplayManager reassigns GameManager.player at runtime, so resolve live instead
        // (matches ElementManager.ResolvePlayer's workaround for the same staleness issue).
        PlayerController? player = GameManager.Instance != null && GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;
        bool swordOut = player != null && player.IsSwordOut;

        Sprite? desired = swordOut ? dashIconSprite : attackIconSprite;
        if (iconImage.sprite != desired)
        {
            iconImage.sprite = desired;
        }
        iconImage.enabled = desired != null;
    }

    private void HandleActiveElementChanged(Element element)
    {
        _activeElement = element;
        RefreshColors();

        if (element == Element.Physical && imbueBorder != null)
        {
            imbueBorder.Progress = 0f;
        }
    }

    private void HandleEmpowermentTimerChanged(float remaining, float duration)
    {
        if (imbueBorder == null) return;
        imbueBorder.Progress = duration > 0f ? remaining / duration : 0f;
    }

    private void RefreshColors()
    {
        if (imbueBorder != null)
        {
            imbueBorder.color = _activeElement == Element.Physical
                ? inactiveBorderColor
                : ElementVisualUtility.GetAccentColor(_activeElement);
        }

        if (knobImage != null)
        {
            Color tint = _activeElement == Element.Physical
                ? inactiveKnobColor
                : ElementVisualUtility.GetAccentColor(_activeElement);
            tint.a = _knobAlpha;
            knobImage.color = tint;
        }
    }
}
