#nullable enable
using UnityEngine;
using System;
using TMPro;

public class EnemyController : MonoBehaviour
{
    public event Action? OnDeath;

    [SerializeField] private GameObject? player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float hp = 100f;

    public float speedMultiplier = 1f;

    private Rigidbody2D? rb;
    private IMovementStrategy? movementStrategy;

    public Element element = Element.Physical;

    public GameObject? floatingPoints;

    private void Start()
    {
        player = GameManager.Instance?.player;
        rb = GetComponent<Rigidbody2D>();
        movementStrategy = GetComponent<IMovementStrategy>();
    }

    private void FixedUpdate()
    {
        if (player == null || rb == null || movementStrategy == null)
        {
            return;
        }
        movementStrategy.Move(rb, player.transform, speed * speedMultiplier);
    }

    public void TakeDamage(float damage)
    {
        if (GameManager.Instance)
            GameManager.Instance.DisplayDamageUI(transform.position, damage);

        hp -= damage;

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
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}