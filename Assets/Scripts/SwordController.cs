#nullable enable

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SwordController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float angleModifier;
    [SerializeField] private Transform? gear;
    [SerializeField] private Transform? player;
    
    private bool canMove;
    private Rigidbody2D? rb;
    private float damage = 1f;
    private Vector3 lastPlayerPosition;
    
    public void MoveSword(Vector2 playerPosition)
    {
        rb.ThrowIfNull(nameof(rb));

        if(canMove == false)
        {
            return;
        }

        DetachFromGear();

        Vector2 swordPosition = transform.position;
        Vector2 pullDirection = (playerPosition - swordPosition).normalized;
        float angle = Mathf.Atan2(pullDirection.y, pullDirection.x) * Mathf.Rad2Deg + angleModifier;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        rb.linearVelocity = pullDirection * speed;
    }

    public float ApplyDamage(Element enemyElement)
    {
        // add multipliers in the future
        float finalDamage = damage;
        // Apply all embue multipliers
        foreach (Embue embue in currentEmbues)
        {
            finalDamage *= embue.damageMultiplier;
        }

        // apply elemental multiplier
        finalDamage *= ElementalInteractions.interactionMatrix[mostRecentElement][enemyElement];
        return finalDamage;
    }
    
    private void Start()
    {
        player.ThrowIfNull(nameof(player));
        
        lastPlayerPosition = player.position;
        canMove = true;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (transform.parent != null)
        {
            PointAtPlayer();
            MoveWithPlayer();
        }
    }

    private void MoveWithPlayer()
    {
        player.ThrowIfNull(nameof(player));
        
        transform.position += player.position - lastPlayerPosition;
        lastPlayerPosition = player.position;
    }

    private void PointAtPlayer()
    {
        player.ThrowIfNull(nameof(player));
        
        Vector2 swordPosition = transform.position;
        Vector2 playerPosition = player.position;
        Vector2 pullDirection = (playerPosition - swordPosition).normalized;
        float angle = Mathf.Atan2(pullDirection.y, pullDirection.x) * Mathf.Rad2Deg + angleModifier;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void AttachToGear()
    {
        player.ThrowIfNull(nameof(player));
        
        if (gear != null)
        {
            transform.parent = gear;
            lastPlayerPosition = player.position;
            canMove = true;
        }
    }

    private void DetachFromGear()
    {
        if (gear != null)
        {
            transform.parent = null;

            canMove = false;
        }
    }

    private List<Embue> currentEmbues = new List<Embue>();
    private Element mostRecentElement = Element.Physical;
    private Dictionary<Embue, Coroutine> activeEmbueTimers = new Dictionary<Embue, Coroutine>();

    // --- NEW: Method to Apply Embue Effects ---
    public void ApplyEmbue(Embue embueSource)
    {

        Embue existingEmbue = currentEmbues.Find(embue => embue.GetType() == embueSource.GetType());

        // if list of embues already contains current type, just reset timer
        if (existingEmbue != null && activeEmbueTimers.TryGetValue(existingEmbue, out Coroutine existingCoroutine))
        {
            StopCoroutine(existingCoroutine);
            activeEmbueTimers[existingEmbue] = StartCoroutine(EmbueTimer(existingEmbue));
            Debug.Log($"Resetting timer for existing {embueSource.GetType().Name}");
            return; // Exit, as we've reset the timer
        }

        if (embueSource is FireEmbue fireEmbue) // More robust check
        {
            mostRecentElement = Element.Fire;
            Debug.Log("Applying fire embue"); // Diagnostic log

            // handle burn damage later
/*            currentBonusDamage = fireEmbue.fireDamage;
            currentDamageType = DamageType.Fire;
            canApplyBurn = true;
            burnDamagePerSecond = fireEmbue.burnDPS;
            burnDurationToApply = fireEmbue.burnDuration;*/

/*            if (fireEmbue.fireEffectPrefab != null)
            {
                currentVisualEffectInstance = Instantiate(fireEmbue.fireEffectPrefab, transform);
                currentVisualEffectInstance.transform.localPosition = Vector3.zero;
            }*/
        }

        currentEmbues.Add(embueSource);
        activeEmbueTimers.Add(embueSource, StartCoroutine(EmbueTimer(embueSource)));

    }
    private IEnumerator EmbueTimer(Embue embue)
    {
        yield return new WaitForSeconds(embue.effectDuration); // Wait for the duration

        Debug.Log("Embue expired: " + embue.GetType().Name); // Debug message

        // Remove the embue from the list
        currentEmbues.Remove(embue);
        if(currentEmbues.Count == 0)
        {
            mostRecentElement = Element.Physical;
        }

        // Reset the effects that were applied by this embue.  IMPORTANT!
/*        if (embue is FireEmbue fireEmbue)
        {
            currentBonusDamage -= fireEmbue.fireDamage; // Remove the bonus
            if (currentVisualEffectInstance != null)
            {
                Destroy(currentVisualEffectInstance); // Destroy the visual effect
            }
            // Consider how you want to handle burn.  Do you stop it immediately?
            canApplyBurn = false; // Simplest: Stop burn.  You might have a system.
            burnDamagePerSecond = 0f;
            burnDurationToApply = 0f;
        }*/
        // Add logic for other embue types here
        // else if (embue is WaterEmbue waterEmbue) { ... }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb != null)
        {
            if (collision.transform.gameObject.layer == 11)
            { // gear layer
                rb.linearVelocity = Vector2.zero;

                // attach it to the gear
                AttachToGear();
            }
        }
    }
}
