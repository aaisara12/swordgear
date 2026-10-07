using UnityEngine;

/// <summary> The payload of an ultimate ability, invoked once the meter is spent. </summary>
public interface IUltimate
{
    /// <summary>
    /// Runs the ultimate. <paramref name="overchargeLevel"/> is 0 for the base version and rises by one for every
    /// extra full set of matching augments the player has stacked, up to the ability's
    /// <see cref="UltimateAbilitySO.MaxOverchargeLevel"/> — see
    /// <see cref="UltimateAbilitySO.GetOverchargeLevel"/>. Each ultimate decides for itself what overcharge
    /// does: more passes, bigger damage, extra elements, whatever suits it. A locked ultimate never gets here.
    /// </summary>
    void ExecuteUlt(int overchargeLevel, Transform player);
}
