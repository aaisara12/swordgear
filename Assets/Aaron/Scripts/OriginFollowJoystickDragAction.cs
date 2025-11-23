#nullable enable

using UnityEngine;

[CreateAssetMenu(fileName = "OriginFollowJoystickDragAction", menuName = "Scriptable Objects/Joystick Drag Action/OriginFollowJoystickDragAction")]
public class OriginFollowJoystickDragAction : JoystickDragAction
{
    public override void OnJoystickDragged(Vector2 pointerPosition, JoystickVisual joystickVisual)
    {
        joystickVisual.KnobPosition = pointerPosition;

        if (Vector2.Distance(pointerPosition, joystickVisual.OriginPosition) > joystickVisual.KnobRange)
        {
            var directionFromPointerToOrigin = (joystickVisual.OriginPosition - pointerPosition).normalized;
            joystickVisual.OriginPosition = joystickVisual.KnobPosition + directionFromPointerToOrigin * joystickVisual.KnobRange;
        }
    }
}


