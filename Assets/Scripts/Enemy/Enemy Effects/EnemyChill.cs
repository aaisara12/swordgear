using UnityEngine;
using static GameManager;

public class EnemyChill : MonoBehaviour, IEnemyEffect
{
    [SerializeField] float speedMultiplier = 0.5f;

    EnemyEffect effect = EnemyEffect.Chill;

    void IEnemyEffect.EffectBegin(EnemyController enemy)
    {
        enemy.speedMultiplier *= speedMultiplier;
        EnemyStatusVisual.For(enemy).SetChill(true);
    }

    void IEnemyEffect.EffectEnd(EnemyController enemy)
    {
        enemy.speedMultiplier /= speedMultiplier;
        EnemyStatusVisual.For(enemy).SetChill(false);
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
