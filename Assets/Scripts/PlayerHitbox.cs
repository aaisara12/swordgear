using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] [Tooltip("Whether we should consider this hitbox melee or ranged")] 
    bool isMelee = true;
    [SerializeField] [Tooltip("For ranged hitboxes, set this if we should pass a different transform as the source of the collision")] 
    Transform rangedHitSource = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            if (enemy)
            {
                if (isMelee)
                {
                    ElementManager.Instance.OnEnemyMeleeHit(enemy);
                }
                else
                {
                    Transform hitSource = rangedHitSource ? rangedHitSource : transform;
                    ElementManager.Instance.OnEnemyRangedHit(enemy, hitSource);
                }
            }
        }
    }
}
