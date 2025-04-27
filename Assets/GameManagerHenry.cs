using UnityEngine;

public class GameManagerHenry : MonoBehaviour
{
    public static GameManagerHenry Instance { get; private set; }

    [SerializeField] public int levelsPerRound = 3;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void EnterShop()
    {
        Debug.Log("Entering Shop...");
    }

    public void StartRound()
    {
        RoundController.Instance.StartNewRound();
    }

    public void Start()
    {
        StartRound();
    }
}
