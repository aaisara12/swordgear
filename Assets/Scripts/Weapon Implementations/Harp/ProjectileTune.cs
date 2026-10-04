#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// A tune that fires notes at enemies: Flurry's run of small homing sixteenths, Strike's single heavy
/// quarter note.
/// </summary>
/// <remarks>
/// The projectile is a <see cref="PlayerProjectile"/>, the same pooled shot Fire's fireballs and Earth's
/// rock use, so homing, impact bursts and attunement all behave exactly as they do there. Each tune points
/// at its own note prefab because the pool keys by prefab.
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Projectile Tune", fileName = "ProjectileTune")]
public class ProjectileTune : HarpTune
{
    [SerializeField] private GameObject? notePrefab;
    [SerializeField, Min(1)] private int count = 1;
    [Tooltip("Seconds between notes, so a run reads as a run rather than a shotgun.")]
    [SerializeField, Min(0f)] private float interval = 0.06f;
    [Tooltip("Total fan, in degrees, the notes leave across, centred on the aim. Ignored for a ring.")]
    [SerializeField] private float spreadDegrees = 0f;
    [Tooltip("Fire evenly all the way round, starting straight at the aim (Staccato). A centred fan of an " +
             "even count never sends a note straight at the target.")]
    [SerializeField] private bool ring = false;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float spawnOffset = 0.6f;
    [Tooltip("Damage per note, as a multiple of base.")]
    [SerializeField] private float damageMultiplier = 0.35f;
    [Tooltip("Degrees per second a note can turn toward its target. 0 flies straight.")]
    [SerializeField, Min(0f)] private float homingTurnRate = 0f;
    [Tooltip("Burst radius on impact. 0 damages only what it hits.")]
    [SerializeField, Min(0f)] private float explosionRadius = 0f;
    [Tooltip("How far to look for an enemy to aim the volley at. With none in range it leaves along the " +
             "player's facing.")]
    [SerializeField] private float aimRadius = 12f;

    public override void Play(HarpContext context)
    {
        if (notePrefab == null)
        {
            Debug.LogError($"ProjectileTune '{name}': notePrefab is null");
            return;
        }

        context.Run(Volley(context.Player));
    }

    private IEnumerator Volley(Transform player)
    {
        float step = ring ? 360f / count : count > 1 ? spreadDegrees / (count - 1) : 0f;
        float start = ring ? 0f : -spreadDegrees * 0.5f;

        for (int i = 0; i < count; i++)
        {
            if (player == null || PrefabPool.Instance == null || GameManager.Instance == null)
            {
                yield break;
            }

            // Re-aimed per note, so a run tracks a target that moves mid-volley.
            Vector2 aim = AimDirection(player);
            float angle = count > 1 ? start + step * i : 0f;
            Launch(player, Quaternion.Euler(0f, 0f, angle) * aim);

            if (interval > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private Vector2 AimDirection(Transform player)
    {
        if (ActiveEnemyRegistry.TryGetNearest(player.position, aimRadius, out EnemyController nearest, out _) && nearest != null)
        {
            Vector2 toEnemy = (Vector2)nearest.transform.position - (Vector2)player.position;
            if (toEnemy.sqrMagnitude > 0.0001f)
            {
                return toEnemy.normalized;
            }
        }

        Vector2 facing = player.up;
        return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
    }

    private void Launch(Transform player, Vector2 direction)
    {
        Vector3 spawnPos = player.position + (Vector3)(direction * spawnOffset);
        GameObject obj = PrefabPool.Instance!.Spawn(notePrefab!, spawnPos, Quaternion.identity);
        PlayerProjectile? projectile = obj.GetComponent<PlayerProjectile>();
        if (projectile == null)
        {
            Debug.LogError($"ProjectileTune '{name}': notePrefab has no PlayerProjectile");
            PrefabPool.Instance.Release(obj);
            return;
        }

        projectile.Launch(Element.Light, GameManager.Instance!.GetEffectiveBaseDamage() * damageMultiplier, direction, speed);

        if (homingTurnRate > 0f)
        {
            projectile.EnableHoming(homingTurnRate);
        }

        if (explosionRadius > 0f)
        {
            projectile.EnableExplosion(explosionRadius);
        }
    }
}
