#nullable  enable

using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{
    public event Action? OnDeath;
    
    [SerializeField] private GameObject? player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float hp = 10f;
    
    private Rigidbody2D? rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        player = GameManager.Instance?.player;
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        player.ThrowIfNull(nameof(player));
        
        Move(player.transform.position);
    }

    private void Move(Vector2 target)
    {
        rb.ThrowIfNull(nameof(rb));
        
        Vector2 position = transform.position;
        Vector2 direction = (target - position).normalized;

        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        float damage = collision.gameObject.GetComponent<SwordController>().getDamage();
        TakeDamage(damage);
    }

    private void TakeDamage(float damage)
    {
        hp -= damage;
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
