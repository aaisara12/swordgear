using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPointTopLeft;
    [SerializeField] private Transform spawnPointBottomRight;
    [SerializeField] private GameObject player;

    [SerializeField] private int[] enemiesPerWave;
    [SerializeField] private float minSpawnDelay = 0.5f;
    [SerializeField] private float maxSpawnDelay = 2f;
    [SerializeField] private float minSpawnDistanceFromPlayer = 3f;

    private int currentWave = 0;
    private int enemiesAlive = 0;

    private Vector2 topLeftSpawningBound;
    private Vector2 bottomRightSpawningBound;

    private void Start()
    {
        topLeftSpawningBound = spawnPointTopLeft.position;
        bottomRightSpawningBound = spawnPointBottomRight.position;
        StartCoroutine(StartWave());
    }

    private IEnumerator StartWave()
    {
        while (currentWave < enemiesPerWave.Length)
        {
            yield return StartCoroutine(SpawnWave(currentWave));
            yield return new WaitUntil(() => enemiesAlive <= 0);
            currentWave++;
        }
    }

    private IEnumerator SpawnWave(int waveIndex)
    {
        int enemyCount = enemiesPerWave[waveIndex];

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private Vector2 GetRandomLocationBetween(Vector3 position1, Vector3 position2)
    {
        float randomX = Random.Range(position1.x, position2.x);
        float randomY = Random.Range(position1.y, position2.y);
        return new Vector2(randomX, randomY);
    }
    
    private void SpawnEnemy()
    {

        Vector2 spawnLocation = GetRandomLocationBetween(topLeftSpawningBound, bottomRightSpawningBound);
        Vector2 playerPosition = player.transform.position;
        while(Vector2.Distance(spawnLocation, playerPosition) < minSpawnDistanceFromPlayer)
        {
            spawnLocation = GetRandomLocationBetween(topLeftSpawningBound, bottomRightSpawningBound);
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnLocation, Quaternion.identity);

        enemiesAlive++;
        enemy.GetComponent<EnemyController>().onDeath += EnemyDefeated;
    }

    private void EnemyDefeated()
    {
        enemiesAlive--;
    }
}
