#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class LineDrawer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform? _base;
    [SerializeField] private RectTransform? _100UnitLine;
    [SerializeField] private Camera? _camera;
    [SerializeField] private Canvas? _canvas;

    [SerializeField] private JoystickVisualProvider? _joystickProvider;

    private JoystickVisual? _joystick;
    
    private Vector2 _lastPointerPosition;
    private PointerEventData.InputButton _lastPointerButton;
    private bool _isAiming;
    
    private void Awake()
    {
        if (_joystickProvider == null) return;

        _joystick = _joystickProvider.Visual;
    }

    private void Update()
    {
        if (_base == null) return;
        if(_100UnitLine == null) return;
        if(_camera == null) return;
        if(_canvas == null) return;
        if(_joystick == null) return;

        Vector2 direction = _lastPointerPosition - new Vector2(_base.position.x, _base.position.y);

        _100UnitLine.position = _base.position;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        _100UnitLine.localScale = new Vector3(direction.magnitude/_canvas.scaleFactor / 100f, 1, 1);
        
        _100UnitLine.rotation = Quaternion.Euler(0, 0, angle);

        _joystick.OriginPosition = _base.position/_canvas.scaleFactor;
        _joystick.KnobPosition = _joystick.OriginPosition + direction.normalized * _joystick.KnobRange;
        
        _joystick.JoystickValue *= (_lastPointerButton == PointerEventData.InputButton.Left) ? 0.5f : 1;
    }
    

    public void OnPointerDown(PointerEventData eventData)
    {
        _isAiming = true;
        _lastPointerPosition = eventData.position;
        _lastPointerButton = eventData.button;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _lastPointerPosition = eventData.position;
        _lastPointerButton = eventData.button;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isAiming = false;
    }
}
