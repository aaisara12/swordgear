#nullable  enable

using UnityEngine;
using System;
using TMPro;

public class EnemyController : MonoBehaviour
{
    public event Action? OnDeath;
    
    [SerializeField] private GameObject? player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float hp = 100f;
    
    private Rigidbody2D? rb;

    public Element element = Element.Physical;

    public GameObject? floatingPoints;

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

    public void TakeDamage(float damage)
    {
        if (GameManager.Instance)
            GameManager.Instance.DisplayDamageUI(transform.position, damage);
        hp -= damage;
        if (floatingPoints != null)
        {
            //GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
            //points.transform.GetChild(0).GetComponent<TextMeshPro>().text = damage.ToString();

        }
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
