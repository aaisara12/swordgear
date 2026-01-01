#nullable enable


using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public class PressableJoystickComposite : InputBindingComposite<PressableJoystickValue>
{
    [InputControl(layout = "Button")]
    public int pressPart;
    
    [InputControl(layout = "Axis")]
    public int verticalAxis;
    
    [InputControl(layout = "Axis")]
    public int horizontalAxis;
    
    public override PressableJoystickValue ReadValue(ref InputBindingCompositeContext context)
    {
        var joystickVertical = context.ReadValue<float>(verticalAxis);
        var joystickHorizontal = context.ReadValue<float>(horizontalAxis);
        var press = context.ReadValueAsButton(pressPart);
        return new PressableJoystickValue
        {
            joystick = new Vector2(joystickHorizontal, joystickVertical),
            press = press
        };
    }
}

public struct PressableJoystickValue
{
    public Vector2 joystick;
    public bool press;
}
