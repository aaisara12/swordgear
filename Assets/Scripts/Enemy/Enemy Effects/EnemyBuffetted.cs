using System.Collections;
using UnityEngine;
using static GameManager;

/// <summary>
/// Rending Gale's debuff: while active, the enemy takes an extra instance of wind damage every time
/// it takes damage. Unlike Burn/Chill/Static this isn't a per-second tick — it procs off
/// EnemyController.OnAnyEnemyHit, which only fires for feedsCombo:true hits, so the proc itself
/// (feedsCombo:false) can never re-trigger itself or feed the combo/ult.
/// </summary>
public class EnemyBuffetted : MonoBehaviour, IEnemyEffect
{
    [SerializeField] float extraDamage = 2f;

    EnemyEffect effect = EnemyEffect.Buffetted;

    void IEnemyEffect.EffectBegin(EnemyController enemy)
    {
        EnemyStatusVisual.For(enemy).SetBuffetted(true);
    }

    void IEnemyEffect.EffectEnd(EnemyController enemy)
    {
        EnemyStatusVisual.For(enemy).SetBuffetted(false);
    }

    void IEnemyEffect.EffectTick(EnemyController enemy)
    {
        // No periodic tick — the proc is driven by OnAnyEnemyHit below.
    }

    EnemyEffect IEnemyEffect.getEffect()
    {
        return effect;
    }

    private void OnEnable()
    {
        EnemyController.OnAnyEnemyHit += HandleAnyEnemyHit;
    }

    private void OnDisable()
    {
        EnemyController.OnAnyEnemyHit -= HandleAnyEnemyHit;
    }

    private void HandleAnyEnemyHit(EnemyController enemy, float damage, MoveType moveType)
    {
        if (enemy == null) return;
        if (!Instance.CheckEnemyEffect(enemy, effect)) return;

        // Deferred a frame so this proc lands after the triggering hit has fully resolved
        // (including its own death check) instead of re-entering mid-TakeDamage.
        StartCoroutine(DeferredProc(enemy));
    }

    private IEnumerator DeferredProc(EnemyController enemy)
    {
        yield return null;

        if (enemy == null) yield break;

        enemy.TakeDamage(extraDamage, new MoveType(Element.Wind, AttackKind.Ranged), applyImpactFeel: false, feedsCombo: false);
    }

    private void Start()
    {
        Instance.enemyEffect[effect] = this;
    }
}
