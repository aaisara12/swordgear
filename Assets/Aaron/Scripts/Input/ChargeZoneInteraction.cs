#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Corresponds to the interaction when the player is holding the joystick in a specified zone while also holding a charge button.
/// If the joystick leaves the zone, the interaction is canceled and disabled until the joystick is released.
/// If the charge button is released while in the zone, the interaction is performed.
/// </summary>
public class ChargeZoneInteraction : IInputInteraction<Vector3>
{
    private enum WaitingSubStateType
    {
        READY_FOR_HOLD,
        VERIFYING_HOLD,
        DISABLED,
    }
    
    // aisara => Represents how much leeway away we give the player when trying to hold down on the joystick at the center
    // Note that this pretty much only applies to mobile controls since the charge button is on top of the joystick
    public float joystickSafeZone = 0.2f;
    public float secondsToHoldBeforeStart = 0.3f;
    
    private WaitingSubStateType waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
    
    public void Process(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        // NOTE: aisara => The reason we read in a composite input that puts joystick on x/y and charge button on z
        // is because this interaction depends on the state of both the joystick and the charge button at the same time.
        var joystickInput = new Vector2(input.x, input.y);
        bool isChargeButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= joystickSafeZone;
        
        switch (context.phase)
        {
            case InputActionPhase.Waiting:
            {
                ProcessWaitingState(ref context);
                break;
            }
            case InputActionPhase.Started:
            {
                if (isInSafeZone == false)
                {
                    context.Canceled();
                    waitingSubState = WaitingSubStateType.DISABLED;
                    break;
                }
                
                if (isChargeButtonPressed == false)
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
    
    private void ProcessWaitingState(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        var joystickInput = new Vector2(input.x, input.y);
        bool isChargeButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= joystickSafeZone;
        
        switch (waitingSubState)
        {
            case WaitingSubStateType.READY_FOR_HOLD:
            {
                if (isChargeButtonPressed == false || isInSafeZone == false)
                {
                    break;
                }
                    
                context.SetTimeout(secondsToHoldBeforeStart);
                waitingSubState = WaitingSubStateType.VERIFYING_HOLD;
                break;
            }
            case WaitingSubStateType.VERIFYING_HOLD:
            {
                if (isChargeButtonPressed == false || isInSafeZone == false)
                {
                    waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
                    break;
                }
                    
                if (context.timerHasExpired)
                {
                    // If we're in this clause, it means the hold timer has expired and the player has successfully held long enough

                    waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
                    context.Started();
                }

                break;
            }
            case WaitingSubStateType.DISABLED:
            {
                if (isChargeButtonPressed == false)
                {
                    waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
                }

                break;
            }
        }
    }
}
