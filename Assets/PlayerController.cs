using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 3f;
    [SerializeField] SwordController sword;
    [SerializeField] GearController gear;

    Rigidbody2D rb;
    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
    }

    void OnAction()
    {
        sword.MoveSword(transform.position);
    }

    void OnRotate(InputValue value)
    {
        gear.Rotate(value.Get<float>());
    }
}
