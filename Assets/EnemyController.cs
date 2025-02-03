using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private GameObject player;

    [SerializeField] float speed = 2f;

    Rigidbody2D rb;

    [SerializeField] float hp = 10f;

    public event Action onDeath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameManager.Instance.player;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Move(player.transform.position);
    }

    void Move(Vector2 target)
    {
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
        onDeath.Invoke();
        Destroy(gameObject);
    }
}
