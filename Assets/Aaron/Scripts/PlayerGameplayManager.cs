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
    public bool IsDefeated { get; private set; }

    [SerializeField] private UnityEvent onDefeated = new UnityEvent();
    [SerializeField] private BoolEventChannelSO? defeatVisibilityChannel;
    [SerializeField] private BoolEventChannelSO? simulatedJoysticksVisibilityChannel;
    [SerializeField] private PlayerGameplayPawn? pawnPrefab;
    [SerializeField] private PlayerGameplayInputManager? inputManager;

    [Header("Input")]
    [SerializeField] private TransformEventChannelSO? spawnPawnAtLocationEventChannel;

    [Header("Health")]
    [SerializeField] private float baseMaxHp = 100f;

    private float maxHp = 100f;
    private float currentHp;
    private Coroutine? _regenCoroutine;

    // aisara => HP now persists across nodes within a run; it's only refilled at run start / Rest nodes.
    // This guards the first-ever spawn so the pawn isn't created at 0 HP before a run has been initialized.
    private bool runHealthInitialized;

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
        if (!runHealthInitialized)
        {
            // First spawn of a run (or before RunManager has initialized health): start at full.
            currentHp = maxHp;
            runHealthInitialized = true;
        }
        else
        {
            // Persist HP across nodes; clamp in case max HP changed from augments.
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
        }
        NotifyHealthChanged(0f);
        
        // Avoid stacking handlers when SpawnPawnAtLocation is called without a matching Despawn.
        spawnedPawn.OnRegisterDamage -= HandlePawnRegisterDamage;
        spawnedPawn.OnRegisterDamage += HandlePawnRegisterDamage;
        
        spawnedPawn.transform.position = location.position;

        // Clean combat/movement state so nothing (e.g. a thrown sword) carries over from the previous node.
        spawnedPawn.ResetForNode();

        // TODO: aisara => refactor GameManager so that we don't have to do this - ideally the PlayerGameplayManager would be the source of truth for the player pawn
        GameManager.Instance.player = spawnedPawn.gameObject;

        GameManager.OnPlayerDealtDamage -= HandlePlayerDealtDamage;
        GameManager.OnPlayerDealtDamage += HandlePlayerDealtDamage;
        PlayerStatModifiers.OnStatsChanged -= HandleStatsChanged;
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

    /// <summary>
    /// Initializes health to full for the start of a brand-new run. HP then persists across nodes
    /// until this is called again (new run) or <see cref="FullHeal"/> is used (Rest node).
    /// </summary>
    public void InitializeHealthForNewRun()
    {
        maxHp = baseMaxHp * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MaxHpMultiplier : 1f);
        currentHp = maxHp;
        runHealthInitialized = true;
        IsDefeated = false;
        defeatVisibilityChannel?.RaiseDataChanged(false);
        NotifyHealthChanged(0f);
    }

    /// <summary>
    /// Restores the player to full health (used by Rest nodes).
    /// </summary>
    public void FullHeal()
    {
        float before = currentHp;
        currentHp = maxHp;
        NotifyHealthChanged(currentHp - before);
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
        float previousMaxHp = maxHp;
        float previousHp = currentHp;
        float newMaxHp = baseMaxHp * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MaxHpMultiplier : 1f);
        maxHp = newMaxHp;
        // aisara => Max-HP augments grant the gained capacity as current HP (e.g. +50 max also heals +50).
        // Decreases (trade-off augments) only clamp; never revive a dead player.
        float maxDelta = newMaxHp - previousMaxHp;
        if (maxDelta > 0f && currentHp > 0f)
        {
            currentHp = Mathf.Min(currentHp + maxDelta, maxHp);
        }
        else
        {
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
        }
        NotifyHealthChanged(currentHp - previousHp);
        StopRegen();
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
        // Realtime so regen still ticks while augment UI pauses gameplay (timeScale=0).
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
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
        if (IsDefeated)
        {
            return;
        }

        IsDefeated = true;
        StopRegen();
        simulatedJoysticksVisibilityChannel?.RaiseDataChanged(false);
        inputManager?.DisableGameplayInput();
        spawnedPawn?.DoDefeatAnimation();
        onDefeated.Invoke();
        defeatVisibilityChannel?.RaiseDataChanged(true);
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