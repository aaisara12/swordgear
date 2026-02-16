#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// A joystick control region that actuates a joystick centered on a target transform and driven by the position
/// of the user's click relative to that target. The joystick's value is determined by the direction the click came from
/// relative to the target's position. This is a joystick region specific to PC controls.
/// </summary>
public class TargetCentricMouseClickDrivenJoystickControlRegion : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform? _target;
    
    [Header("Input Screen Click Position References")]
    [SerializeField] private Canvas? _canvas;

    [Header("Visualization")]
    [SerializeField] private JoystickVisualProvider? _joystickProvider;
    [SerializeField] private RectTransform? _100UnitLine;
    [SerializeField] private bool _enableVisualization;

    private JoystickVisual? _joystick;
    private Vector2 _joystickValue;
    private Vector2 _targetToLastClickPoint;
    
    [Header("Output Control")]
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_ControlPath = string.Empty;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }
    
    private void Start()
    {
        if (_joystickProvider == null) return;

        _joystick = _joystickProvider.Visual;
    }

    // TODO: aisara => Decouple visualization from functionality so that we don't have to run editor-only code in Update or other visualization code mixed in with functionality
    #if UNITY_EDITOR
    private void Update()
    {
        if (_joystick == null || _100UnitLine == null)
        {
            return;
        }
        
        if (_enableVisualization)
        {
            RenderJoystick(_joystickValue);
            RenderDirectionLine(_targetToLastClickPoint);
        }
        else
        {
            _100UnitLine.gameObject.SetActive(false);
            _joystick.Hide();
        }
    }
    #endif
    
    private void RenderDirectionLine(Vector2 lineValue)
    {
        if (_100UnitLine == null) return;
        if (_canvas == null) return;
        if (_target == null) return;
        
        _100UnitLine.gameObject.SetActive(true);
        
        _100UnitLine.position = _target.position;
        
        float angle = Mathf.Atan2(lineValue.y, lineValue.x) * Mathf.Rad2Deg;
        
        _100UnitLine.localScale = new Vector3(lineValue.magnitude / _canvas.scaleFactor / 100f, 1, 1);
        
        _100UnitLine.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void RenderJoystick(Vector2 joystickValue)
    {
        if (_joystick == null) return;
        if (_target == null) return;
        if (_canvas == null) return;
        
        var normalizedPosition = _target.position / _canvas.scaleFactor;
        
        _joystick.Show();

        _joystick.OriginPosition = normalizedPosition;
        _joystick.JoystickValue = joystickValue;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_joystick == null) return;
        if (_target == null) return;
        
        Vector2 targetScreenPoint = new Vector2(_target.position.x, _target.position.y);
        Vector2 direction = (eventData.position - targetScreenPoint).normalized;

        var joystickValue = direction;
        
        // aisara => Left click forces a smaller joystick magnitude since our joysticks read different inputs based on magnitude
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            joystickValue *= 0.1f;
        }
        
        SendValueToControl(joystickValue);
        _joystickValue = joystickValue;
        _targetToLastClickPoint = eventData.position - targetScreenPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_joystick == null) return;
        if (_target == null) return;
        
        Vector2 targetScreenPoint = new Vector2(_target.position.x, _target.position.y);
        Vector2 direction = (eventData.position - targetScreenPoint).normalized;

        var joystickValue = direction;
        
        // aisara => Left click forces a smaller joystick magnitude since our joysticks read different inputs based on magnitude
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            joystickValue *= 0.1f;
        }
        
        SendValueToControl(joystickValue);
        _joystickValue = joystickValue;
        _targetToLastClickPoint = eventData.position - targetScreenPoint;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SendValueToControl(Vector2.zero);
        _joystickValue = Vector2.zero;
        _targetToLastClickPoint = Vector2.zero;
    }
}
