using System.Collections;
using UnityEngine;

/// <summary>
/// Wind wisps that swirl around the sword after a partially-charged throw. Purely visual contact —
/// damage comes from a periodic AoE tick centered on the sword's current position, not from the
/// wisp particles touching enemies.
/// </summary>
public class WindWispEffect : MonoBehaviour
{
    [SerializeField] private float visualRadiusReference = 1.5f;
    [SerializeField] private ParticleSystem[] wispParticles;

    private Coroutine _routine;
    private Transform _followTarget;

    public void Begin(Transform followTarget, float radius, float damagePerTick, float tickInterval, float duration)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _followTarget = followTarget;

        float visualScale = radius / Mathf.Max(0.01f, visualRadiusReference);
        transform.localScale = Vector3.one * visualScale;

        if (wispParticles != null)
        {
            foreach (ParticleSystem ps in wispParticles)
            {
                if (ps == null) continue;
                ps.Play(true);
            }
        }

        _routine = StartCoroutine(TickRoutine(radius, damagePerTick, tickInterval, duration));
    }

    private void LateUpdate()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
        }
    }

    private IEnumerator TickRoutine(float radius, float damagePerTick, float tickInterval, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            DealDamage(radius, damagePerTick);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        PrefabPool.Instance!.Release(gameObject);
    }

    private void DealDamage(float radius, float damagePerTick)
    {
        MeleeAugmentUtility.DamageEnemiesInRadius(transform.position, radius, enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Wind, damagePerTick),
                new MoveType(Element.Wind, AttackKind.Ranged));
        });
    }
}
