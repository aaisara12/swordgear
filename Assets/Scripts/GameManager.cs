using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject player; // Assign this in the Inspector

    private void Awake()
    {
        Instance = this;
    }
}