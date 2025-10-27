using UnityEngine;
using System.Collections.Generic;

public class RoundGenerator : MonoBehaviour
{
    public static RoundGenerator Instance; // Singleton

    [Header("Asset Pools (Drag Assets Here)")]
    public List<ArenaLayoutTemplate> LayoutPool;
    public List<EnemyWaveConfig> WavePool;
    public List<LevelTransitionType> TransitionPool;

    [Header("Wave Generation Settings")]
    public int MinWavesPerLevel = 2;
    public int MaxWavesPerLevel = 4; // Max is inclusive, Unity's Random is exclusive, we'll fix in the code

    void Awake() { Instance = this; }

    public List<LevelBlueprint> GenerateNewRound()
    {
        List<LevelBlueprint> currentRound = new List<LevelBlueprint>();

        // Generate 3 unique levels
        for (int i = 0; i < 3; i++)
        {
            // 1. Randomly select main components
            ArenaLayoutTemplate selectedLayout = LayoutPool[Random.Range(0, LayoutPool.Count)];
            LevelTransitionType selectedTransition = TransitionPool[Random.Range(0, TransitionPool.Count)];

            // 2. Generate dynamic wave list
            List<EnemyWaveConfig> dynamicWaves = new List<EnemyWaveConfig>();
            // Random.Range(int, int) is exclusive for the max, so MaxWavesPerLevel + 1
            int waveCount = Random.Range(MinWavesPerLevel, MaxWavesPerLevel + 1);

            for (int w = 0; w < waveCount; w++)
            {
                EnemyWaveConfig selectedWave = WavePool[Random.Range(0, WavePool.Count)];
                dynamicWaves.Add(selectedWave);
            }

            // 3. Construct the final Level Blueprint
            LevelBlueprint newBlueprint = new LevelBlueprint
            {
                Layout = selectedLayout,
                Waves = dynamicWaves,
                Transition = selectedTransition
            };
            currentRound.Add(newBlueprint);
        }

        return currentRound;
    }
}