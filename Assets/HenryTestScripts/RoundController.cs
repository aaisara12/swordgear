using UnityEngine;
using System.Collections.Generic;
public class RoundController : MonoBehaviour
{
    public static RoundController Instance { get; private set; }

    private int currentRound = 0;
    private int currentLevel = 0;
    private List<Level> levels = new List<Level>();

    [SerializeField] private GameObject? enemyPrefab;
    [SerializeField] private GameObject? arenaPrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartNewRound()
    {
        currentRound++;
        currentLevel = 0;
        Debug.Log($"Round {currentRound} started.");

        StartLevel(currentLevel);
    }

    private void StartLevel(int levelNumber)
    {
        Level level = levels[levelNumber];

        // instantiate the arena
        Instantiate(level.arena);

        // temp hacky solution
        EnemySpawner.Instance.StartLevel();

    }

    // ALL STUFF BELOW IS FUTURE

    public void GenerateRound()
    {
        levels.Clear(); // Clear previous round data
        for (int i = 0; i < GameManagerHenry.Instance.levelsPerRound; i++)
        {
            Level newLevel = new Level
            {
                arena = GenerateArena(),
                waves = GenerateWaves()
            };
            levels.Add(newLevel);
        }
        Debug.Log("New round generated");
    }

    private GameObject GenerateArena()
    {
        Debug.Log("Generating Arena...");
        return arenaPrefab;
    }

    private List<Wave> GenerateWaves()
    {
        List<Wave> waves = new List<Wave>();
        int waveCount = 3;

        for (int i = 0; i < waveCount; i++)
        {
            Wave newWave = new Wave
            {
                enemy = new List<GameObject> { enemyPrefab }
            };
            waves.Add(newWave);
        }

        Debug.Log($"Generated {waveCount} waves for Level {currentLevel}.");
        return waves;
    }
}

struct Wave
{
    public List<GameObject> enemy;
}

struct Level
{
    public GameObject arena;
    public List<Wave> waves;
}
