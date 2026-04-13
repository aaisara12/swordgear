using UnityEngine;

public class SpriteAligner : MonoBehaviour
{
    // Config
    [SerializeField] private float movementThreshold = 0.001f;

    // State
    private Quaternion initialWorldRotation;
    private Vector3 lastPosition;

    void Start()
    {
        // Store the starting global rotation
        initialWorldRotation = transform.rotation;

        // Initialize last position
        lastPosition = transform.position;
    }

    void Update()
    {
        // --- Maintain global rotation ---
        transform.rotation = initialWorldRotation;

        // --- Calculate movement ---
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;

        // Only update direction if movement is significant
        if (delta.sqrMagnitude > movementThreshold * movementThreshold)
        {
            Vector3 direction = delta.normalized;

            Vector3 scale = transform.localScale;

            // Flip based on horizontal movement
            if (direction.x > 0)
            {
                scale.x = -Mathf.Abs(scale.x);
            }
            else if (direction.x < 0)
            {
                scale.x = Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }

        // Store position for next frame
        lastPosition = currentPosition;
    }
}