using UnityEngine;

public interface IMovementStrategy
{
    void Move(Rigidbody2D rb, Transform targetTransform, float speed);
}