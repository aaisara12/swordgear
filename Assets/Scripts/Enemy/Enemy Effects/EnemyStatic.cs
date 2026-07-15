using UnityEngine;
using static GameManager;

public class EnemyStatic : MonoBehaviour, IEnemyEffect
{
    [SerializeField] float speedMultiplier = 0.9f;

    EnemyEffect effect = EnemyEffect.Static;

    void IEnemyEffect.EffectBegin(EnemyController enemy)
    {
        enemy.speedMultiplier *= speedMultiplier;
        EnemyStatusVisual.For(enemy).SetStatic(true);
    }

    void IEnemyEffect.EffectEnd(EnemyController enemy)
    {
        enemy.speedMultiplier /= speedMultiplier;
        EnemyStatusVisual.For(enemy).SetStatic(false);
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
