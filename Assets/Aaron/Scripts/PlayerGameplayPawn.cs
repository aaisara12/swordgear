#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Represents the visual representation of the player in gameplay, handling physical actions such as movement and attacks.
/// </summary>
// aisara => Using Unreal's terminology here because I think it aptly encapsulates the idea of an entity that
// simply serves as a vessel for player control and does nothing else.
public abstract class 
    PlayerGameplayPawn : MonoBehaviour
{
    public event Action<float>? OnRegisterDamage;
    
    public abstract void Attack(Vector2 direction);
    
    // TODO: Charge attacks will also need support for directionality (given that basic attack has directionality)
    public abstract void BeginChargeAttack();
    public abstract void ReleaseChargeAttack();
    public abstract void CancelChargeAttack();
    
    public abstract void AimInDirection(Vector2 direction);
    public abstract void DoAimedAttackInDirection(Vector2 direction);
    public abstract void StopAiming();
    
    public abstract void MoveInDirection(Vector2 direction);

    /// <summary>
    /// The most recent direction passed to <see cref="MoveInDirection"/>, exposed so visual-only
    /// components (e.g. sprite facing) can react to movement input without depending on the
    /// concrete pawn implementation.
    /// </summary>
    public abstract Vector2 MoveDirection { get; }

    /// <summary>
    /// Resets transient combat/movement state so the pawn is clean when (re)spawned into a node:
    /// stops in-flight weapons, cancels dashes/recalls, clears cooldowns and velocity, and resets facing.
    /// Does not touch health (owned by <see cref="PlayerGameplayManager"/>).
    /// </summary>
    public abstract void ResetForNode();

    public virtual void UseUltimate() { }

    public abstract void DoSpawnAnimation();

    public abstract void DoDefeatAnimation();
    
    // aisara => Note how we don't track health in the pawn because it's simply a vessel for player control.
    // that responsibility belongs to another component
    public void RegisterDamage(float amount)
    {
        OnRegisterDamage?.Invoke(amount);
    }
}