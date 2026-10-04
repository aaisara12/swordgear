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
/// <para>
/// Every tick says "heal" three ways at once, because drifting opal notes alone didn't read as healing:
/// a green number off the player, a high harp sparkle, and (through the health event) a green flash on
/// the HP bar. A shimmer under the lullaby's opening marks the start.
/// </para>
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

    [Header("Heal feedback")]
    [Tooltip("Pops '+N' off the player on each tick that actually healed. Needs a TMP_Text child.")]
    [SerializeField] private GameObject? healNumberPrefab;
    [SerializeField] private float healNumberSeconds = 0.8f;
    [SerializeField] private Vector2 healNumberOffset = new(0f, 0.8f);
    [Tooltip("Sideways scatter, so a run of ticks reads as a stream rather than one number blinking.")]
    [SerializeField] private float healNumberJitter = 0.5f;
    [SerializeField] private Color healNumberColor = new(0.45f, 1f, 0.5f, 1f);
    [SerializeField, Range(0f, 1f)] private float shimmerVolume = 0.6f;
    [Tooltip("Harp plucks cycled one per tick, high and quiet: the sound of the heal landing.")]
    [SerializeField] private int[] sparkleSemitones = { 24, 26, 28, 31 };
    [SerializeField, Range(0f, 1f)] private float sparkleVolume = 0.14f;

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
        AudioSystem.Play(AudioSystem.Sound.Heal_Shimmer, shimmerVolume);
        context.Run(HealOverTime(context.Player));
    }

    private IEnumerator HealOverTime(Transform player)
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
            float before = manager.CurrentHp;
            manager.Heal(manager.MaxHp * maxHpFraction / ticks);
            float healed = manager.CurrentHp - before;

            // At full health a tick heals nothing, and saying "+2" anyway would be a lie.
            if (healed <= 0.01f || player == null)
            {
                continue;
            }

            ShowHealNumber(player.position, healed);

            if (sparkleSemitones.Length > 0)
            {
                int semitones = sparkleSemitones[i % sparkleSemitones.Length];
                AudioSystem.Play(AudioSystem.Sound.Harp_Pluck, sparkleVolume, Mathf.Pow(2f, semitones / 12f));
            }
        }
    }

    private void ShowHealNumber(Vector3 playerPosition, float healed)
    {
        if (healNumberPrefab == null || PrefabPool.Instance == null)
        {
            return;
        }

        Vector3 position = playerPosition + (Vector3)healNumberOffset
                           + Vector3.right * Random.Range(-healNumberJitter, healNumberJitter);
        GameObject number = PrefabPool.Instance.Spawn(healNumberPrefab, position, Quaternion.identity);

        TMPro.TMP_Text? label = number.GetComponentInChildren<TMPro.TMP_Text>();
        if (label != null)
        {
            label.text = "+" + Mathf.Max(1, Mathf.RoundToInt(healed));
            label.color = healNumberColor;
        }

        number.GetComponent<PooledInstance>()?.ReleaseAfter(healNumberSeconds);
    }
}
