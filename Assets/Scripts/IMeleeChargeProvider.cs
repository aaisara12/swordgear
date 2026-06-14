#nullable enable

using System.Collections.Generic;

/// <summary>
/// Optional capability for elemental weapons that support a hold-to-charge melee attack.
/// Implement on weapons that need charge VFX/UI; ElementManager forwards display state to indicators.
/// </summary>
public interface IMeleeChargeProvider
{
    /// <summary>True while a hold-to-charge melee is in progress.</summary>
    bool IsCharging { get; }

    /// <summary>0 while idle; 0..1 while charging.</summary>
    float ChargeProgress { get; }

    /// <summary>True when charge has reached full power and is held at max.</summary>
    bool IsMaxCharge { get; }

    /// <summary>Whether charge indicators should be eligible (upgrade owned, player state valid, etc.).</summary>
    bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player);
}

/// <summary>Snapshot for world/UI charge indicators. Query via <see cref="ElementManager.TryGetMeleeChargeDisplayState"/>.</summary>
public readonly struct MeleeChargeDisplayState
{
    public float Progress { get; }
    public Element Element { get; }
    public bool IsMaxCharge { get; }

    public MeleeChargeDisplayState(float progress, Element element, bool isMaxCharge)
    {
        Progress = progress;
        Element = element;
        IsMaxCharge = isMaxCharge;
    }
}
