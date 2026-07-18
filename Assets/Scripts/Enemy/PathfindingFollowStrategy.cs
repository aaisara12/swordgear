using UnityEngine;

// Chases the player around walls using the shared flow field, and cuts straight across open ground
// whenever the body has a clear path, so movement does not look grid-locked in empty rooms.
public class PathfindingFollowStrategy : MonoBehaviour, IMovementStrategy
{
    [SerializeField] private float steeringSharpness = 12f;

    private const float CenteringBias = 0.35f;

    private float bodyRadius = 0.25f;
    private Vector2 smoothedDirection;

    private void Awake()
    {
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
        Vector2 desired = (target - self).normalized;

        if (!EnemyVision.CanWalk(self, target, bodyRadius))
        {
            EnemyFlowField field = EnemyFlowField.Instance;
            if (field != null && field.TryGetSteeredDirection(self, CenteringBias, out Vector2 flowDirection))
            {
                desired = flowDirection;
            }
        }

        smoothedDirection = smoothedDirection == Vector2.zero
            ? desired
            : Vector2.Lerp(smoothedDirection, desired, 1f - Mathf.Exp(-steeringSharpness * Time.fixedDeltaTime));

        rb.linearVelocity = smoothedDirection.normalized * speed;
    }
}
