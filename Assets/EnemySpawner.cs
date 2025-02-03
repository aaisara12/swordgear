using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] GameObject enemyPrefab;
    [SerializeField] Transform spawnPointTopLeft;
    [SerializeField] Transform spawnPointBottomRight;
    [SerializeField] GameObject player;

    [SerializeField] int[] enemiesPerWave;
    [SerializeField] float minSpawnDelay = 0.5f;
    [SerializeField] float maxSpawnDelay = 2f;
    [SerializeField] float minSpawnDistanceFromPlayer = 3f;

    private int currentWave = 0;
    private int enemiesAlive = 0;

    Vector2 topLeftSpawningBound;
    Vector2 bottomRightSpawningBound;

    void Start()
    {
        topLeftSpawningBound = spawnPointTopLeft.position;
        bottomRightSpawningBound = spawnPointBottomRight.position;
        StartCoroutine(StartWave());
    }

    IEnumerator StartWave()
    {
        while (currentWave < enemiesPerWave.Length)
        {
            yield return StartCoroutine(SpawnWave(currentWave));
            yield return new WaitUntil(() => enemiesAlive <= 0);
            currentWave++;
        }
    }

    IEnumerator SpawnWave(int waveIndex)
    {
        int enemyCount = enemiesPerWave[waveIndex];

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(spawnDelay);
        }
    }



    Vector2 GetRandomLocationBetween(Vector3 position1, Vector3 position2)
    {
        float randomX = Random.Range(position1.x, position2.x);
        float randomY = Random.Range(position1.y, position2.y);
        return new Vector2(randomX, randomY);
    }
    void SpawnEnemy()
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

    void EnemyDefeated()
    {
        enemiesAlive--;
    }
}
