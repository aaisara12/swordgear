#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Represents the visual representation of the player in gameplay, handling physical actions such as movement and attacks.
/// </summary>
// aisara => Using Unreal's terminology here because I think it aptly encapsulates the idea of an entity that
// simply serves as a vessel for player control and does nothing else.
public abstract class PlayerGameplayPawn : MonoBehaviour
{
    public event Action<float>? OnRegisterDamage;
    
    public abstract void Attack();
    public abstract void BeginChargeAttack();
    public abstract void ReleaseChargeAttack();
    public abstract void CancelChargeAttack();
    
    public abstract void AimInDirection(Vector2 direction);
    public abstract void DoAimedAttackInDirection(Vector2 direction);
    
    public abstract void MoveInDirection(Vector2 direction);

    public abstract void DoSpawnAnimation();

    public abstract void DoDefeatAnimation();
    
    // aisara => Note how we don't track health in the pawn because it's simply a vessel for player control.
    // that responsibility belongs to another component
    public void RegisterDamage(float amount)
    {
        OnRegisterDamage?.Invoke(amount);
    }
}