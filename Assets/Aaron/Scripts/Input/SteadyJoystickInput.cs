#nullable enable

/// <summary>
/// Shared thresholds for the paired tap/hold interactions on the attack button.
/// </summary>
/// <remarks>
/// <c>Attack</c> and <c>ChargeAttack</c> are bound to the <b>same button</b> and told apart purely by
/// time: a press shorter than <see cref="TapHoldSplitSeconds"/> is a tap, anything longer is a hold. Both
/// interactions must therefore read the identical value. If the tap invalidated later than the hold
/// validated, one press would fire both actions; earlier, and there is a dead band where a press is no
/// longer a tap but not yet a charge — which the player feels as unresponsiveness.
/// <para>
/// They previously kept separate copies and drifted. Both classes defaulted to 0.3s, then the tap alone
/// was tuned to 0.2s through an <c>.inputactions</c> parameter the hold could not see, leaving exactly
/// that 0.1s dead band. <b>Do not reintroduce a per-action override for these</b> — an asset parameter
/// silently wins over the field default and puts the drift straight back.
/// </para>
/// </remarks>
public static class SteadyJoystickInput
{
    /// <summary>A press shorter than this is a tap; longer is a hold. One number, both interactions.</summary>
    public const float TapHoldSplitSeconds = 0.2f;

    /// <summary>How far the joystick may stray from centre and still count as held steady.</summary>
    public const float JoystickSafeZone = 0.2f;
}
