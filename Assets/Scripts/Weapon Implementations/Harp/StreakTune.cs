#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tune that starts or feeds a hot streak: Crescendo (damage), Allegro (attack and move speed).
/// </summary>
/// <remarks>
/// The streak itself lives on <see cref="PlayerStatModifiers"/>, not here and not on the weapon: switching
/// element (or a new arena) ends Light's imbue, and a buff owned by the weapon would end with it. Stacks build up to
/// <see cref="maxStacks"/>, each refreshing a safety timer, and the whole streak busts the moment the
/// player takes damage. That's the gamble: ride your luck, but don't get touched.
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Streak Tune", fileName = "StreakTune")]
public class StreakTune : HarpTune
{
    [Tooltip("Names the streak. Must match the HUD row and the overhead indicator display for it.")]
    [SerializeField] private string streakId = "";
    [SerializeField] private List<StreakBonus> bonuses = new();
    [SerializeField, Min(1)] private int maxStacks = 3;
    [Tooltip("Seconds a streak lasts with no new stack. A backstop: the real way a streak ends is a hit.")]
    [SerializeField] private float capSeconds = 20f;

    public override void Play(HarpContext context)
    {
        if (string.IsNullOrEmpty(streakId))
        {
            Debug.LogError($"StreakTune '{name}': streakId is empty");
            return;
        }

        PlayerStatModifiers.Instance?.AddStreakStack(streakId, bonuses, maxStacks, capSeconds);
    }
}
