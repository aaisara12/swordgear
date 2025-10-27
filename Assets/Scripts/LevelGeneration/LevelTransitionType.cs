using UnityEngine;

[CreateAssetMenu(fileName = "LevelTransitionType", menuName = "Scriptable Objects/LevelTransitionType")]
public class LevelTransitionType : ScriptableObject
{
    // teleporter, door, etc.
    [Header("Transition")] public GameObject TransitionPrefab;
}
