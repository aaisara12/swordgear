#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class PlayerCentricJoystickControlRegion : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform? _base;
    
    [Header("Input Screen Click Position References")]
    [SerializeField] private Camera? _camera;
    [SerializeField] private Canvas? _canvas;

    [Header("Visualization")]
    [SerializeField] private JoystickVisualProvider? _joystickProvider;
    [SerializeField] private RectTransform? _100UnitLine;

    private JoystickVisual? _joystick;
    
    [Header("Output Control")]
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_ControlPath = string.Empty;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }
    
    private void Awake()
    {
        if (_joystickProvider == null) return;

        _joystick = _joystickProvider.Visual;
    }

    private void PointJoystickTowardsScreenPoint(JoystickVisual joystick, Vector2 screenPoint)
    {
        if (_base == null) return;
        if(_100UnitLine == null) return;
        if(_camera == null) return;
        if(_canvas == null) return;

        Vector2 direction = screenPoint - new Vector2(_base.position.x, _base.position.y);

        _100UnitLine.position = _base.position;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        _100UnitLine.localScale = new Vector3(direction.magnitude/_canvas.scaleFactor / 100f, 1, 1);
        
        _100UnitLine.rotation = Quaternion.Euler(0, 0, angle);

        joystick.OriginPosition = _base.position/_canvas.scaleFactor;
        joystick.KnobPosition = joystick.OriginPosition + direction.normalized * joystick.KnobRange;
    }
    

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_joystick == null)
        {
            return;
        }
        
        PointJoystickTowardsScreenPoint(_joystick, eventData.position);

        _joystick.JoystickValue = _joystick.JoystickValue.normalized;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _joystick.JoystickValue *= 0.1f;
        }
        
        SendValueToControl(_joystick.JoystickValue);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_joystick == null)
        {
            return;
        }
        
        PointJoystickTowardsScreenPoint(_joystick, eventData.position);

        _joystick.JoystickValue = _joystick.JoystickValue.normalized;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _joystick.JoystickValue *= 0.1f;
        }
        
        SendValueToControl(_joystick.JoystickValue);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_joystick == null)
        {
            return;
        }
        
        _joystick.ResetPositions();
        SendValueToControl(_joystick.JoystickValue);
    }
}
