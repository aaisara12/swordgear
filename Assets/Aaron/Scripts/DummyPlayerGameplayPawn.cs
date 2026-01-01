#nullable enable

using UnityEngine;

namespace Testing
{
    /// <summary>
    /// Broadcasts player gameplay actions via Unity's messaging system so that multiple components can respond to them
    /// </summary>
    public class DummyPlayerGameplayPawn : PlayerGameplayPawn
    {
        [SerializeField] private Mover? mover;
        [SerializeField] private Attacker? attacker;
        [SerializeField] private Shooter? shooter;
        [SerializeField] private ShootDirectionVisualizer? shootDirectionVisualizer;
        
        public override void Attack()
        {
            attacker?.Attack();
        }

        public override void AimSwordInDirection(Vector2 direction)
        {
            shootDirectionVisualizer?.SetShootDirection(direction);
        }

        public override void ThrowSwordInDirection(Vector2 direction)
        {
            shooter?.ShootInDirection(direction);
        }

        public override void MoveInDirection(Vector2 direction)
        {
            mover?.Move(direction);
        }

        public override void BeginChargeAttack()
        {
            Debug.Log("Charge attack started");
        }

        public override void ReleaseChargeAttack()
        {
            Debug.Log("Charge attack released");
        }

        public override void CancelChargeAttack()
        {
            Debug.Log("Charge attack canceled");
        }
    }
}

