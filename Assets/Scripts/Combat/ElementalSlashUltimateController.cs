#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalSlashUltimateController : MonoBehaviour
{
    [Serializable]
    public struct SlashEntry
    {
        public MonoBehaviour effect; // Must implement IAttackAnimator
        public Element element;
        public float damageMultiplier;
        public float damageRadius;
        public float duration;
    }

    [SerializeField] private List<SlashEntry> _slashes = new();
    [SerializeField] private List<SlashEntry> _finishingSlashes = new();

    public void Begin(Transform player)
    {
        StartCoroutine(ExecuteSequence(player));
    }

    private IEnumerator ExecuteSequence(Transform player)
    {
        PlayerController? playerController = player.GetComponent<PlayerController>();
        playerController?.SetUltimateInvincible(true);
        playerController?.SetUltimateFrozen(true);

        if (playerController != null)
            yield return playerController.PlayVanishAndHide();

        List<EnemyController> enemySnapshot = new(ActiveEnemyRegistry.All);

        foreach (SlashEntry slash in _slashes)
        {
            (slash.effect as IAttackAnimator)?.PlayAnimation();
            DamageEnemiesInRadius(player.position, slash, enemySnapshot);
            yield return new WaitForSeconds(slash.duration);
        }

        if (_finishingSlashes.Count > 0)
        {
            enemySnapshot = new(ActiveEnemyRegistry.All);
            Vector2 finishOrigin = player.position;
            int remaining = _finishingSlashes.Count;

            foreach (SlashEntry slash in _finishingSlashes)
                StartCoroutine(ExecuteFinishingSlash(slash, finishOrigin, enemySnapshot, () => remaining--));

            yield return new WaitUntil(() => remaining <= 0);
        }

        if (playerController != null)
            yield return playerController.PlayAppearAndShow();

        UltimateChargeTracker.Instance?.EndExecution();
        playerController?.SetUltimateInvincible(false);
        playerController?.SetUltimateFrozen(false);
        Destroy(gameObject);
    }

    private IEnumerator ExecuteFinishingSlash(SlashEntry slash, Vector2 origin, List<EnemyController> enemies, Action onDone)
    {
        (slash.effect as IAttackAnimator)?.PlayAnimation();
        DamageEnemiesInRadius(origin, slash, enemies);
        yield return new WaitForSeconds(slash.duration);
        onDone();
    }

    private static void DamageEnemiesInRadius(Vector2 origin, SlashEntry slash, List<EnemyController> enemies)
    {
        float radiusSqr = slash.damageRadius * slash.damageRadius;
        float baseDamage = GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveBaseDamage() * slash.damageMultiplier
            : slash.damageMultiplier;

        var moveType = new MoveType(slash.element, AttackKind.MeleeStrike);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > radiusSqr) continue;

            float damage = GameManager.Instance != null
                ? GameManager.Instance.CalculateDamage(enemy.element, slash.element, baseDamage)
                : baseDamage;

            enemy.TakeDamage(damage, moveType, damageElementOverride: slash.element);
        }
    }
}
