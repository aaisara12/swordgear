using UnityEngine;

/// <summary>Turret enemies — does not reposition.</summary>
public class StationaryMovementStrategy : MonoBehaviour, IMovementStrategy
{
    public void Move(Rigidbody2D rb, Transform targetTransform, float speed)
    {
        rb.linearVelocity = Vector2.zero;
    }
}
