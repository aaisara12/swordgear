#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null!;
    public GameObject? player; // Assign this in the Inspector
    public PlayerController? playerController;

    /// <summary> Fired when the player deals damage to an enemy (for lifesteal etc). </summary>
    public static event Action<float>? OnPlayerDealtDamage;

    /// <summary> Call this when the player deals damage; raises OnPlayerDealtDamage for subscribers. </summary>
    public static void NotifyPlayerDealtDamage(float damage) => OnPlayerDealtDamage?.Invoke(damage);

    [Header("Damage visual references")]
    [SerializeField] GameObject? damageUI;

    [Header("Damage settings")]

    public float baseDamage = 10;
    public float currentDamage = 10;
    public float rangedMultiplier = 1.2f;

    public float GetEffectiveBaseDamage()
    {
        float multiplier = PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.DamageMultiplier : 1f;
        return baseDamage * multiplier;
    }

    public float GetEffectiveRangedMultiplier()
    {
        float bonus = PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.RangedDamageMultiplierBonus : 0f;
        return rangedMultiplier + bonus;
    }
    private Element _currentElement = Element.Physical;
    public Element currentElement
    {
        get { return _currentElement; }
        set
        {
            _currentElement = value;

            // Old API (to be replaced)
            //playerController.SetElement(value);
            //SwordProjectile.Instance.CurrentBuff = value;

            // New API
            if (ElementManager.Instance)
                ElementManager.Instance.SetActiveElement(value);
        }
    }


    private void Awake()
    {
        Instance = this;
        playerController = player != null ? player.GetComponent<PlayerController>() : null;
    }

    private void Start()
    {
        // For enemy effect handling
        StartCoroutine(EffectTickLoop());
        currentElement = Element.Physical;
        currentDamage = GetEffectiveBaseDamage();

        AudioSystem.PlayLoop(AudioSystem.Sound.BGM);  // TODO: put this logic in a place where we can control the background music better (fading in different tracks, etc.)
    }

    public void DisplayDamageUI(Vector3 position, float amt, Element? sourceElement = null)
    {
        if (!damageUI) return;

        GameObject ui = PrefabPool.Instance!.Spawn(damageUI, position, Quaternion.identity);

        // Colour by the DAMAGE's source element, not the player's current element, so DoT ticks and
        // off-element hits (burn/chill/static) read with the correct colour; fall back to the current
        // element when no source is given.
        ui.GetComponent<DamageUI>().ShowNumber(amt, sourceElement ?? currentElement);
    }

    /// <summary>
    /// Attunement augment: an attack whose element matches the target's element is fully nullified. Callers
    /// should skip the whole hit (no damage, no VFX/SFX/hit-reaction, projectile phases through) when true —
    /// checking this instead of relying on 0 damage, so a matching hit reads as "passed through", not "hit".
    /// </summary>
    public bool IsAttunementBlocked(Element attackerElement, Element defenderElement)
    {
        return attackerElement == defenderElement
            && ElementManager.Instance != null
            && ElementManager.Instance.HasUpgrade(UpgradeType.Nonelemental_Attunement);
    }

    public float CalculateDamage(Element defenderElement, Element attackerElement, float baseDamage)
    {
        // Safety net: even if a caller forgets the IsAttunementBlocked pre-check, a matching hit deals 0.
        if (IsAttunementBlocked(attackerElement, defenderElement))
        {
            return 0f;
        }

        // add multipliers in the future
        float finalDamage = baseDamage;
        //// Apply all embue multipliers
        //finalDamage *= embue.damageMultiplier;
        // apply elemental multiplier
        // GetMultiplier(attacker, defender) returns the damage multiplier, defaulting to neutral for
        // any pair the matrix doesn't cover (Earth/Dark/Light are all neutral for now).
        finalDamage *= ElementalInteractions.GetMultiplier(attackerElement, defenderElement);
        return finalDamage;
    }

    /// <summary>
    /// Imbues the player with an element. An imbue lasts until the next one, or until a new arena clears it
    /// (<see cref="ClearEmpowerment"/>): it doesn't time out.
    /// </summary>
    public void ApplyEmpowerment(Element newElement, float newDamageMultiplier)
    {
        currentDamage = GetEffectiveBaseDamage() * newDamageMultiplier;
        currentElement = newElement;
    }

    /// <summary> Back to no element (Physical), as each arena starts. Does nothing if there's no imbue. </summary>
    public void ClearEmpowerment()
    {
        if (currentElement == Element.Physical)
        {
            return;
        }

        currentDamage = GetEffectiveBaseDamage();
        currentElement = Element.Physical;
    }

    #region Enemy Effects

    public enum EnemyEffect
    {
        Burn,
        Static,
        Chill,
        Buffetted
    }

    public interface IEnemyEffect
    {
        public EnemyEffect getEffect();
        public void EffectBegin(EnemyController enemy);
        public void EffectTick(EnemyController enemy);
        public void EffectEnd(EnemyController enemy);
    }

    [SerializeField] public Dictionary<EnemyEffect, IEnemyEffect> enemyEffect = new();

    private Dictionary<EnemyController, List<(IEnemyEffect effect, int duration)>> _activeEffects = new();

    public void AddEffect(EnemyController enemy, EnemyEffect effectName, int duration)
    {
        IEnemyEffect effect = enemyEffect[effectName];

        if (!_activeEffects.TryGetValue(enemy, out var effectList))
        {
            effectList = new List<(IEnemyEffect, int)>();
            _activeEffects[enemy] = effectList;
        }

        var index = effectList.FindIndex(pair => pair.Item1.getEffect() == effect.getEffect());

        if (index != -1)
        {
            // If effect already exists, refresh the duration
            effectList[index] = (effectList[index].Item1, duration);
        }
        else
        {
            // New effect
            effectList.Add((effect, duration));
            effect.EffectBegin(enemy);
        }
    }

    public bool CheckEnemyEffect(EnemyController enemy, EnemyEffect effectName)
    {
        IEnemyEffect effect = enemyEffect[effectName];

        if (!_activeEffects.TryGetValue(enemy, out var effectList))
        {
            effectList = new List<(IEnemyEffect, int)>();
            _activeEffects[enemy] = effectList;
        }

        var index = effectList.FindIndex(pair => pair.Item1.getEffect() == effect.getEffect());

        return index != -1;
    }

    private IEnumerator EffectTickLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (true)
        {
            yield return wait;

            var enemiesToRemove = new List<EnemyController>();

            foreach (var kvp in _activeEffects)
            {
                var enemy = kvp.Key;
                var effectList = kvp.Value;

                if (!enemy)
                {
                    enemiesToRemove.Add(kvp.Key);
                    continue;
                }

                for (int i = effectList.Count - 1; i >= 0; i--)
                {
                    var (effect, duration) = effectList[i];
                    effect.EffectTick(enemy);
                    duration--;

                    if (duration <= 0)
                    {
                        effect.EffectEnd(enemy);
                        effectList.RemoveAt(i);
                    }
                    else
                    {
                        effectList[i] = (effect, duration);
                    }
                }

                if (effectList.Count == 0)
                {
                    enemiesToRemove.Add(enemy);
                }
            }

            foreach (var enemy in enemiesToRemove)
            {
                _activeEffects.Remove(enemy);
            }
        }
    }


    #endregion
}