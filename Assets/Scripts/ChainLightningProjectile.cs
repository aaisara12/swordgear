using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class ChainLightningProjectile : MonoBehaviour
{
    [Header("Search Settings")]
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float searchConeAngle = 60f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Effect Settings")]
    [SerializeField] private float travelSpeed = 20f;
    [SerializeField] private int staticDuration = 3;

    [Header("Lightning Visuals")]
    [SerializeField] private int lightningPoints = 10;
    [SerializeField] private float jaggedness = 0.2f;
    [SerializeField] private float lightningDuration = 0.05f;

    private Transform spawnOrigin;
    private Vector2 forwardDir;
    private EnemyController currentTarget;
    private LineRenderer lineRenderer;

    private HashSet<EnemyController> pastTargets = new HashSet<EnemyController>();

    public void Initialize(Transform origin)
    {
        spawnOrigin = origin;
        forwardDir = (transform.position - spawnOrigin.position).normalized;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        StartCoroutine(ChainRoutine());
    }

    private IEnumerator ChainRoutine()
    {
        Vector2 currentPos = transform.position;

        while (true)
        {
            EnemyController nextTarget = FindNextTarget(currentPos, forwardDir);
            if (nextTarget == null)
            {
                Destroy(gameObject);
                yield break;
            }

            currentTarget = nextTarget;
            Vector2 targetPos = currentTarget.transform.position;
            forwardDir = (targetPos - currentPos).normalized;

            // Show lightning arc
            StartCoroutine(ShowLightningArc(currentPos, targetPos));

            // Fly toward the target
            yield return StartCoroutine(FlyToTarget(currentTarget.transform));
            if (currentTarget == null)
            {
                Destroy(gameObject);
                yield break;
            }

            bool alreadyStatic = GameManager.Instance.CheckEnemyEffect(currentTarget, GameManager.EnemyEffect.Static);
            GameManager.Instance.AddEffect(currentTarget, GameManager.EnemyEffect.Static, staticDuration);

            if (!alreadyStatic)
            {
                Destroy(gameObject);
                yield break;
            }
            // Already static
            currentTarget.TakeDamage(GameManager.Instance.CalculateDamage(currentTarget.element, Element.Ice, 10));
            currentPos = currentTarget.transform.position;
        }
    }

    private EnemyController FindNextTarget(Vector2 origin, Vector2 direction)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyMask);
        List<EnemyController> enemiesInCone = new List<EnemyController>();

        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && enemy.transform != spawnOrigin && !pastTargets.Contains(enemy))
            {
                Vector2 toEnemy = (enemy.transform.position - (Vector3)origin).normalized;
                float angle = Vector2.Angle(direction, toEnemy);
                if (angle <= searchConeAngle / 2f)
                {
                    enemiesInCone.Add(enemy);
                    pastTargets.Add(enemy);
                }
            }
        }

        if (enemiesInCone.Count == 0)
            return null;

        enemiesInCone = enemiesInCone.OrderBy(e => Vector2.Distance(origin, e.transform.position)).ToList();

        foreach (var e in enemiesInCone)
        {
            if (!GameManager.Instance.CheckEnemyEffect(e, GameManager.EnemyEffect.Static))
                return e;
        }

        return enemiesInCone.FirstOrDefault();
    }

    private IEnumerator FlyToTarget(Transform target)
    {
        while (target != null && Vector2.Distance(transform.position, target.position) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, travelSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator ShowLightningArc(Vector2 start, Vector2 end)
    {
        lineRenderer.positionCount = lightningPoints;

        for (int i = 0; i < lightningPoints; i++)
        {
            float t = (float)i / (lightningPoints - 1);
            Vector2 pointPos = Vector2.Lerp(start, end, t);

            // Offset to make it jagged
            Vector2 perpendicular = Vector2.Perpendicular((end - start).normalized);
            float offset = Random.Range(-jaggedness, jaggedness) * (1f - Mathf.Abs(t - 0.5f) * 2f);
            pointPos += perpendicular * offset;

            lineRenderer.SetPosition(i, pointPos);
        }

        yield return new WaitForSeconds(lightningDuration);
        lineRenderer.positionCount = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
        Vector3 dir = spawnOrigin != null ? forwardDir : transform.up;

        Gizmos.DrawWireSphere(origin, searchRadius);
        Vector3 leftBoundary = Quaternion.Euler(0, 0, searchConeAngle / 2f) * dir;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -searchConeAngle / 2f) * dir;

        Gizmos.DrawLine(origin, origin + leftBoundary * searchRadius);
        Gizmos.DrawLine(origin, origin + rightBoundary * searchRadius);
    }
}
