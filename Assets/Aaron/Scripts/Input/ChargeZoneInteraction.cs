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
    // aisara => Represents how much leeway away we give the player when trying to hold down on the joystick at the center
    // Note that this pretty much only applies to mobile controls since the charge button is on top of the joystick
    public float joystickSafeZone = 0.2f;
    
    private bool isDisabled = false;
    
    public void Process(ref InputInteractionContext context)
    {
        var input = context.ReadValue<Vector3>();
        
        var joystickInput = new Vector2(input.x, input.y);
        bool isChargeButtonPressed = input.z > 0.5f;
        
        float magnitude = joystickInput.magnitude;
        bool isInSafeZone = magnitude <= joystickSafeZone;
        switch (context.phase)
        {
            case InputActionPhase.Waiting:
            {
                if (isDisabled)
                {
                    if (context.ControlIsActuated() == false)
                    {
                        isDisabled = false;
                    }
                }
                else if (isInSafeZone && isChargeButtonPressed)
                {
                    // aisara => Notice how control might technically not be actuated but could still be in active zone if activeZoneMin is 0
                    context.Started();
                }

                break;
            }
            case InputActionPhase.Started:
            {
                if (isInSafeZone == false)
                {
                    context.Canceled();
                    isDisabled = true;
                }
                else if (context.ControlIsActuated() == false)
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
