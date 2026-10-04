#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The three ways a tune can pay out, plus the rare jackpot. Odds are balanced <em>across families</em>,
/// not across tunes, so adding a fourth offensive tune doesn't quietly make Light an offence element.
/// </summary>
public enum HarpTuneFamily
{
    Offense,
    Sustain,
    Streak,
    Jackpot,
}

/// <summary>Everything a tune needs from the weapon that played it.</summary>
public readonly struct HarpContext
{
    public readonly Transform Player;
    public readonly HashSet<UpgradeType> Upgrades;

    // The weapon, so a tune that unfolds over time can run a coroutine on something that lives in the
    // scene. A ScriptableObject can't host one itself.
    private readonly MonoBehaviour host;

    public HarpContext(Transform player, HashSet<UpgradeType> upgrades, MonoBehaviour host)
    {
        Player = player;
        Upgrades = upgrades;
        this.host = host;
    }

    public Coroutine Run(IEnumerator routine) => host.StartCoroutine(routine);
}

/// <summary>One plucked note of a tune's melody.</summary>
[System.Serializable]
public struct HarpNote
{
    [Tooltip("Semitones above the pluck sample's C5. 12 is the octave above; negative goes below.")]
    public int semitones;
    [Tooltip("Seconds after the previous note. The first note's delay is from the tap.")]
    public float delay;
    [Range(0f, 1f)] public float volume;
}

/// <summary>
/// One tune Light's harp can play: what it does, how likely it is, the note that shows it and the
/// melody that sounds it.
/// </summary>
/// <remarks>
/// Tunes are data assets rather than code paths in the weapon, one subclass per <em>kind</em> of effect
/// (projectiles, pulses, heals, …) and one asset per tune. Two tunes that differ only in numbers — Flurry
/// and Strike are both "fire notes at things" — are two assets of the same class, so retuning the odds or
/// the payout of any tune is an Inspector edit.
/// <para>
/// The glyph is real notation and the note value carries meaning: quick notes are many quick hits, long
/// notes are sustained effects. Players learn to read the pull from the note before the effect lands.
/// </para>
/// </remarks>
public abstract class HarpTune : ScriptableObject
{
    [SerializeField] private HarpTuneFamily family;
    [Tooltip("Relative odds within the whole roll. Keep each family's weights summing to the same total " +
             "so the families stay even.")]
    [SerializeField, Min(0f)] private float weight = 1f;
    [Tooltip("The note shown above the player as this tune plays.")]
    [SerializeField] private Sprite? glyph;
    [Tooltip("What the tune sounds like. Its shape should echo the note: a run for sixteenths, one long " +
             "tone for a whole note.")]
    [SerializeField] private HarpNote[] melody = System.Array.Empty<HarpNote>();

    public HarpTuneFamily Family => family;
    public float Weight => weight;
    public Sprite? Glyph => glyph;

    /// <summary>Performs the tune's effect.</summary>
    public abstract void Play(HarpContext context);

    /// <summary>Plucks the melody, re-pitching the one harp sample per note.</summary>
    /// <remarks>
    /// Pitch through <c>AudioSource.pitch</c> also changes a note's length (an octave up rings half as
    /// long), which is how a real harp behaves anyway: high strings are short and die fast.
    /// </remarks>
    public IEnumerator PlayMelody()
    {
        foreach (HarpNote note in melody)
        {
            if (note.delay > 0f)
            {
                yield return new WaitForSeconds(note.delay);
            }

            AudioSystem.Play(AudioSystem.Sound.Harp_Pluck, note.volume, Mathf.Pow(2f, note.semitones / 12f));
        }
    }

    /// <summary>
    /// Weighted pick. <paramref name="roll"/> is a uniform value in [0, 1), passed in rather than drawn
    /// here so the pick is deterministic to test.
    /// </summary>
    /// <returns>The chosen tune, or null when nothing has positive weight.</returns>
    public static HarpTune? Pick(IReadOnlyList<HarpTune?> tunes, float roll)
    {
        float total = 0f;
        foreach (HarpTune? tune in tunes)
        {
            if (tune != null && tune.weight > 0f)
            {
                total += tune.weight;
            }
        }

        if (total <= 0f)
        {
            return null;
        }

        float target = Mathf.Clamp01(roll) * total;
        HarpTune? last = null;
        foreach (HarpTune? tune in tunes)
        {
            if (tune == null || tune.weight <= 0f)
            {
                continue;
            }

            last = tune;
            target -= tune.weight;
            if (target < 0f)
            {
                return tune;
            }
        }

        // roll == 1 lands exactly on the end; float error can too.
        return last;
    }
}
