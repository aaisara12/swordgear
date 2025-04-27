#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerOld : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;

    private Rigidbody2D? rb;
    
    private void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
    }
    
    private void OnMove(InputValue value)
    {
        rb.ThrowIfNull(nameof(rb));
        
        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
    }

    private void OnAction()
    {
        sword.ThrowIfNull(nameof(sword));
        
        sword.MoveSword(transform.position);
    }

    private void OnRotate(InputValue value)
    {
        gear.ThrowIfNull(nameof(gear));
        
        gear.Rotate(value.Get<float>());
    }
}
