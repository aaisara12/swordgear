#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents an interaction where a button hold is only registered if the joystick is held steady, within some threshold.
/// </summary>
public class ButtonHoldWithSteadyJoystickInteraction : IInputInteraction<Vector3>
{
    private enum WaitingSubStateType
    {
        READY_FOR_HOLD,
        VALIDATING_HOLD,
        DISABLED,
    }
    
    // aisara => Represents how much leeway away we give the player when trying to hold down on the joystick at the center
    // Note that this pretty much only applies to mobile controls since the charge button is on top of the joystick
    public float JoystickSafeZone = 0.2f;
    public float SecondsBeforeHoldValidated = 0.3f;
    
    private WaitingSubStateType waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
    
    public void Process(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        // NOTE: aisara => The reason we read in a composite input that puts joystick on x/y and charge button on z
        // is because this interaction depends on the state of both the joystick and the charge button at the same time.
        var joystickInput = new Vector2(input.x, input.y);
        bool isButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= JoystickSafeZone;
        
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
    
    private void ProcessWaitingState(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        var joystickInput = new Vector2(input.x, input.y);
        bool isButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= JoystickSafeZone;
        
        switch (waitingSubState)
        {
            case WaitingSubStateType.READY_FOR_HOLD:
            {
                if (isButtonPressed == false)
                {
                    break;
                }
                
                if (isInSafeZone == false)
                {
                    // aisara => We disable here because it feels weird if you're allowed to start charging by
                    // moving the joystick into the safe zone with the charge button pressed 
                    // as opposed to being in the safe zone FIRST and then pressing the charge button (the intended way to start charging)
                    waitingSubState = WaitingSubStateType.DISABLED;
                    break;
                }
                    
                context.SetTimeout(SecondsBeforeHoldValidated);
                waitingSubState = WaitingSubStateType.VALIDATING_HOLD;
                break;
            }
            case WaitingSubStateType.VALIDATING_HOLD:
            {
                if (isButtonPressed == false || isInSafeZone == false)
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
                if (isButtonPressed == false)
                {
                    waitingSubState = WaitingSubStateType.READY_FOR_HOLD;
                }

                break;
            }
        }
    }
}
