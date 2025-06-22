#nullable enable

using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float flickThreshold = 50f;

    [SerializeField] private float speed = 3f;
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;

    [SerializeField] private GameObject? weaponObject;

    public enum PlayerState
    {
        MeleeReady,
        SwordThrown
    }

    PlayerState playerState = PlayerState.MeleeReady;

    IMeleeWeapon? weapon;

    private Rigidbody2D? rb;

    private void Awake()
    {
        weaponObject.ThrowIfNull(nameof(weaponObject));

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        weapon = weaponObject.GetComponent<IMeleeWeapon>();
    }

    private void Start()
    {
        InputManager.OnReleaseInIdleZone += OnReleaseInIdle;
        InputManager.OnReleaseInMoveZone += OnReleaseInMove;
        InputManager.OnPressInIdleZone += OnTapInIdle;
        InputManager.OnDragInIdleZone += OnHoldInIdle;
    }

    void MeleeCharge()
    {
        weapon.ThrowIfNull(nameof(weapon));

        weapon.Charge(transform);
    }

    void MeleeCancelCharge()
    {
        weapon.ThrowIfNull(nameof(weapon));

        weapon.Charge(transform, true);
    }

    void MeleeAttack()
    {
        rb.ThrowIfNull(nameof(rb));
        weapon.ThrowIfNull(nameof(weapon));

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject? nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < shortestDistance && distance <= attackRadius)
            {
                shortestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy == null)
        {
            weapon.Strike(transform);
            return;
        }

        Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;
        transform.up = direction;

        Vector2 dashPosition = (Vector2)transform.position + direction * (shortestDistance * dashFactor);
        transform.position = dashPosition;
        weapon.Strike(transform);
    }

    void SwordThrow(Vector2 direction)
    {
        if (playerState == PlayerState.MeleeReady)
        {
            // Cancel charge if applicable
            MeleeCancelCharge();
            SwordProjectile.Instance.StartFlight(transform.position, direction * projectileSpeed);
            playerState = PlayerState.SwordThrown;
        }
        
    }

    private void OnMove(InputValue value)
    {
        rb.ThrowIfNull(nameof(rb));

        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
    }

    [SerializeField] ParticleSystem? recallParticles;
    [SerializeField] float recallTime = 1f;
    float curRecallTimer = 0f;

    void RecallSword()
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));

        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        recallParticles.Stop();
        // Play effect
    }

    void CatchSword()
    {
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        // Add buffs
    }

    private void OnReleaseInIdle(Vector2 val)
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));

        if (playerState == PlayerState.SwordThrown)
        {
            recallParticles.Stop();
        }
        if (playerState == PlayerState.MeleeReady)
        {
            MeleeAttack();
        }

        if (playerState == PlayerState.SwordThrown &&
            Vector2.Distance(transform.position, SwordProjectile.Instance.transform.position) < 0.5f)
        {
            CatchSword();
        }
    }

    private void OnReleaseInMove(Vector2 val)
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));
        if (playerState == PlayerState.SwordThrown)
        {
            recallParticles.Stop();
        }
        SwordThrow(val.normalized);
    }

    private void OnTapInIdle(Vector2 val)
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));
        if (playerState == PlayerState.SwordThrown)
        {
            curRecallTimer = 0f;
            recallParticles.Play();
        }
        else if (playerState == PlayerState.MeleeReady)
        {
            MeleeCharge();
        }
    }

    private void OnHoldInIdle(Vector2 val)
    {
        if (playerState == PlayerState.SwordThrown)
        {
            curRecallTimer += Time.deltaTime;
            if (curRecallTimer >= recallTime)
            {
                RecallSword();
            }
        }
    }

}
