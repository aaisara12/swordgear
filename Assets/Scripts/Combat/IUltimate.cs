using UnityEngine;

/// <summary> The payload of an ultimate ability, invoked once the meter is spent. </summary>
public interface IUltimate
{
    /// <summary>
    /// Runs the ultimate. <paramref name="level"/> is 1 for the base version and rises by one for every extra
    /// full set of matching augments the player has stacked — see <see cref="UltimateAbilitySO.GetLevel"/>.
    /// It is never 0: a level-0 ultimate is locked and can't be activated at all.
    /// </summary>
    void ExecuteUlt(int level, Transform player);
}
