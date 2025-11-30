using UnityEngine;
using static GameManager;

public class EnemyChill : IEnemyEffect
{
    [SerializeField] float speedMultiplier = 0.5f;

    EnemyEffect effect = EnemyEffect.Chill;

    void IEnemyEffect.EffectBegin(EnemyController enemy)
    {
        enemy.speedMultiplier *= speedMultiplier;
    }

    void IEnemyEffect.EffectEnd(EnemyController enemy)
    {
        enemy.speedMultiplier /= speedMultiplier;
    }

    void IEnemyEffect.EffectTick(EnemyController enemy)
    {

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
