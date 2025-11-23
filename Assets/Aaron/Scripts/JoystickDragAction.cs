#nullable enable

using UnityEngine;

public abstract class JoystickDragAction : ScriptableObject
{
    public abstract void OnJoystickDragged(Vector2 pointerPosition, JoystickVisual joystickVisual);
}


