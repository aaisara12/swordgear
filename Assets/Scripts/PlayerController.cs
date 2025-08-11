#nullable enable

using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public event Action? OnDeath;

    [Header("Player Stats")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float flickThreshold = 50f;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Weapon Management")]
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;
    [SerializedDictionary("Element Type", "Weapon Prefab")]
    [SerializeField] private SerializedDictionary<Element, GameObject>? elementWeaponDict;
    Dictionary<Element, IMeleeWeapon> elementToWeapon = new Dictionary<Element, IMeleeWeapon>();

    public enum PlayerState
    {
        MeleeReady,
        SwordThrown
    }

    PlayerState playerState = PlayerState.MeleeReady;
    IMeleeWeapon? curWeapon;
    private Rigidbody2D? rb;

    [Header("Sword Recall")]
    [SerializeField] ParticleSystem? recallParticles;
    [SerializeField] float recallTime = 1f;
    float curRecallTimer = 0f;

    private void Awake()
    {
        elementWeaponDict.ThrowIfNull(nameof(elementWeaponDict));
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        foreach (Element elem in elementWeaponDict.Keys)
        {
            GameObject weaponObj = Instantiate(elementWeaponDict[elem]);
            IMeleeWeapon weapon = weaponObj.GetComponent<IMeleeWeapon>();
            elementToWeapon[elem] = weapon;
        }
        currentHp = maxHp;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            curWeapon = elementToWeapon[GameManager.Instance.currentElement];
        InputManager.OnReleaseInIdleZone += OnReleaseInIdle;
        InputManager.OnReleaseInMoveZone += OnReleaseInMove;
        InputManager.OnPressInIdleZone += OnTapInIdle;
        InputManager.OnDragInIdleZone += OnHoldInIdle;
    }

    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return; // Player is already dead

        currentHp -= damage;
        // You might want to display a damage UI here or trigger a hit animation.
        // For example, if GameManager has a method:
        // GameManager.Instance?.DisplayDamageUI(transform.position, damage);

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        OnDeath?.Invoke();
        // Here, you would trigger the game over state or restart the level.
        // Destroy(gameObject); // Or you could just disable the player.
    }

    public void SetElement(Element element)
    {
        curWeapon = elementToWeapon[element];
    }

    void MeleeCharge()
    {
        curWeapon.ThrowIfNull(nameof(curWeapon));
        curWeapon.Charge(transform);
    }

    void MeleeCancelCharge()
    {
        curWeapon.ThrowIfNull(nameof(curWeapon));
        curWeapon.Charge(transform, true);
    }

    void MeleeAttack()
    {
        rb.ThrowIfNull(nameof(rb));
        curWeapon.ThrowIfNull(nameof(curWeapon));

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
            curWeapon.Strike(transform);
            return;
        }

        Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;
        transform.up = direction;

        Vector2 dashPosition = (Vector2)transform.position + direction * (shortestDistance * dashFactor);
        transform.position = dashPosition;
        curWeapon.Strike(transform);
    }

    void SwordThrow(Vector2 direction)
    {
        if (playerState == PlayerState.MeleeReady)
        {
            MeleeCancelCharge();
            SwordProjectile.Instance.StartFlight(transform.position, direction * projectileSpeed);
            playerState = PlayerState.SwordThrown;
        }
    }

    void RecallSword()
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        recallParticles.Stop();
    }

    void CatchSword()
    {
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
    }

    private void OnMove(InputValue value)
    {
        rb.ThrowIfNull(nameof(rb));
        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
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