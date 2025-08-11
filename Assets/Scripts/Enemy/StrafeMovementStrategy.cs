using UnityEngine;

public class StrafeMovementStrategy : MonoBehaviour, IMovementStrategy
{
    [SerializeField] private float strafeDistance = 5f;
    [SerializeField] private float strafeSpeed = 2f;

    // Defines how close the enemy can get before it starts moving away
    [SerializeField] private float strafeDistanceTolerance = 1f;

    // A range for the random time to switch directions
    [SerializeField] private Vector2 directionChangeTimeRange = new Vector2(1f, 3f);
    private float nextDirectionChangeTime;
    private int strafeDirectionMultiplier = 1; // 1 for right, -1 for left
    private RangedAttackStrategy rangedAttackStrategy;

    private void Start()
    {
        // Randomly set the initial strafe direction
        strafeDirectionMultiplier = (Random.Range(0, 2) == 0) ? 1 : -1;
        // Set the first direction change time randomly within the defined range
        nextDirectionChangeTime = Time.time + Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);

        rangedAttackStrategy = GetComponent<RangedAttackStrategy>();
    }

    public void Move(Rigidbody2D rb, Transform targetTransform, float speed)
    {
        // If the enemy is in the middle of a charge-up, stand still
        if (rangedAttackStrategy != null && rangedAttackStrategy.IsAttacking())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (targetTransform.position - rb.transform.position).normalized;
        float currentDistance = Vector2.Distance(rb.transform.position, targetTransform.position);

        if (Time.time >= nextDirectionChangeTime)
        {
            // Flip the strafe direction
            strafeDirectionMultiplier *= -1;
            // Set the next time to change direction to a new random value
            nextDirectionChangeTime = Time.time + Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);
        }

        if (currentDistance > strafeDistance)
        {
            // Move closer if too far away
            rb.linearVelocity = direction * speed;
        }
        else if (currentDistance < strafeDistance - strafeDistanceTolerance)
        {
            // Move away if too close, using the new tolerance field
            rb.linearVelocity = -direction * speed;
        }
        else
        {
            // Use the multiplier to change strafing direction
            Vector2 strafeDirection = Vector2.Perpendicular(direction).normalized * strafeDirectionMultiplier;
            rb.linearVelocity = strafeDirection * strafeSpeed;
        }
    }
}