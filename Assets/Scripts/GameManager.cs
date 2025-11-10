using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject player; // Assign this in the Inspector
    PlayerController playerController;
    [SerializeField] GameObject damageUI;

    public float baseDamage = 10;
    public float currentDamage = 10;
    public float rangedMultiplier = 1.2f;
    private Element _currentElement = Element.Physical;
    public Element currentElement
    {
        get { return _currentElement; }
        set
        {
            _currentElement = value;

            // Old API (to be replaced)
            playerController.SetElement(value);
            SwordProjectile.Instance.CurrentBuff = value;

            // New API
            if (ElementManager.Instance)
                ElementManager.Instance.SetActiveElement(value);
        }
    }


    // A private field to store the reference to the currently running empowerment coroutine
    private Coroutine _currentEmpowermentRoutine;

    private void Awake()
    {
        Instance = this;
        playerController = player.GetComponent<PlayerController>();
    }

    private void Start()
    {
        // For enemy effect handling
        StartCoroutine(EffectTickLoop());
    }

    public void DisplayDamageUI(Vector3 position, float amt)
    {
        if (!damageUI) return;

        GameObject ui = Instantiate(damageUI, position, Quaternion.identity);

        Color color = Color.white;
        switch (currentElement)
        {
            case Element.Fire:
                color = Color.red;
                break;
            case Element.Ice:
                color = Color.cyan;
                break;

        }

        ui.GetComponent<DamageUI>().ShowNumber(amt, color);
    }

    public float CalculateDamage(Element enemyElement, Element swordElement, float swordDamage)
    {
        // add multipliers in the future
        float finalDamage = swordDamage;
        //// Apply all embue multipliers
        //finalDamage *= embue.damageMultiplier;
        // apply elemental multiplier
        finalDamage *= ElementalInteractions.interactionMatrix[swordElement][enemyElement];
        return finalDamage;
    }

    // Call this method from the Embue script
    public void ApplyEmpowerment(Element newElement, float newDamageMultiplier, float duration)
    {
        // 1. If an empowerment coroutine is already running on the sword, stop it.
        if (_currentEmpowermentRoutine != null)
        {
            Debug.Log("Stopping previous routine");
            StopCoroutine(_currentEmpowermentRoutine);
        }

        // 2. Start the new empowerment coroutine and store its reference.
        // This allows us to stop it later using StopCoroutine(_currentEmpowermentRoutine).
        _currentEmpowermentRoutine = StartCoroutine(EmpowermentRoutine(newElement, newDamageMultiplier, duration));
        Debug.Log($"Starting new empowerment routine for {newElement} on {gameObject.name}.");
    }

    private IEnumerator EmpowermentRoutine(Element elementToApply, float damageMultiplierToApply, float duration)
    {
        // --- Apply Empowerment ---
        currentDamage = baseDamage * damageMultiplierToApply;
        currentElement = elementToApply;

        Debug.Log($"Sword {gameObject.name} is now {elementToApply}. Current Damage: {currentDamage}");

        // --- Wait for Duration ---
        yield return new WaitForSeconds(duration);

        // --- Undo Empowerment ---
        Debug.Log($"Empower effect for {elementToApply} on {gameObject.name} finished.");

        currentDamage = baseDamage;
        currentElement = Element.Physical; // Reset to default/physical

        // Clear the coroutine reference as it has now finished executing.
        _currentEmpowermentRoutine = null;
    }

    #region Enemy Effects

    public enum EnemyEffect
    {
        Burn,
        Static
    }

    public interface IEnemyEffect
    {
        public EnemyEffect getEffect();
        public void EffectBegin(EnemyController enemy);
        public void EffectTick(EnemyController enemy);
        public void EffectEnd(EnemyController enemy);
    }

    public Dictionary<EnemyEffect, IEnemyEffect> enemyEffect = new();

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
                    enemiesToRemove.Add(enemy);
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