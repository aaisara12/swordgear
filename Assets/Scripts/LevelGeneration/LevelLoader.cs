using UnityEngine;
using System.Collections.Generic;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance; // Singleton
    private LevelBlueprint currentBlueprint;
    private int currentWaveIndex = 0;
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    void Awake() { Instance = this; }

    public void LoadLevel(LevelBlueprint blueprint)
    {
        // Clear existing room/enemies (Implementation depends on your scene management)
        // ...

        currentBlueprint = blueprint;
        currentWaveIndex = 0;

        // Instantiate Room Geometry and Transition
        GameObject room = Instantiate(blueprint.Layout.LevelPrefab);
        Instantiate(blueprint.Transition.TransitionPrefab, room.transform);

        // Start the first wave
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentBlueprint.Waves.Count)
        {
            Debug.Log("Level Complete! Activating transition.");
            // TODO: Activate the transition object/mechanic here
            return;
        }

        EnemyWaveConfig wave = currentBlueprint.Waves[currentWaveIndex];

        // Use Invoke to handle the delay defined in the previous wave's data (DelayAfterClear)
        Invoke(nameof(SpawnEnemiesForWave), wave.DelayAfterClear);
    }

    private void SpawnEnemiesForWave()
    {
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

    // Called automatically when ANY subscribed enemy invokes its OnDeath event
    public void EnemyDied()
    {
        activeEnemies.RemoveAll(e => e == null); // remove dead enemies

        // Check if the wave is cleared
        if (activeEnemies.Count == 0)
        {
            currentWaveIndex++;
            StartNextWave();
        }
    }
}