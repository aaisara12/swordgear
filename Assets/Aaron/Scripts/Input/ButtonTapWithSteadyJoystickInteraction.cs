#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents an interaction where a button tap is only registered if the joystick is held steady, within some threshold.
/// </summary>
public class ButtonTapWithSteadyJoystickInteraction : IInputInteraction<Vector3>
{
    public float JoystickSafeZone = 0.2f;
    public float SecondsBeforeTapInvalidated = 0.3f;

    private bool isDisabled = false;
    
    public void Process(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        var joystickInput = new Vector2(input.x, input.y);
        bool isButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= JoystickSafeZone;

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
            {
                if (isDisabled)
                {
                    if (isButtonPressed == false)
                    {
                        isDisabled = false;
                    }

                    break;
                }

                if (isInSafeZone == false)
                {
                    break;
                }

                if (isButtonPressed)
                {
                    context.Started();
                    context.SetTimeout(SecondsBeforeTapInvalidated);
                }

                break;
            }
            case InputActionPhase.Started:
            {
                if (context.timerHasExpired || isInSafeZone == false)
                {
                    isDisabled = true;
                    context.Canceled();
                    break;
                }

                if (isButtonPressed == false)
                {
                    context.Performed();
                }

                break;
            }
        }
    }
        
    public void Reset()
    {
    }
}
