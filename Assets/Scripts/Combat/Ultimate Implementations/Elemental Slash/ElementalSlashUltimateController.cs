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

    [Header("Overcharge Scaling")]
    [Tooltip("The slash sequence runs one extra time per overcharge level, up to this many passes. " +
             "The finisher always plays once.")]
    [SerializeField] private int _maxPasses = 5;

    [Tooltip("Damage scale per overcharge level — entry 0 is the base ult. Levels past the end extend by " +
             "Damage Per Extra Level.")]
    [SerializeField] private List<float> _damageMultiplierByLevel = new() { 1f, 1.4f, 1.8f };

    [SerializeField] private float _damagePerExtraLevel = 0.4f;

    // Inactive stand-ins for the authored slash effects, one per _slashes entry, built before the first pass
    // consumes the originals. Null where an entry has no effect assigned.
    private readonly List<GameObject?> _slashTemplates = new();

    private PlayerController? _playerController;
    private bool _sequenceFinished;

    /// <summary> <paramref name="overchargeLevel"/> is 0 for the base ult; each level adds a slash pass. </summary>
    public void Begin(Transform player, int overchargeLevel = 0)
    {
        StartCoroutine(ExecuteSequence(player, Mathf.Max(0, overchargeLevel)));
    }

    private IEnumerator ExecuteSequence(Transform player, int overchargeLevel)
    {
        _playerController = player.GetComponent<PlayerController>();
        _playerController?.SetUltimateInvincible(true);
        _playerController?.SetUltimateFrozen(true);

        if (_playerController != null)
            yield return _playerController.PlayVanishAndHide();

        int passes = Mathf.Clamp(overchargeLevel + 1, 1, Mathf.Max(1, _maxPasses));
        float damageScale = GetDamageMultiplier(overchargeLevel);

        if (passes > 1)
            CacheSlashTemplates();

        for (int pass = 0; pass < passes; pass++)
        {
            // Re-snapshot each pass so enemies killed by the previous one drop out.
            List<EnemyController> enemySnapshot = new(ActiveEnemyRegistry.All);

            for (int i = 0; i < _slashes.Count; i++)
            {
                SlashEntry slash = _slashes[i];
                PlaySlashEffect(i, slash, pass);
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

        if (_playerController != null)
            yield return _playerController.PlayAppearAndShow();

        _sequenceFinished = true;
        UltimateChargeTracker.Instance?.EndExecution();
        _playerController?.SetUltimateInvincible(false);
        _playerController?.SetUltimateFrozen(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// The sequence never reached its end — a scene unload, or the object destroyed from outside. Without this
    /// the tracker stays mid-execution (so the ult can never charge again) and the player keeps the frozen,
    /// invincible, hidden state the vanish put them in.
    /// </summary>
    private void OnDestroy()
    {
        if (_sequenceFinished) return;

        UltimateChargeTracker.Instance?.EndExecution();
        if (_playerController != null)
            _playerController.CancelUltimateState();
    }

    /// <summary>
    /// The authored slash effects are one-shot: AnimatorEffectController hands its GameObject to the prefab pool
    /// when the animation ends, and because these are prefab children the pool never spawned, that path destroys
    /// them. An overcharged ult replays the sequence, so snapshot an inactive copy of each effect up front and
    /// spawn later passes from those instead of replaying corpses.
    /// </summary>
    private void CacheSlashTemplates()
    {
        _slashTemplates.Clear();

        foreach (SlashEntry slash in _slashes)
        {
            if (slash.effect == null)
            {
                _slashTemplates.Add(null);
                continue;
            }

            GameObject source = slash.effect.gameObject;
            // Fall back to this object as the parent so templates and their copies are always torn down with the
            // ult, even if an effect was authored as a root object rather than a child.
            Transform parent = source.transform.parent != null ? source.transform.parent : transform;

            GameObject template = Instantiate(source, parent);
            template.SetActive(false);
            _slashTemplates.Add(template);
        }
    }

    /// <summary>
    /// Plays one slash's VFX: the authored object on the first pass, a fresh copy of its template after that.
    /// Each copy cleans itself up the same way the original does.
    /// </summary>
    private void PlaySlashEffect(int index, SlashEntry slash, int pass)
    {
        if (pass == 0)
        {
            // The destroyed-object check has to happen on the MonoBehaviour reference. Casting to the interface
            // first yields a plain reference that ?. sees as live, and PlayAnimation then throws.
            if (slash.effect != null)
                (slash.effect as IAttackAnimator)?.PlayAnimation();
            return;
        }

        GameObject? template = index < _slashTemplates.Count ? _slashTemplates[index] : null;
        if (template == null) return;

        GameObject copy = Instantiate(template, template.transform.parent);
        copy.SetActive(true);
        copy.GetComponent<IAttackAnimator>()?.PlayAnimation();
    }

    /// <summary>
    /// The base ult (overcharge 0) uses the first entry. Beyond the authored list the curve keeps climbing
    /// linearly, so raising the ability's overcharge cap never needs a matching entry here.
    /// </summary>
    private float GetDamageMultiplier(int overchargeLevel)
    {
        if (_damageMultiplierByLevel.Count == 0)
            return 1f;

        int index = Mathf.Max(0, overchargeLevel);
        int lastIndex = _damageMultiplierByLevel.Count - 1;

        if (index <= lastIndex)
            return _damageMultiplierByLevel[index];

        return _damageMultiplierByLevel[lastIndex] + _damagePerExtraLevel * (index - lastIndex);
    }

    private IEnumerator ExecuteFinishingSlash(SlashEntry slash, Vector2 origin, List<EnemyController> enemies, float damageScale, Action onDone)
    {
        if (slash.effect != null)
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
