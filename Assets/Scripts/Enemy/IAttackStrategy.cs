using UnityEngine;

public interface IAttackStrategy
{
    void Attack(Transform selfTransform, Transform targetTransform);
}