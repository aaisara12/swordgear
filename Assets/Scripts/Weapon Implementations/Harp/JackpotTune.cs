#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Grand Chord: the harp's jackpot. Plays every tune it knows, all at once.
/// </summary>
/// <remarks>
/// It grows with the arsenal: on a fresh run it's three tunes, once everything is learned it's ten. That
/// makes "Learn a Tune" pay out twice: once in the shop, and again every time the chord lands.
/// <para>
/// Each tune plays its effect but not its melody: ten melodies at once is noise, and the chord's own
/// strum is the sound of the jackpot. The weapon rolls for this outside the family roll.
/// </para>
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Jackpot Tune", fileName = "JackpotTune")]
public class JackpotTune : HarpTune
{
    [SerializeField] private string announcement = "JACKPOT!";
    [SerializeField] private Color announcementColor = new(1f, 0.85f, 0.3f, 1f);

    public override void Play(HarpContext context)
    {
        // Copied first: the owned list is rebuilt on every read, and a played tune may read it again.
        var tunes = new List<HarpTune?>(context.Owned);
        foreach (HarpTune? tune in tunes)
        {
            if (tune == null || tune == this || tune.Family == HarpTuneFamily.Jackpot)
            {
                continue;
            }

            tune.Play(context);
        }

        StreakWorldIndicator.Instance?.Announce(announcement, announcementColor);
    }
}
