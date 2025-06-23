using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject player; // Assign this in the Inspector
    PlayerController playerController;
    [SerializeField] GameObject damageUI;

    public float baseDamage = 10;
    public float currentDamage = 10;
    public float rangedMultiplier = 1.2f;
    private Element _currentElement = Element.Physical;
    public Element currentElement
    {
        get { return _currentElement; }
        set
        {
            _currentElement = value;
            playerController.SetElement(value);
            SwordProjectile.Instance.CurrentBuff = value;
        }
    }


    // A private field to store the reference to the currently running empowerment coroutine
    private Coroutine _currentEmpowermentRoutine;

    private void Awake()
    {
        Instance = this;
        playerController = player.GetComponent<PlayerController>();
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

    // Call this method from the Embue script
    public void ApplyEmpowerment(Element newElement, float newDamageMultiplier, float duration)
    {
        // 1. If an empowerment coroutine is already running on the sword, stop it.
        if (_currentEmpowermentRoutine != null)
        {
            Debug.Log("Stopping previous routine");
            StopCoroutine(_currentEmpowermentRoutine);
        }

        // 2. Start the new empowerment coroutine and store its reference.
        // This allows us to stop it later using StopCoroutine(_currentEmpowermentRoutine).
        _currentEmpowermentRoutine = StartCoroutine(EmpowermentRoutine(newElement, newDamageMultiplier, duration));
        Debug.Log($"Starting new empowerment routine for {newElement} on {gameObject.name}.");
    }

    private IEnumerator EmpowermentRoutine(Element elementToApply, float damageMultiplierToApply, float duration)
    {
        // --- Apply Empowerment ---
        currentDamage = baseDamage * damageMultiplierToApply;
        currentElement = elementToApply;

        Debug.Log($"Sword {gameObject.name} is now {elementToApply}. Current Damage: {currentDamage}");

        // --- Wait for Duration ---
        yield return new WaitForSeconds(duration);

        // --- Undo Empowerment ---
        Debug.Log($"Empower effect for {elementToApply} on {gameObject.name} finished.");

        currentDamage = baseDamage;
        currentElement = Element.Physical; // Reset to default/physical

        // Clear the coroutine reference as it has now finished executing.
        _currentEmpowermentRoutine = null;
    }
}