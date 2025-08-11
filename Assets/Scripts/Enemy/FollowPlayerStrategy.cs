using UnityEngine;

public class FollowPlayerStrategy : MonoBehaviour, IMovementStrategy
{
    public void Move(Rigidbody2D rb, Transform targetTransform, float speed)
    {
        Vector2 direction = (targetTransform.position - rb.transform.position).normalized;
        rb.linearVelocity = direction * speed;
    }
}