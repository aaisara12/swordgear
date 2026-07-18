using UnityEngine;

public class StrafeMovementStrategy : MonoBehaviour, IMovementStrategy
{
    [SerializeField] private float strafeDistance = 5f;
    [SerializeField] private float strafeSpeed = 2f;

    // Defines how close the enemy can get before it starts moving away
    [SerializeField] private float strafeDistanceTolerance = 1f;

    // A range for the random time to switch directions
    [SerializeField] private Vector2 directionChangeTimeRange = new Vector2(1f, 3f);
    [Tooltip("How far ahead to probe before committing to a strafe or retreat direction.")]
    [SerializeField] private float clearanceProbeDistance = 1.5f;

    [Tooltip("Uncheck for attackers that fire through cover, so they hold the ring instead of closing in.")]
    [SerializeField] private bool requiresLineOfSight = true;

    private const float CenteringBias = 0.35f;

    private float nextDirectionChangeTime;
    private int strafeDirectionMultiplier = 1; // 1 for right, -1 for left
    private IChargingAttackStrategy? chargingAttack;
    private float bodyRadius = 0.25f;

    private void Start()
    {
        // Randomly set the initial strafe direction
        strafeDirectionMultiplier = (Random.Range(0, 2) == 0) ? 1 : -1;
        // Set the first direction change time randomly within the defined range
        nextDirectionChangeTime = Time.time + Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);

        chargingAttack = GetComponent<IChargingAttackStrategy>();

        CircleCollider2D body = GetComponent<CircleCollider2D>();
        if (body != null)
        {
            bodyRadius = body.radius * Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
        }
    }

    public void Move(Rigidbody2D rb, Transform targetTransform, float speed)
    {
        Vector2 self = rb.position;
        Vector2 target = targetTransform.position;
        Vector2 toTarget = target - self;
        float currentDistance = toTarget.magnitude;
        Vector2 direction = currentDistance > Mathf.Epsilon ? toTarget / currentDistance : Vector2.right;

        // Standing still telegraphs the shot, but doing it while still closing in reads as stutter-stepping,
        // so only hold once the enemy has reached the distance it wants to fight from.
        if (chargingAttack != null && chargingAttack.IsCharging && currentDistance <= strafeDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Holding the ring is pointless behind cover, so close in until the shot opens up.
        if (requiresLineOfSight && !EnemyVision.CanShoot(self, target))
        {
            rb.linearVelocity = Steer(self, SeekSight(self, direction)) * speed;
            return;
        }

        if (Time.time >= nextDirectionChangeTime)
        {
            strafeDirectionMultiplier *= -1;
            nextDirectionChangeTime = Time.time + Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);
        }

        if (currentDistance > strafeDistance)
        {
            // A clear shot is a hairline ray; the body still needs a corridor it actually fits through.
            Vector2 approach = EnemyVision.CanWalk(self, target, bodyRadius) ? direction : SeekSight(self, direction);
            rb.linearVelocity = Steer(self, approach) * speed;
            return;
        }

        if (currentDistance < strafeDistance - strafeDistanceTolerance)
        {
            rb.linearVelocity = Steer(self, -direction) * speed;
            return;
        }

        Vector2 strafeDirection = Vector2.Perpendicular(direction).normalized * strafeDirectionMultiplier;
        if (!IsClearAhead(self, strafeDirection))
        {
            strafeDirectionMultiplier *= -1;
            strafeDirection = -strafeDirection;
        }

        rb.linearVelocity = Steer(self, strafeDirection) * strafeSpeed;
    }

    private Vector2 SeekSight(Vector2 self, Vector2 fallbackDirection)
    {
        EnemyFlowField field = EnemyFlowField.Instance;
        return field != null && field.TryGetSteeredDirection(self, CenteringBias, out Vector2 flowDirection)
            ? flowDirection
            : fallbackDirection;
    }

    private Vector2 Steer(Vector2 self, Vector2 desired)
    {
        if (desired.sqrMagnitude < 0.0001f)
        {
            return desired;
        }

        desired = desired.normalized;
        float probeRadius = bodyRadius * 0.95f;
        RaycastHit2D hit = Physics2D.CircleCast(self, probeRadius, desired, clearanceProbeDistance, EnemyVision.MovementMask);
        if (hit.collider == null)
        {
            return desired;
        }

        Vector2 tangent = Vector2.Perpendicular(hit.normal);
        if (Vector2.Dot(tangent, desired) < 0f)
        {
            tangent = -tangent;
        }

        if (Physics2D.CircleCast(self, probeRadius, tangent, clearanceProbeDistance, EnemyVision.MovementMask).collider == null)
        {
            return tangent;
        }

        // Boxed in on both axes — push off the surface rather than grinding into it.
        return hit.normal;
    }

    private bool IsClearAhead(Vector2 self, Vector2 direction)
    {
        return EnemyVision.CanWalk(self, self + direction * clearanceProbeDistance, bodyRadius);
    }
}