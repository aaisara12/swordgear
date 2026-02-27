#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Maintains and drives the player's gameplay state
/// </summary>
// aisara => We use a MonoBehaviour here because it needs to track state at runtime, and we want to be able to inject
// dependencies via the inspector
public class PlayerGameplayManager : MonoBehaviour
{
    [SerializeField] private UnityEvent onDefeated = new UnityEvent();
    [SerializeField] private PlayerGameplayPawn? pawnPrefab;
    [SerializeField] private PlayerGameplayInputManager? inputManager;

    [Header("Input")]
    [SerializeField] private TransformEventChannelSO? spawnPawnAtLocationEventChannel;

    [Header("Health")]
    [SerializeField] private float baseMaxHp = 10000f;

    private float maxHp = 10000f;
    private float currentHp;
    private Coroutine? _regenCoroutine;
    
    private PlayerGameplayPawn? spawnedPawn;
    
    // aisara => Intentionally don't give reference to the spawned pawn because the caller shouldn't need to know that information
    public void SpawnPawnAtLocation(Transform location)
    {
        if (pawnPrefab == null || inputManager == null || spawnedPawn == null)
        {
            Debug.LogError("PlayerGameplayManager: Cannot spawn pawn, missing references");
            return;
        }
        
        spawnedPawn.gameObject.SetActive(true);
        spawnedPawn.DoSpawnAnimation();
        inputManager.LinkPawn(spawnedPawn);

        maxHp = baseMaxHp * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MaxHpMultiplier : 1f);
        currentHp = maxHp;
        
        spawnedPawn.OnRegisterDamage += HandlePawnRegisterDamage;
        
        spawnedPawn.transform.position = location.position;

        // TODO: aisara => refactor GameManager so that we don't have to do this - ideally the PlayerGameplayManager would be the source of truth for the player pawn
        GameManager.Instance.player = spawnedPawn.gameObject;

        GameManager.OnPlayerDealtDamage += HandlePlayerDealtDamage;
        PlayerStatModifiers.OnStatsChanged += HandleStatsChanged;
        StartRegenIfNeeded();
    }

    public void DespawnPawn()
    {
        if (inputManager == null || spawnedPawn == null)
        {
            Debug.LogError("PlayerGameplayManager: Cannot despawn pawn, missing references");
            return;
        }
        
        spawnedPawn.OnRegisterDamage -= HandlePawnRegisterDamage;
        inputManager.UnlinkCurrentPawn();
        spawnedPawn.gameObject.SetActive(false);

        GameManager.OnPlayerDealtDamage -= HandlePlayerDealtDamage;
        PlayerStatModifiers.OnStatsChanged -= HandleStatsChanged;
        StopRegen();

        // TODO: aisara => refactor GameManager so that we don't have to do this - ideally the PlayerGameplayManager would be the source of truth for the player pawn
        GameManager.Instance.player = null;
    }

    public void Heal(float amount)
    {
        if (currentHp <= 0) return;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }

    private void HandlePlayerDealtDamage(float damage)
    {
        if (PlayerStatModifiers.Instance == null) return;
        float lifesteal = damage * (PlayerStatModifiers.Instance.LifestealPercent / 100f);
        if (lifesteal > 0f) Heal(lifesteal);
    }

    private void StartRegenIfNeeded()
    {
        if (_regenCoroutine != null) return;
        if (PlayerStatModifiers.Instance == null || PlayerStatModifiers.Instance.RegenPercentPerSecond <= 0f)
            return;
        _regenCoroutine = StartCoroutine(RegenTick());
    }

    private void HandleStatsChanged()
    {
        if (spawnedPawn == null || !spawnedPawn.gameObject.activeInHierarchy) return;
        float newMaxHp = baseMaxHp * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MaxHpMultiplier : 1f);
        maxHp = newMaxHp;
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
        StartRegenIfNeeded();
    }

    private void StopRegen()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }
    }

    private IEnumerator RegenTick()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            if (currentHp <= 0 || maxHp <= 0) continue;
            if (PlayerStatModifiers.Instance == null) continue;
            float regen = maxHp * (PlayerStatModifiers.Instance.RegenPercentPerSecond / 100f);
            if (regen > 0f) Heal(regen);
        }
    }

    private void Awake()
    {
        if (pawnPrefab == null)
        {
            Debug.LogError("PlayerGameplayManager: pawn prefab is null");
            return;
        }

        if (inputManager == null)
        {
            Debug.LogError("PlayerGameplayManager: input manager is null");
            return;
        }

        if (spawnPawnAtLocationEventChannel == null)
        {
            Debug.LogError("PlayerGameplayManager: spawnPawnAtLocationEventChannel is null");
            return;
        }
        
        spawnedPawn = Instantiate(pawnPrefab);
        spawnedPawn.gameObject.SetActive(false);
        spawnPawnAtLocationEventChannel.OnDataChanged += HandleSpawnPawnAtLocationEvent;
    }

    private void HandleSpawnPawnAtLocationEvent(Transform location)
    {
        SpawnPawnAtLocation(location);
    }

    private void TakeDamage(float damage)
    {
        if (currentHp <= 0) return;
        currentHp -= damage;
        if (currentHp <= 0f)
        {
            Defeat();
        }
    }

    private void Defeat()
    {
        if (spawnedPawn != null)
        {
            spawnedPawn.DoDefeatAnimation();
        }
        
        onDefeated.Invoke();
    }
    
    private void HandlePawnRegisterDamage(float damage)
    {
        TakeDamage(damage);
    }

    private void OnDestroy()
    {
        if (spawnPawnAtLocationEventChannel != null)
        {
            spawnPawnAtLocationEventChannel.OnDataChanged -= HandleSpawnPawnAtLocationEvent;
        }

        if (spawnedPawn != null)
        {
            DespawnPawn();
            Destroy(spawnedPawn);
        }
    }
}