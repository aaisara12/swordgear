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
/// <b>PlayerController pulls <see cref="IsAimLocked"/> every frame rather than being pushed a
/// lock/unlock event.</b> Whatever ends the charge — release, cancel, node change, death, element switch,
/// a future dash — makes this report false on the next frame, and movement returns without that path
/// having to know the root exists. A pushed flag would need every one of those paths to be correct.
/// </para>
/// </remarks>
public interface IAimLockProvider
{
    /// <summary>True while this weapon's charge is rooting the player.</summary>
    bool IsAimLocked { get; }

    /// <summary>The direction the weapon will fire on release.</summary>
    Vector2 AimDirection { get; }

    /// <summary>Feeds the movement stick in as aim while locked. Near-zero input is ignored so releasing
    /// the stick holds the last aim rather than snapping it back to a default.</summary>
    void SetAimDirection(Vector2 direction);
}
