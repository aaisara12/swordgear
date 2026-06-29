using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance; // Singleton

    [SerializeField] private GameObject exitPortalPrefab;

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
    private GameObject currentTransition;
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
        if(currentTransition != null)
        {
            Destroy(currentTransition);
        }

        // Instantiate Room Geometry and Transition
        currentRoom = Instantiate(blueprint.Layout.LevelPrefab);
        MinimapController.Instance?.Refresh(currentRoom);

        SpawnPlayerFromRoomMarker();

        // TODO implement transitions later
        // currentTransition = Instantiate(blueprint.Transition.TransitionPrefab, currentRoom.transform);

        if (blueprint.IsShopLevel)
            return;

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
            Debug.Log("Level Complete! Spawning exit portal.");
            SpawnExitPortal();
            if (ComboSystem.Instance != null)
            {
                ComboSystem.Instance.OnLevelFinished();
            }
            OnLevelClear?.Invoke();

            // You should also perform cleanup here to prevent memory leaks!
            CleanupWaveSubscriptions(); // See note below
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

        if (spawnPoints.Length == 0) return;

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
                }
            }
        }
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
        ComboSystem.Instance?.OnLevelFinished();

        OnLevelClear?.Invoke();
        CleanupWaveSubscriptions();
    }

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
        if (exitPortalPrefab == null || currentRoom == null)
        {
            Debug.LogWarning("LevelLoader: cannot spawn exit portal; prefab or room is missing.");
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
        else
        {
            spawnPosition = currentRoom.transform.position + new Vector3(0f, 5f, 0f);
            spawnRotation = Quaternion.identity;
            Debug.LogWarning("LevelLoader: no ExitSpawnPoint in room; using fallback position.");
        }

        spawnedExitPortal = Instantiate(exitPortalPrefab, spawnPosition, spawnRotation, currentRoom.transform);

        LevelExitPortal portal = spawnedExitPortal.GetComponent<LevelExitPortal>();
        if (portal != null)
        {
            portal.OnPlayerEntered += HandleExitPortalEntered;
        }
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

        spawnedExitPortal = null;
    }

    private void OnDestroy()
    {
        ClearExitPortal();
    }
}