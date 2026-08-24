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

    [Header("Level Scaling")]
    [Tooltip("The slash sequence runs once per ult level, up to this many passes. The finisher always plays once.")]
    [SerializeField] private int _maxPasses = 5;

    [Tooltip("Damage scale per ult level — entry 0 is level 1. Levels past the end extend by Damage Per Extra Level.")]
    [SerializeField] private List<float> _damageMultiplierByLevel = new() { 1f, 1.4f, 1.8f };

    [SerializeField] private float _damagePerExtraLevel = 0.4f;

    public void Begin(Transform player, int level = 1)
    {
        StartCoroutine(ExecuteSequence(player, Mathf.Max(1, level)));
    }

    private IEnumerator ExecuteSequence(Transform player, int level)
    {
        PlayerController? playerController = player.GetComponent<PlayerController>();
        playerController?.SetUltimateInvincible(true);
        playerController?.SetUltimateFrozen(true);

        if (playerController != null)
            yield return playerController.PlayVanishAndHide();

        int passes = Mathf.Clamp(level, 1, Mathf.Max(1, _maxPasses));
        float damageScale = GetDamageMultiplier(level);

        for (int pass = 0; pass < passes; pass++)
        {
            // Re-snapshot each pass so enemies killed by the previous one drop out.
            List<EnemyController> enemySnapshot = new(ActiveEnemyRegistry.All);

            foreach (SlashEntry slash in _slashes)
            {
                (slash.effect as IAttackAnimator)?.PlayAnimation();
                DamageEnemiesInRadius(player.position, slash, enemySnapshot, damageScale);
                yield return new WaitForSeconds(slash.duration);
            }
        }

        if (_finishingSlashes.Count > 0)
        {
            List<EnemyController> finishSnapshot = new(ActiveEnemyRegistry.All);
            Vector2 finishOrigin = player.position;
            int remaining = _finishingSlashes.Count;

            foreach (SlashEntry slash in _finishingSlashes)
                StartCoroutine(ExecuteFinishingSlash(slash, finishOrigin, finishSnapshot, damageScale, () => remaining--));

            yield return new WaitUntil(() => remaining <= 0);
        }

        if (playerController != null)
            yield return playerController.PlayAppearAndShow();

        UltimateChargeTracker.Instance?.EndExecution();
        playerController?.SetUltimateInvincible(false);
        playerController?.SetUltimateFrozen(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// Level 1 uses the first entry. Beyond the authored list the curve keeps climbing linearly so an infinitely
    /// overcharged ult never plateaus.
    /// </summary>
    private float GetDamageMultiplier(int level)
    {
        if (_damageMultiplierByLevel.Count == 0)
            return 1f;

        int index = Mathf.Max(1, level) - 1;
        int lastIndex = _damageMultiplierByLevel.Count - 1;

        if (index <= lastIndex)
            return _damageMultiplierByLevel[index];

        return _damageMultiplierByLevel[lastIndex] + _damagePerExtraLevel * (index - lastIndex);
    }

    private IEnumerator ExecuteFinishingSlash(SlashEntry slash, Vector2 origin, List<EnemyController> enemies, float damageScale, Action onDone)
    {
        (slash.effect as IAttackAnimator)?.PlayAnimation();
        DamageEnemiesInRadius(origin, slash, enemies, damageScale);
        yield return new WaitForSeconds(slash.duration);
        onDone();
    }

    private static void DamageEnemiesInRadius(Vector2 origin, SlashEntry slash, List<EnemyController> enemies, float damageScale)
    {
        float radiusSqr = slash.damageRadius * slash.damageRadius;
        float scaledMultiplier = slash.damageMultiplier * damageScale;
        float baseDamage = GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveBaseDamage() * scaledMultiplier
            : scaledMultiplier;

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
