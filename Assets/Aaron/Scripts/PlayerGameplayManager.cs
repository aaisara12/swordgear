#nullable enable

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public readonly struct PlayerHealthSnapshot
{
    public float Current { get; }
    public float Max { get; }
    public float Delta { get; }

    public PlayerHealthSnapshot(float current, float max, float delta)
    {
        Current = current;
        Max = max;
        Delta = delta;
    }
}

/// <summary>
/// Maintains and drives the player's gameplay state
/// </summary>
// aisara => We use a MonoBehaviour here because it needs to track state at runtime, and we want to be able to inject
// dependencies via the inspector
public class PlayerGameplayManager : MonoBehaviour
{
    public static PlayerGameplayManager? Instance { get; private set; }

    public static event Action<PlayerHealthSnapshot>? OnHealthChanged;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsPawnActive => spawnedPawn != null && spawnedPawn.gameObject.activeInHierarchy;

    [SerializeField] private UnityEvent onDefeated = new UnityEvent();
    [SerializeField] private PlayerGameplayPawn? pawnPrefab;
    [SerializeField] private PlayerGameplayInputManager? inputManager;

    [Header("Input")]
    [SerializeField] private TransformEventChannelSO? spawnPawnAtLocationEventChannel;

    [Header("Health")]
    [SerializeField] private float baseMaxHp = 100f;

    private float maxHp = 100f;
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
        NotifyHealthChanged(0f);
        
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
        float before = currentHp;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        if (!Mathf.Approximately(before, currentHp))
        {
            NotifyHealthChanged(currentHp - before);
        }
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
        NotifyHealthChanged(0f);
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
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

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
        currentHp = Mathf.Max(0f, currentHp - damage);
        NotifyHealthChanged(-damage);
        if (currentHp <= 0f)
        {
            Defeat();
        }
    }

    private void NotifyHealthChanged(float delta)
    {
        OnHealthChanged?.Invoke(new PlayerHealthSnapshot(currentHp, maxHp, delta));
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
        if (Instance == this)
        {
            Instance = null;
        }

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