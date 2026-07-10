using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance; // Singleton

    [SerializeField] private GameObject exitPortalPrefab;
    [Tooltip("Elite scale/stat multipliers + aura reference (Commit 20).")]
    [SerializeField] private ElitePresentation elitePresentation;

    private LevelBlueprint currentBlueprint;
    private int currentWaveIndex = 0;
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    public event Action OnLevelClear;
    public event Action OnExitPortalEntered;

    // aisara => Static so the persistent CombatHUD announcer can subscribe once and survive Arena scene reloads
    // (LevelLoader is re-created per Arena load).
    public static event Action<int> OnWaveIncoming; // 1-based wave number
    public static event Action OnWaveCleared;

    void Awake() { Instance = this; }

    private bool isWaveAdvancing = false;

    private GameObject currentRoom;
    private GameObject spawnedExitPortal;

    public void RefreshMinimapIfLoaded()
    {
        if (currentRoom != null)
        {
            MinimapController.Instance?.Refresh(currentRoom);
        }
    }

    public void LoadLevel(LevelBlueprint blueprint)
    {
        // Reset combo/scoring state for the new level.
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.ResetForNewLevel();
        }

        // Clear existing room/enemies (Implementation depends on your scene management)
        currentBlueprint = blueprint;
        currentWaveIndex = 0;
        if(currentRoom != null)
        {
            ClearExitPortal();
            Destroy(currentRoom);

        }

        // Instantiate room geometry
        currentRoom = Instantiate(blueprint.Layout.LevelPrefab);
        MinimapController.Instance?.Refresh(currentRoom);

        SpawnPlayerFromRoomMarker();

        if (blueprint.IsShopLevel)
        {
            SpawnExitPortal();
            return;
        }

        StartNextWave();
    }

    private void SpawnPlayerFromRoomMarker()
    {
        if (currentRoom == null)
        {
            return;
        }

        PlayerSpawnMarker marker = currentRoom.GetComponentInChildren<PlayerSpawnMarker>();
        if (marker == null)
        {
            Debug.LogWarning("LevelLoader: no PlayerSpawnMarker found in loaded room; player will not be spawned.");
            return;
        }

        if (PlayerGameplayManager.Instance == null)
        {
            Debug.LogError("LevelLoader: PlayerGameplayManager.Instance is null; cannot spawn player.");
            return;
        }

        PlayerGameplayManager.Instance.SpawnPawnAtLocation(marker.transform);
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentBlueprint.Waves.Count)
        {
            CompleteLevel();
            return;
        }

        EnemyWaveConfig wave = currentBlueprint.Waves[currentWaveIndex];
        Debug.Log(wave.name);

        // Announce the incoming wave (1-based) so the HUD can show a banner + cue during the DelayAfterClear breather.
        OnWaveIncoming?.Invoke(currentWaveIndex + 1);

        // Use Invoke to handle the delay defined in the previous wave's data (DelayAfterClear)
        Invoke(nameof(SpawnEnemiesForWave), wave.DelayAfterClear);
    }

    private void SpawnEnemiesForWave()
    {
        Debug.Log("Spawning enemies!");
        isWaveAdvancing = false;
        EnemyWaveConfig wave = currentBlueprint.Waves[currentWaveIndex];
        EnemySpawnPoint[] spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("LevelLoader: no EnemySpawnPoint found; skipping enemy spawn for this wave.");
            TryAdvanceIfWaveEmpty();
            return;
        }

        bool isLastWave = currentWaveIndex >= currentBlueprint.Waves.Count - 1;
        bool eliteAssignedThisWave = false;

        foreach (var enemyCount in wave.Enemies)
        {
            for (int i = 0; i < enemyCount.Count; i++)
            {
                // Pick a random index every time an enemy is created
                int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);

                // Instantiate the enemy prefab at the random spawn point
                GameObject enemyGO = Instantiate(enemyCount.EnemyPrefab, spawnPoints[spawnIndex].transform.position, Quaternion.identity);

                // Get the controller component
                EnemyController enemyController = enemyGO.GetComponent<EnemyController>();

                if (enemyController != null)
                {
                    // Subscribe to the enemy's death event
                    enemyController.OnDeath += EnemyDied;

                    // Add the controller to the list
                    activeEnemies.Add(enemyController);

                    // Interim elite rule (until WaveComposer owns placement): first enemy of the last wave.
                    bool isElite = isLastWave && !eliteAssignedThisWave;
                    if (isElite)
                    {
                        eliteAssignedThisWave = true;
                    }

                    ApplySpawnModifiers(enemyController, enemyCount.EnemyPrefab, isElite);
                    BeginSpawnPresentation(enemyGO, isElite);
                }
            }
        }

        TryAdvanceIfWaveEmpty();
    }

    private void ApplySpawnModifiers(EnemyController enemyController, GameObject enemyPrefab, bool isElite)
    {
        SpawnModifiers modifiers = SpawnModifiers.Identity;

        if (EncounterContext.TryFromCurrent(RunManager.Instance?.Run, out EncounterContext context))
        {
            EnemyCatalog? enemyCatalog = RunManager.Instance?.EnemyCatalog;
            modifiers = enemyCatalog != null
                ? enemyCatalog.ResolveSpawnModifiers(context, enemyPrefab)
                : DifficultyCurve.Evaluate(context);
        }
        else
        {
            // Outside a linear combat step (e.g. editor playtest) — still apply elemental knobs if catalog is present.
            EnemyCatalog? catalog = RunManager.Instance?.EnemyCatalog;
            if (catalog != null && catalog.TryGetByPrefab(enemyPrefab, out EnemyArchetype? archetype)
                && archetype != null && archetype.applyElementKnobsAtSpawn)
            {
                modifiers = SpawnModifiers.FromElement(catalog.GetElementKnobs(archetype.element));
            }
        }

        if (isElite && elitePresentation != null)
        {
            modifiers = SpawnModifiers.Combine(modifiers, elitePresentation.ToSpawnModifiers());
        }

        enemyController.ApplySpawnModifiers(modifiers);
    }

    private static void BeginSpawnPresentation(GameObject enemyGO, bool isElite)
    {
        EnemySpawnPresentation presentation = enemyGO.GetComponent<EnemySpawnPresentation>();
        if (presentation == null)
        {
            // Prefabs should be wired via Henry → Wire Enemy Spawn Presentation; fail soft for legacy.
            return;
        }

        presentation.Begin(isElite);
    }

    private void TryAdvanceIfWaveEmpty()
    {
        if (activeEnemies.Count == 0 && !isWaveAdvancing)
        {
            StartCoroutine(CheckWaveCompletionNextFrame());
        }
    }

    private void CompleteLevel()
    {
        Debug.Log("Level Complete! Spawning exit portal.");
        SpawnExitPortal();
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.OnLevelFinished();
        }

        OnLevelClear?.Invoke();
        CleanupWaveSubscriptions();
    }

    // --- Event Handler ---
    // Called automatically when ANY subscribed enemy invokes its OnDeath event
    public void EnemyDied()
    {
        StartCoroutine(CheckWaveCompletionNextFrame());
    }

    private IEnumerator CheckWaveCompletionNextFrame()
    {
        // Wait until the end of the current frame. This guarantees that 
        // Destroy(gameObject) calls from the enemy controllers have been processed 
        // by the engine and the objects are now truly 'null'.
        yield return new WaitForEndOfFrame();

        // 1. Remove dead enemies
        activeEnemies.RemoveAll(e => e == null);

        // 2. Check if the wave is cleared
        if (activeEnemies.Count == 0 && !isWaveAdvancing)
        {
            isWaveAdvancing = true;
            Debug.Log("WAVE CLEARED! Advancing index.");
            OnWaveCleared?.Invoke();
            currentWaveIndex++;
            StartNextWave();
        }
    }

    public void AdvanceManually()
    {
        CancelInvoke(nameof(SpawnEnemiesForWave));
        CleanupWaveSubscriptions();
        isWaveAdvancing = true;
        currentWaveIndex = currentBlueprint.Waves.Count;
        CompleteLevel();
    }

    /// <summary>Editor/dev helper: kills every live enemy in the current wave.</summary>
    public void DebugClearCurrentWave()
    {
        CancelInvoke(nameof(SpawnEnemiesForWave));

        var enemies = new List<EnemyController>(ActiveEnemyRegistry.All);
        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(99999f);
            }
        }
    }

    /// <summary>Editor/dev helper: skip straight to the exit portal without fighting remaining waves.</summary>
    public void DebugCompleteLevel()
    {
        CancelInvoke(nameof(SpawnEnemiesForWave));
        CleanupWaveSubscriptions();
        isWaveAdvancing = true;
        currentWaveIndex = currentBlueprint.Waves.Count;
        CompleteLevel();
    }

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaveCount => currentBlueprint != null ? currentBlueprint.Waves.Count : 0;
    public bool IsLevelComplete => currentBlueprint != null && currentWaveIndex >= currentBlueprint.Waves.Count;

    private void CleanupWaveSubscriptions()
    {
        // This removes the LevelLoader's EnemyDied method from all enemies' OnDeath events
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= EnemyDied;
            }
        }
        activeEnemies.Clear();
    }

    private void SpawnExitPortal()
    {
        if (exitPortalPrefab == null)
        {
            Debug.LogError("LevelLoader: exitPortalPrefab is not assigned on Level Loader (CoreSystems).");
            return;
        }

        if (currentRoom == null)
        {
            Debug.LogError("LevelLoader: currentRoom is null; cannot spawn exit portal.");
            return;
        }

        ClearExitPortal();

        ExitSpawnPoint exitMarker = currentRoom.GetComponentInChildren<ExitSpawnPoint>();
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (exitMarker != null)
        {
            spawnPosition = exitMarker.transform.position;
            spawnRotation = exitMarker.transform.rotation;
        }
        else if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            Vector3 playerPosition = GameManager.Instance.player.transform.position;
            spawnPosition = playerPosition + new Vector3(0f, 2.5f, 0f);
            spawnRotation = Quaternion.identity;
            Debug.LogWarning("LevelLoader: no ExitSpawnPoint in room; spawning portal near player.");
        }
        else
        {
            spawnPosition = currentRoom.transform.position + new Vector3(0f, 2.5f, 0f);
            spawnRotation = Quaternion.identity;
            Debug.LogWarning("LevelLoader: no ExitSpawnPoint in room; using fallback position.");
        }

        spawnedExitPortal = Instantiate(exitPortalPrefab, spawnPosition, spawnRotation);
        spawnedExitPortal.name = "ExitPortal (Runtime)";

        LevelExitPortal portal = spawnedExitPortal.GetComponent<LevelExitPortal>();
        if (portal != null)
        {
            portal.OnPlayerEntered += HandleExitPortalEntered;
        }
        else
        {
            Debug.LogError("LevelLoader: exit portal prefab is missing LevelExitPortal.");
        }

        Debug.Log($"LevelLoader: exit portal spawned at {spawnPosition}.");
    }

    private void HandleExitPortalEntered()
    {
        Debug.Log("LevelLoader: player entered exit portal.");
        OnExitPortalEntered?.Invoke();
    }

    private void ClearExitPortal()
    {
        if (spawnedExitPortal == null)
        {
            return;
        }

        LevelExitPortal portal = spawnedExitPortal.GetComponent<LevelExitPortal>();
        if (portal != null)
        {
            portal.OnPlayerEntered -= HandleExitPortalEntered;
        }

        Destroy(spawnedExitPortal);
        spawnedExitPortal = null;
    }

    private void OnDestroy()
    {
        ClearExitPortal();
    }
}