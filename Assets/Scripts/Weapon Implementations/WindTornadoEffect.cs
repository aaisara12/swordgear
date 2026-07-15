using System.Collections;
using UnityEngine;

/// <summary>
/// Max-charge wind throw: a helix/tornado follows the sword, periodically damaging and pulling
/// enemies toward its center, and destroys enemy projectiles that enter it (via a child collider
/// tagged "ProjectileBlocking", the same tag melee hitboxes use to intercept projectiles).
/// </summary>
public class WindTornadoEffect : MonoBehaviour
{
    [SerializeField] private float visualRadiusReference = 4f;
    [SerializeField] private ParticleSystem[] tornadoParticles;

    private Coroutine _routine;
    private Transform _followTarget;

    public void Begin(Transform followTarget, float radius, float damagePerTick, float tickInterval, float duration, float pullForce)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _followTarget = followTarget;

        float visualScale = radius / Mathf.Max(0.01f, visualRadiusReference);
        transform.localScale = Vector3.one * visualScale;

        if (tornadoParticles != null)
        {
            foreach (ParticleSystem ps in tornadoParticles)
            {
                if (ps == null) continue;
                ps.Play(true);
            }
        }

        _routine = StartCoroutine(TickRoutine(radius, damagePerTick, tickInterval, duration, pullForce));
    }

    private void LateUpdate()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
        }
    }

    private IEnumerator TickRoutine(float radius, float damagePerTick, float tickInterval, float duration, float pullForce)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Pulse(radius, damagePerTick, pullForce);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        PrefabPool.Instance!.Release(gameObject);
    }

    private void Pulse(float radius, float damagePerTick, float pullForce)
    {
        Vector2 center = transform.position;
        MeleeAugmentUtility.DamageEnemiesInRadius(center, radius, enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Wind, damagePerTick),
                new MoveType(Element.Wind, AttackKind.Ranged), applyImpactFeel: false);
            enemy.ApplyPull(center, pullForce);
        });
    }
}
