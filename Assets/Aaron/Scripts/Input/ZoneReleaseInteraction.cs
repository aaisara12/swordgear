#nullable enable

using UnityEngine.InputSystem;

/// <summary>
/// Triggers a Performed event when a 2D input is cancelled while in a specified active zone
/// and a Cancelled event when a 2D input leaves the active zone
/// </summary>
public class ZoneReleaseInteraction : IInputInteraction
{
    public float activeZoneMin = 0.2f;
    public float activeZoneMax = 1.0f;

    // A digital-normalized composite builds its diagonal from the constant 0.707107, so a two-key
    // diagonal comes out at magnitude 1.0000001 — just over activeZoneMax. Without this tolerance the
    // upper bound rejects it, and the interaction CANCELS instead of performing: on keyboard every
    // diagonal flick silently did nothing while the cardinals (exactly 1.0) worked.
    private const float ActiveZoneMaxTolerance = 0.001f;

    public void Process(ref InputInteractionContext context)
    {
        // aisara => We get an exception if we try to read a Vector2 from a joystick in order to compute magnitude
        // perhaps because there hasn't been any processing to convert the raw input to Vector2 yet.
        // Thus, we use the defined ComputeMagnitude method instead.
        float magnitude = context.ComputeMagnitude();
        bool isInActiveZone = magnitude > activeZoneMin && magnitude <= activeZoneMax + ActiveZoneMaxTolerance;
        switch (context.phase)
        {
            case InputActionPhase.Waiting:
            {
                if (isInActiveZone)
                {
                    context.Started();
                }

                break;
            }
            case InputActionPhase.Started:
            {
                if (context.ControlIsActuated() == false)
                {
                    context.Performed();
                }
                else if (isInActiveZone == false)
                {
                    context.Canceled();
                }

                break;
            }
        }
    }

    public void Reset()
    {
    }
}
