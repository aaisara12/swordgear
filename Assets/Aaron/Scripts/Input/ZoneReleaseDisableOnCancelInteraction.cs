#nullable enable

using UnityEngine.InputSystem;

public class ZoneReleaseDisableOnCancelInteraction : IInputInteraction
{
    public float activeZoneMin = 0f;
    public float activeZoneMax = 0.2f;
    
    private bool isDisabled = false;
    
    public void Process(ref InputInteractionContext context)
    {
        float magnitude = context.ComputeMagnitude();
        bool isInActiveZone = magnitude >= activeZoneMin && magnitude <= activeZoneMax;
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
                else if (isInActiveZone)
                {
                    // aisara => Notice how control might technically not be actuated but could still be in active zone if activeZoneMin is 0
                    context.Started();
                }

                break;
            }
            case InputActionPhase.Started:
            {
                if (isInActiveZone == false)
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
