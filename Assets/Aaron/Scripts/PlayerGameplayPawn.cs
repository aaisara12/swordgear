#nullable enable
using UnityEngine;

/// <summary>
/// "Interface" for controlling the player's in-game avatar
/// </summary>
// aisara => Using Unreal's terminology here because I think it aptly encapsulates the idea of an entity that
// simply serves as a vessel for player control and does nothing else.
public abstract class PlayerGameplayPawn : MonoBehaviour
{
    public abstract void Attack();
    public abstract void BeginChargeAttack();
    public abstract void ReleaseChargeAttack();
    public abstract void CancelChargeAttack();
    
    public abstract void AimInDirection(Vector2 direction);
    public abstract void DoAimedAttackInDirection(Vector2 direction);
    
    public abstract void MoveInDirection(Vector2 direction);
}