#nullable enable

using UnityEngine;


[CreateAssetMenu(fileName = "OriginRootedJoystickDragAction", menuName = "Scriptable Objects/Joystick Drag Action/OriginRootedJoystickDragAction")]
public class OriginRootedJoystickDragAction : JoystickDragAction
{
    public override void OnJoystickDragged(Vector2 pointerPosition, JoystickVisual joystickVisual)
    {
        var lineFromOriginToPointer = pointerPosition - joystickVisual.OriginPosition;
        
        float finalKnobDistanceFromOrigin = lineFromOriginToPointer.magnitude;

        if (finalKnobDistanceFromOrigin > joystickVisual.KnobRange)
        {
            finalKnobDistanceFromOrigin = joystickVisual.KnobRange;
        }

        joystickVisual.KnobPosition = lineFromOriginToPointer.normalized * finalKnobDistanceFromOrigin +
                                      joystickVisual.OriginPosition;
    }
}


