#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class OnScreenLeftClickButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    [SerializeField] private string m_ControlPath = string.Empty;
    
    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        
        SendValueToControl(1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        
        SendValueToControl(0f);
    }
}
