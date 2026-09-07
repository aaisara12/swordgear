#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mirrors keyboard (and gamepad) input onto an on-screen joystick's knob, so the simulated mobile
/// controls actually move while playing in the editor.
/// </summary>
/// <remarks>
/// The on-screen sticks are <see cref="JoystickControlRegion"/>s that push into virtual gamepad controls,
/// and the keyboard binds straight to the actions instead — so on desktop the knob never moved and the
/// simulated view showed nothing. This drives the knob from the action's own value, which means it works
/// for a real gamepad too.
/// <para>
/// Visual only: it deliberately does NOT send anything to the control. The keyboard already drives the
/// action through its own bindings, and feeding the virtual stick as well would put two sources on one
/// action for no gain.
/// </para>
/// </remarks>
[RequireComponent(typeof(JoystickVisualProvider))]
public class SimulatedJoystickVisualDriver : MonoBehaviour
{
    private enum StickSource
    {
        /// <summary>Left stick — movement, and the aim while an element roots the player.</summary>
        Move,

        /// <summary>Right stick — the gear flick and the charge direction.</summary>
        Attack,
    }

    [Header("Source")]
    [SerializeField] private StickSource source = StickSource.Move;

    [Header("Scene References")]
    [Tooltip("Optional. When set, the driver stands aside while this region is being dragged by a pointer.")]
    [SerializeField] private JoystickControlRegion? controlRegion;
    [SerializeField] private JoystickVisualProvider? visualProvider;

    private PlayerControls? controls;
    private InputAction? sourceAction;
    private bool wasCentered = true;

    private void Awake()
    {
        if (visualProvider == null)
        {
            visualProvider = GetComponent<JoystickVisualProvider>();
        }

        if (controlRegion == null)
        {
            controlRegion = GetComponent<JoystickControlRegion>();
        }

        // Our own instance, read-only: we never subscribe, so this can't disturb the real input manager.
        controls = new PlayerControls();
        sourceAction = source == StickSource.Move
            ? controls.Gameplay.Move
            : controls.Gameplay.AimedAttack;
    }

    private void OnEnable() => sourceAction?.Enable();

    private void OnDisable()
    {
        sourceAction?.Disable();
        ResetKnob();
    }

    private void OnDestroy()
    {
        controls?.Dispose();
        controls = null;
    }

    private void Update()
    {
        if (sourceAction == null || visualProvider == null)
        {
            return;
        }

        // A real pointer drag owns the knob; don't fight it.
        if (controlRegion != null && controlRegion.IsBeingDragged)
        {
            wasCentered = false;
            return;
        }

        Vector2 value = sourceAction.ReadValue<Vector2>();

        if (value.sqrMagnitude < 0.0001f)
        {
            // Only reset once per release, so a resting knob isn't rewritten every frame.
            if (!wasCentered)
            {
                ResetKnob();
                wasCentered = true;
            }

            return;
        }

        wasCentered = false;
        visualProvider.Visual.JoystickValue = Vector2.ClampMagnitude(value, 1f);
    }

    private void ResetKnob()
    {
        if (visualProvider != null)
        {
            visualProvider.Visual.JoystickValue = Vector2.zero;
        }
    }
}
