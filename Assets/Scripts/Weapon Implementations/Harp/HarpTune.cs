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

    /// <summary> Every tune the harp knows right now, for a tune that plays others (the jackpot). </summary>
    public readonly IReadOnlyList<HarpTune?> Owned;

    // The weapon, so a tune that unfolds over time can run a coroutine on something that lives in the
    // scene. A ScriptableObject can't host one itself.
    private readonly MonoBehaviour host;

    public HarpContext(Transform player, HashSet<UpgradeType> upgrades, MonoBehaviour host, IReadOnlyList<HarpTune?>? owned = null)
    {
        Player = player;
        Upgrades = upgrades;
        this.host = host;
        Owned = owned ?? System.Array.Empty<HarpTune?>();
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
    [Tooltip("Relative odds against the other tunes of the same family. Families themselves are always " +
             "even, however many tunes each holds (see PickByFamily).")]
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

    /// <summary>
    /// The harp's roll: pick a family evenly among the families present, then a tune within it by weight.
    /// </summary>
    /// <remarks>
    /// Families first so the odds stay even thirds however lopsided the arsenal is. A run starts with one
    /// tune per family and learns the rest at random, so one family can easily hold four tunes while
    /// another holds one; a flat weighted pick would quietly turn Light into whatever it learned most of.
    /// Jackpot tunes are never part of this roll: the weapon rolls for them separately.
    /// Both rolls are uniform values in [0, 1), passed in so the pick is deterministic to test.
    /// </remarks>
    /// <returns>The chosen tune, or null when no family has a tune with positive weight.</returns>
    public static HarpTune? PickByFamily(IReadOnlyList<HarpTune?> tunes, float familyRoll, float tuneRoll)
    {
        var families = new List<HarpTuneFamily>(3);
        foreach (HarpTune? tune in tunes)
        {
            if (tune != null && tune.weight > 0f && tune.family != HarpTuneFamily.Jackpot && !families.Contains(tune.family))
            {
                families.Add(tune.family);
            }
        }

        if (families.Count == 0)
        {
            return null;
        }

        // Enum order, so the same roll means the same family whatever order the tunes were listed in.
        families.Sort();
        HarpTuneFamily family = families[Mathf.Min((int)(Mathf.Clamp01(familyRoll) * families.Count), families.Count - 1)];

        var inFamily = new List<HarpTune?>(tunes.Count);
        foreach (HarpTune? tune in tunes)
        {
            if (tune != null && tune.family == family)
            {
                inFamily.Add(tune);
            }
        }

        return Pick(inFamily, tuneRoll);
    }

    /// <summary>
    /// A whole tap's roll: the jackpot first, at <paramref name="jackpotChance"/>, otherwise
    /// <see cref="PickByFamily"/> over the owned tunes. All rolls are uniform values in [0, 1).
    /// </summary>
    /// <remarks>
    /// The jackpot sits outside the family roll so its odds are a flat chance per tap, untouched by how
    /// many families or tunes are owned, and so taking it never thins any family.
    /// </remarks>
    public static HarpTune? Roll(
        IReadOnlyList<HarpTune?> owned, HarpTune? jackpot, float jackpotChance,
        float jackpotRoll, float familyRoll, float tuneRoll)
    {
        if (jackpot != null && jackpotRoll < jackpotChance)
        {
            return jackpot;
        }

        return PickByFamily(owned, familyRoll, tuneRoll);
    }
}
