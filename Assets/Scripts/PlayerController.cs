using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private SwordController sword;
    [SerializeField] private GearController gear;

    private Rigidbody2D rb;
    
    private void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
    }
    
    private void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
    }

    private void OnAction()
    {
        sword.MoveSword(transform.position);
    }

    private void OnRotate(InputValue value)
    {
        gear.Rotate(value.Get<float>());
    }
}
