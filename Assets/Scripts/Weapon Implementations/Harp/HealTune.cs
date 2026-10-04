#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// A tune that heals over time: Lullaby. Weak on purpose: it's one outcome of a gamble, not a sustain
/// button, so it tops you up rather than resetting a fight.
/// </summary>
/// <remarks>
/// Heals in ticks rather than one lump so the HP bar visibly climbs while the lullaby plays: the whole
/// note is a sustained note, and the heal is a sustained heal. Goes through
/// <c>PlayerGameplayManager.Heal</c>, so max HP, death and the health events all behave as for lifesteal.
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Heal Tune", fileName = "HealTune")]
public class HealTune : HarpTune
{
    [Tooltip("Total heal, as a fraction of max HP.")]
    [SerializeField, Range(0f, 1f)] private float maxHpFraction = 0.12f;
    [SerializeField] private float durationSeconds = 3f;
    [SerializeField, Min(1)] private int ticks = 12;
    [Tooltip("Worn by the player while the heal runs. Must carry HarpAura.")]
    [SerializeField] private GameObject? auraPrefab;

    public override void Play(HarpContext context)
    {
        if (auraPrefab == null)
        {
            Debug.LogError($"HealTune '{name}': auraPrefab is null");
            return;
        }

        if (PlayerGameplayManager.Instance == null)
        {
            return;
        }

        HarpAuraUtility.Wear(auraPrefab, context.Player, durationSeconds);
        context.Run(HealOverTime());
    }

    private IEnumerator HealOverTime()
    {
        float interval = durationSeconds / ticks;
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(interval);

            PlayerGameplayManager? manager = PlayerGameplayManager.Instance;
            if (manager == null)
            {
                yield break;
            }

            // Re-read max HP each tick, so an augment picked up mid-lullaby is respected.
            manager.Heal(manager.MaxHp * maxHpFraction / ticks);
        }
    }
}
