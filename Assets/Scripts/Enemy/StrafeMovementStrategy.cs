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
        // If the enemy is in the middle of a charge-up, stand still
        if (chargingAttack != null && chargingAttack.IsCharging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 self = rb.position;
        Vector2 target = targetTransform.position;
        Vector2 toTarget = target - self;
        float currentDistance = toTarget.magnitude;
        Vector2 direction = currentDistance > Mathf.Epsilon ? toTarget / currentDistance : Vector2.right;

        // Holding the ring is pointless behind cover, so close in until the shot opens up.
        if (!EnemyVision.CanShoot(self, target))
        {
            rb.linearVelocity = SeekSight(self, direction) * speed;
            return;
        }

        if (Time.time >= nextDirectionChangeTime)
        {
            strafeDirectionMultiplier *= -1;
            nextDirectionChangeTime = Time.time + Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);
        }

        if (currentDistance > strafeDistance)
        {
            rb.linearVelocity = direction * speed;
            return;
        }

        if (currentDistance < strafeDistance - strafeDistanceTolerance && IsClearAhead(self, -direction))
        {
            rb.linearVelocity = -direction * speed;
            return;
        }

        Vector2 strafeDirection = Vector2.Perpendicular(direction).normalized * strafeDirectionMultiplier;
        if (!IsClearAhead(self, strafeDirection))
        {
            strafeDirectionMultiplier *= -1;
            strafeDirection = -strafeDirection;
        }

        rb.linearVelocity = IsClearAhead(self, strafeDirection) ? strafeDirection * strafeSpeed : Vector2.zero;
    }

    private Vector2 SeekSight(Vector2 self, Vector2 fallbackDirection)
    {
        EnemyFlowField field = EnemyFlowField.Instance;
        return field != null && field.TryGetDirection(self, out Vector2 flowDirection)
            ? flowDirection
            : fallbackDirection;
    }

    private bool IsClearAhead(Vector2 self, Vector2 direction)
    {
        return EnemyVision.CanWalk(self, self + direction * clearanceProbeDistance, bodyRadius);
    }
}