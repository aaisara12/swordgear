#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// Represents a joystick centered on the player character. The position of the click on screen relative to the player
/// character determines the direction of the joystick input. This is similar to what is done in Hades.
/// </summary>
public class PlayerCentricJoystickControlRegion : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_ControlPath = string.Empty;
    
    [SerializeField] private Transform? playerTransform;
    
    // aisara => Optional parameter for visualizing the joystick input
    [SerializeField] private JoystickVisualProvider? joystickVisualProvider;
    
    private JoystickVisual? joystickVisual;
    
    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    private void Awake()
    {
        if (joystickVisualProvider != null)
        {
            joystickVisual = joystickVisualProvider.Visual;
        }
    }

    private void Update()
    {
        var playerScreenPoint = GetPlayerScreenPoint();

        if (playerScreenPoint.HasValue && joystickVisual != null)
        {
            joystickVisual.Move(playerScreenPoint.Value);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var playerScreenPoint = GetPlayerScreenPoint();

        if (playerScreenPoint.HasValue == false)
        {
            return;
        }
        
        var joystickVector = (eventData.position - new Vector2(playerScreenPoint.Value.x, playerScreenPoint.Value.y)).normalized * 0.1f;
        
        SendValueToControl(joystickVector);
        
        if (joystickVisual != null)
        {
            joystickVisual.KnobPosition = new Vector2(joystickVector.x, joystickVector.y) * joystickVisual.KnobRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SendValueToControl(Vector2.zero);
        
        if (joystickVisual != null)
        {
            joystickVisual.KnobPosition = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        var playerScreenPoint = GetPlayerScreenPoint();

        if (playerScreenPoint.HasValue == false)
        {
            return;
        }
        
        var joystickVector = (eventData.position - new Vector2(playerScreenPoint.Value.x, playerScreenPoint.Value.y)).normalized * 0.1f;
        
        SendValueToControl(joystickVector);
        
        if (joystickVisual != null)
        {
            joystickVisual.KnobPosition = new Vector2(joystickVector.x, joystickVector.y) * joystickVisual.KnobRange;
        }
    }
    
    private Vector2? GetPlayerScreenPoint()
    {
        // TODO: Refactor this once we're no longer using GameManager singleton. Until then, we're forced to check every frame.
        // var player = GameManager.Instance.player;
        //
        // if(player == null)
        //     return null;
        //
        // var playerTransform = player.transform;
        
        if (playerTransform == null)
            return null;

        // TODO: Refactor this once we've done the GameManager refactor - ideally the camera would be provided via dependency injection or similar
        var cam = Camera.main;
        if (cam == null)
            return null;

        Vector3 screenPoint3 = cam.WorldToScreenPoint(playerTransform.position);
        // if behind the camera, z will be negative — treat as not visible
        if (screenPoint3.z < 0f)
            return null;

        return new Vector2(screenPoint3.x, screenPoint3.y);
    }
}
