using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaveConfig", menuName = "Scriptable Objects/EnemyWaveConfig")]
    public class EnemyWaveConfig : ScriptableObject
    {
        // The enemies to be spawned in the wave
        [Header("Enemies")]
        public List<EnemyGroup> Enemies;

    public float DelayAfterClear;

}

    // Ensure EnemyCount struct is also defined here or in a common utility script
    [System.Serializable]
    public struct EnemyGroup
    {
        public GameObject EnemyPrefab;
        public int Count;
    }