using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance; // Singleton
    private LevelBlueprint currentBlueprint;
    private int currentWaveIndex = 0;
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    public event Action OnLevelClear;
    void Awake() { Instance = this; }

    private bool isWaveAdvancing = false;

    private GameObject currentRoom;
    private GameObject currentTransition;

    public void LoadLevel(LevelBlueprint blueprint)
    {
        // Clear existing room/enemies (Implementation depends on your scene management)
        currentBlueprint = blueprint;
        currentWaveIndex = 0;
        if(currentRoom != null)
        {
            Destroy(currentRoom);

        }
        if(currentTransition != null)
        {
            Destroy(currentTransition);
        }

        // Instantiate Room Geometry and Transition
        currentRoom = Instantiate(blueprint.Layout.LevelPrefab);

        // TODO implement transitions later
        // currentTransition = Instantiate(blueprint.Transition.TransitionPrefab, currentRoom.transform);
        // Start the first wave
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentBlueprint.Waves.Count)
        {
            Debug.Log("Level Complete! Activating transition.");
            // TODO: Activate the transition object/mechanic here
            OnLevelClear?.Invoke();

            // You should also perform cleanup here to prevent memory leaks!
            CleanupWaveSubscriptions(); // See note below
            return;
        }

        EnemyWaveConfig wave = currentBlueprint.Waves[currentWaveIndex];
        Debug.Log(wave.name);

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

        int spawnIndex = 0;
        foreach (var enemyCount in wave.Enemies)
        {
            for (int i = 0; i < enemyCount.Count; i++)
            {
                // Instantiate the enemy prefab
                GameObject enemyGO = Instantiate(enemyCount.EnemyPrefab, spawnPoints[spawnIndex].transform.position, Quaternion.identity);

                // Get the controller component
                EnemyController enemyController = enemyGO.GetComponent<EnemyController>();

                if (enemyController != null)
                {
                    // CRUCIAL STEP: Subscribe to the enemy's death event
                    enemyController.OnDeath += EnemyDied;

                    // Add the controller to the list
                    activeEnemies.Add(enemyController);
                }

                spawnIndex = (spawnIndex + 1) % spawnPoints.Length;
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
            currentWaveIndex++;
            StartNextWave();
        }
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
}