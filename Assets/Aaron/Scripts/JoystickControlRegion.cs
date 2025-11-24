#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;


[RequireComponent(typeof(JoystickVisualProvider))]
public class JoystickControlRegion : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_ControlPath = string.Empty;

    [SerializeField] private Canvas? canvasRegisteringInput;
    [SerializeField] private RectTransform? joystickHome;
    
    [SerializeField] private JoystickVisualProvider? joystickVisualProvider;
    [SerializeField] private JoystickDragAction? joystickDragAction;
    
    private JoystickVisual? joystickVisual;
    
    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public void OnDrag(PointerEventData eventData)
    {
        joystickVisual.ThrowIfNull(nameof(joystickVisual));
        canvasRegisteringInput.ThrowIfNull(nameof(canvasRegisteringInput));

        if (joystickDragAction != null)
        {
            joystickDragAction.OnJoystickDragged(eventData.position / canvasRegisteringInput.scaleFactor, joystickVisual);
        }

        float verticalComponent = joystickVisual.KnobPosition.y - joystickVisual.OriginPosition.y;
        float horizontalComponent = joystickVisual.KnobPosition.x - joystickVisual.OriginPosition.x;
        
        SendValueToControl(new Vector2(horizontalComponent, verticalComponent) / canvasRegisteringInput.scaleFactor / joystickVisual.KnobRange);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        joystickVisual.ThrowIfNull(nameof(joystickVisual));
        canvasRegisteringInput.ThrowIfNull(nameof(canvasRegisteringInput));
        
        joystickVisual.ResetPositions();
        joystickVisual.Move(eventData.position / canvasRegisteringInput.scaleFactor);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        joystickVisual.ThrowIfNull(nameof(joystickVisual));
        canvasRegisteringInput.ThrowIfNull(nameof(canvasRegisteringInput));
        joystickHome.ThrowIfNull(nameof(joystickHome));
        
        joystickVisual.ResetPositions();
        joystickVisual.Move(new Vector2(joystickHome.position.x, joystickHome.position.y) / canvasRegisteringInput.scaleFactor);
        
        SendValueToControl(new Vector2(0, 0));
    }

    private void Awake()
    {
        if (joystickVisualProvider == null)
        {
            joystickVisualProvider = GetComponent<JoystickVisualProvider>();
        }
        
        // SHOULD have a component given we have the RequireComponent attribute
        joystickVisualProvider.ThrowIfNull(nameof(joystickVisualProvider));
        canvasRegisteringInput.ThrowIfNull(nameof(canvasRegisteringInput));
        joystickHome.ThrowIfNull(nameof(joystickHome));
            
        joystickVisual = joystickVisualProvider.Visual;
        
        joystickVisual.ResetPositions();
        joystickVisual.Move(new Vector2(joystickHome.position.x, joystickHome.position.y) / canvasRegisteringInput.scaleFactor);
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (joystickVisualProvider == null)
        {
            joystickVisualProvider = GetComponent<JoystickVisualProvider>();
        }
#endif
    }
}

