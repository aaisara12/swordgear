using UnityEngine;
using static GameManager;

public class EnemyBurn : MonoBehaviour, IEnemyEffect
{
    [SerializeField] float damage = 1f;

    EnemyEffect effect = EnemyEffect.Burn;

    void IEnemyEffect.EffectBegin(EnemyController enemy)
    {
    }

    void IEnemyEffect.EffectEnd(EnemyController enemy)
    {
    }

    void IEnemyEffect.EffectTick(EnemyController enemy)
    {
        enemy.TakeDamage(damage);
    }

    EnemyEffect IEnemyEffect.getEffect()
    {
        return effect;
    }

    private void Start()
    {
        Instance.enemyEffect[effect] = this;
    }
}
