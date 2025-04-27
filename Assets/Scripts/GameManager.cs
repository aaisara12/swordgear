#nullable enable

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager? Instance;
    public GameObject? player; // Assign this in the Inspector
    [SerializeField] GameObject? damageUI; 

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayDamageUI(Vector3 position, float amt)
    {
        if (!damageUI) return;

        GameObject ui = Instantiate(damageUI, position, Quaternion.identity);
        ui.GetComponent<DamageUI>().ShowNumber(amt);
    }
}