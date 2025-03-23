using UnityEngine;

public class BallController : MonoBehaviour
{
    [SerializeField] Vector2 startingVelocity = Vector2.zero;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float decelerationRate = 2f;
    private Rigidbody2D rb;
    public Vector2 prevVelocity = Vector2.zero;
    public static BallController Instance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing!");
        }
        Instance = this;
    }

    private void Start()
    {
        StartMotion();
    }

    public void StartMotion()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = startingVelocity;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed > maxSpeed)
        {
            Vector2 deceleration = rb.linearVelocity.normalized * (decelerationRate * Time.fixedDeltaTime);
            rb.linearVelocity -= deceleration;

            if (rb.linearVelocity.magnitude < maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        prevVelocity = rb.linearVelocity;
    }
}
