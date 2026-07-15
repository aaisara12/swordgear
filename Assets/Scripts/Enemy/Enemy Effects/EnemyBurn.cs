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
        // Fire-coloured DoT number, but don't let the burn feed the combo/ult (feedsCombo:false).
        enemy.TakeDamage(damage, new MoveType(Element.Fire, AttackKind.Ranged), applyImpactFeel: false, feedsCombo: false);
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
