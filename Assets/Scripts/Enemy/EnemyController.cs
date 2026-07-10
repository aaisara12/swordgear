#nullable enable
using UnityEngine;
using System;
using System.Collections;
using TMPro;

public class EnemyController : MonoBehaviour
{
    // Fired when this specific enemy dies.
    public event Action? OnDeath;

    // Global events so systems like ComboSystem can listen to all enemy hits/deaths.
    public static event Action<EnemyController, float, MoveType>? OnAnyEnemyHit;
    public static event Action<EnemyController>? OnAnyEnemyDeath;

    [SerializeField] private GameObject? player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float hp = 100f;
    [SerializeField] private GameObject? deathFX;

    public float speedMultiplier = 1f;

    private Rigidbody2D? rb;
    private IMovementStrategy? movementStrategy;

    public Element element = Element.Physical;

    public GameObject? floatingPoints;

    /// <summary>
    /// Applies difficulty / elemental / elite spawn multipliers.
    /// Call immediately after Instantiate, before the enemy acts.
    /// </summary>
    public void ApplySpawnModifiers(in SpawnModifiers modifiers)
    {
        hp *= Mathf.Max(0.05f, modifiers.HpMultiplier);
        speed *= Mathf.Max(0.05f, modifiers.SpeedMultiplier);

        if (!Mathf.Approximately(modifiers.ScaleMultiplier, 1f))
        {
            transform.localScale *= Mathf.Max(0.05f, modifiers.ScaleMultiplier);
        }

        EnemyAttackDamage? combat = GetComponent<EnemyAttackDamage>();
        if (combat == null)
        {
            combat = gameObject.AddComponent<EnemyAttackDamage>();
        }

        combat.ApplyCombatMultipliers(modifiers);
    }

    private void OnEnable()
    {
        ActiveEnemyRegistry.Register(this);
    }

    private void OnDisable()
    {
        ActiveEnemyRegistry.Unregister(this);
    }

    private void Start()
    {
        player = GameManager.Instance?.player;
        rb = GetComponent<Rigidbody2D>();
        movementStrategy = GetComponent<IMovementStrategy>();
    }

    private void FixedUpdate()
    {
        if (PlayerGameplayManager.Instance?.IsDefeated == true)
        {
            return;
        }

        if (player == null || rb == null || movementStrategy == null)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float directionX = player.transform.position.x - rb.position.x;
        if (directionX > 0)
        {
            scale.x = Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;

        // move 
        movementStrategy.Move(rb, player.transform, speed * speedMultiplier);
    }

    public void TakeDamage(float damage, MoveType moveType = default)
    {
        if (GameManager.Instance)
            GameManager.Instance.DisplayDamageUI(transform.position, damage);

        OnAnyEnemyHit?.Invoke(this, damage, moveType);

        hp -= damage;

        // Notify systems that player dealt damage (e.g. lifesteal)
        GameManager.NotifyPlayerDealtDamage(damage);

        // Note: The floating points code is commented out here as it was in your original script.
        // if (floatingPoints != null)
        // {
        //     GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
        //     points.transform.GetChild(0).GetComponent<TextMeshPro>().text = damage.ToString();
        // }

        if (hp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Global death event for systems that care about any enemy death.
        OnAnyEnemyDeath?.Invoke(this);

        OnDeath?.Invoke();
        GameObject? effectObject = PrefabPool.Instance!.Spawn(deathFX, transform.position, Quaternion.identity);
        IAttackAnimator? effect = null;
        if (effectObject != null)
            effect = effectObject.GetComponent<IAttackAnimator>();

        if (effect != null)
            effect.PlayAnimation();
        Destroy(gameObject);
    }
}