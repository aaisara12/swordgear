using UnityEngine;

[CreateAssetMenu(fileName = "ArenaLayoutTemplate", menuName = "Scriptable Objects/ArenaLayoutTemplate")]
public class ArenaLayoutTemplate : ScriptableObject
{
    // note: expect EnemySpawnPoint components inside of this
    [Header("Arena Layout")] public GameObject LevelPrefab;
    // TODO: create preview diagrams for each level prefab
    // wonder if this is possible to do programatically
    public Texture2D PreviewDiagram;
}
