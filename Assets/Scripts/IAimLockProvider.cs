#nullable enable

using UnityEngine;

/// <summary>
/// Optional capability for elemental weapons whose charge roots the player and repurposes the movement
/// stick as aim. <see cref="ElementManager"/> forwards the lock state to <see cref="PlayerController"/>.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="IMeleeChargeProvider"/>. Fire and Lightning charge but must NOT
/// root, so folding the lock into that interface would impose it on every weapon that merely wants a
/// charge indicator.
/// <para>
/// The weapon does not own an aim direction. While locked, the movement stick steers
/// <c>PlayerWeaponIndicator</c> — which every element already treats as the source of truth for facing,
/// auto-aim included — and the weapon reads it back through <c>player.up</c> at release, exactly like any
/// other attack.
/// <para>
/// <b>PlayerController pulls <see cref="IsAimLocked"/> every frame rather than being pushed a
/// lock/unlock event.</b> Whatever ends the charge — release, cancel, node change, death, element switch,
/// a future dash — makes this report false on the next frame, and movement returns without that path
/// having to know the root exists. A pushed flag would need every one of those paths to be correct.
/// </para>
/// </para>
/// </remarks>
public interface IAimLockProvider
{
    /// <summary>True while this weapon's charge is rooting the player.</summary>
    bool IsAimLocked { get; }

    /// <summary>
    /// True for a weapon with <b>no tap attack</b>, where every press is a charge.
    /// </summary>
    /// <remarks>
    /// Attack and ChargeAttack share a button and are told apart by holding past
    /// <see cref="SteadyJoystickInput.TapHoldSplitSeconds"/>. A weapon with no tap has nothing to be told
    /// apart from, so that wait is pure input latency — the charge begins on the press instead. The
    /// release still arrives through whichever action claimed the press, and both end at
    /// <c>OnTap</c>, so the weapon sees one code path either way.
    /// </remarks>
    bool ChargesOnPress => false;
}
