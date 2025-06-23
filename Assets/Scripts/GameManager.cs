#nullable enable

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager? Instance;
    public GameObject? player; // Assign this in the Inspector
    [SerializeField] GameObject? damageUI;

    public float baseDamage = 10;
    public float currentDamage = 10;
    public float rangedMultiplier = 1.2f;
    public Element currentElement = Element.Physical;

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayDamageUI(Vector3 position, float amt)
    {
        if (!damageUI) return;

        GameObject ui = Instantiate(damageUI, position, Quaternion.identity);

        Color color = Color.white;
        switch (currentElement)
        {
            case Element.Fire:
                color = Color.red;
                break;
            case Element.Ice:
                color = Color.cyan;
                break;

        }

        ui.GetComponent<DamageUI>().ShowNumber(amt, color);
    }

    public float CalculateDamage(Element enemyElement, Element swordElement, float swordDamage)
    {
        // add multipliers in the future
        float finalDamage = swordDamage;
        //// Apply all embue multipliers
        //finalDamage *= embue.damageMultiplier;
        // apply elemental multiplier
        finalDamage *= ElementalInteractions.interactionMatrix[swordElement][enemyElement];
        return finalDamage;
    }
}