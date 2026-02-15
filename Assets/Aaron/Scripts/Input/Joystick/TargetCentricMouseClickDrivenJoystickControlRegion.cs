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
    [SerializeField] private Camera? _camera;
    [SerializeField] private Canvas? _canvas;

    [Header("Visualization")]
    [SerializeField] private JoystickVisualProvider? _joystickProvider;
    [SerializeField] private RectTransform? _100UnitLine;
    [SerializeField] private bool _enableVisualization;

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
            _100UnitLine.gameObject.SetActive(true);
            _joystick.Show();
        }
        else
        {
            _100UnitLine.gameObject.SetActive(false);
            _joystick.Hide();
        }
    }
    #endif

    private void PointJoystickTowardsScreenPoint(JoystickVisual joystick, Vector2 screenPoint)
    {
        if (_target == null) return;
        if(_100UnitLine == null) return;
        if(_camera == null) return;
        if(_canvas == null) return;

        Vector2 direction = screenPoint - new Vector2(_target.position.x, _target.position.y);

        _100UnitLine.position = _target.position;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        _100UnitLine.localScale = new Vector3(direction.magnitude/_canvas.scaleFactor / 100f, 1, 1);
        
        _100UnitLine.rotation = Quaternion.Euler(0, 0, angle);

        joystick.OriginPosition = _target.position/_canvas.scaleFactor;
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

        if (_100UnitLine != null)
        {
            _100UnitLine.localScale = Vector3.zero;
        }
        
        _joystick.ResetPositions();
        SendValueToControl(_joystick.JoystickValue);
    }
}
