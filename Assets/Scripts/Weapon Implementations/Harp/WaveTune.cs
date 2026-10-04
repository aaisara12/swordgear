#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tune that sends sound waves rolling out from the player: Resonance. Each wave damages every enemy
/// once, at the moment its front passes them.
/// </summary>
/// <remarks>
/// A travelling wave rather than a point-blank burst, because Light is a gamble: every tune has to pay out
/// in most situations. A burst only paid when enemies were already on you and rolled a dud otherwise; a
/// wave that reaches most of the screen pays out whether the room is crowded or spread out.
/// <para>
/// The front's position is read back from the wave's animation (<see cref="HarpWave.FrontRadius"/>), not
/// recomputed here, so the ring on screen is exactly where the damage is.
/// </para>
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Wave Tune", fileName = "WaveTune")]
public class WaveTune : HarpTune
{
    [Tooltip("The travelling ring. Must carry HarpWave.")]
    [SerializeField] private GameObject? wavePrefab;
    [SerializeField, Min(1)] private int waves = 2;
    [Tooltip("Seconds between waves. Match the melody's note spacing so each pluck sends a wave.")]
    [SerializeField, Min(0f)] private float interval = 0.15f;
    [Tooltip("How far a wave travels before it dies. Just inside the gear ring reads as 'the whole room'.")]
    [SerializeField] private float radius = 8f;
    [Tooltip("Seconds for a wave to reach full radius.")]
    [SerializeField] private float travelSeconds = 0.5f;
    [Tooltip("Damage per wave, as a multiple of base.")]
    [SerializeField] private float damageMultiplier = 0.5f;

    private readonly List<EnemyController> scanBuffer = new();

    public override void Play(HarpContext context)
    {
        if (wavePrefab == null)
        {
            Debug.LogError($"WaveTune '{name}': wavePrefab is null");
            return;
        }

        for (int i = 0; i < waves; i++)
        {
            context.Run(Wave(context.Player, i * interval));
        }
    }

    private IEnumerator Wave(Transform player, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (player == null || PrefabPool.Instance == null)
        {
            yield break;
        }

        // Emitted from where it was played and left there: a sound wave doesn't follow the harpist.
        Vector2 origin = player.position;
        GameObject obj = PrefabPool.Instance.Spawn(wavePrefab!, origin, Quaternion.identity);
        HarpWave? wave = obj.GetComponent<HarpWave>();
        if (wave == null)
        {
            Debug.LogError($"WaveTune '{name}': wavePrefab has no HarpWave");
            PrefabPool.Instance.Release(obj);
            yield break;
        }

        wave.Play(radius, travelSeconds);
        obj.GetComponent<PooledInstance>()?.ReleaseAfter(travelSeconds);

        var hit = new HashSet<EnemyController>();
        float elapsed = 0f;
        while (elapsed < travelSeconds)
        {
            DamagePassedEnemies(origin, wave.FrontRadius, hit);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final sweep at full radius, so a frame boundary can't let an enemy at the very edge slip past.
        DamagePassedEnemies(origin, radius, hit);
    }

    /// <summary>Damages every enemy the front has reached that this wave hasn't hit yet.</summary>
    private void DamagePassedEnemies(Vector2 origin, float frontRadius, HashSet<EnemyController> hit)
    {
        GameManager? gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        // Snapshot: a kill unregisters the enemy from the live list mid-loop.
        scanBuffer.Clear();
        scanBuffer.AddRange(ActiveEnemyRegistry.All);

        float frontSqr = frontRadius * frontRadius;
        float damage = gameManager.GetEffectiveBaseDamage() * damageMultiplier;

        foreach (EnemyController enemy in scanBuffer)
        {
            if (enemy == null || hit.Contains(enemy))
            {
                continue;
            }

            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > frontSqr)
            {
                continue;
            }

            hit.Add(enemy);

            // Attunement, matching PlayerProjectile: a same-element enemy is untouched by its own element.
            if (gameManager.IsAttunementBlocked(Element.Light, enemy.element))
            {
                continue;
            }

            enemy.TakeDamage(
                gameManager.CalculateDamage(enemy.element, Element.Light, damage),
                new MoveType(Element.Light, AttackKind.Ranged));
        }
    }
}
