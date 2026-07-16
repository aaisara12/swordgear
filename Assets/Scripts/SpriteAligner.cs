using UnityEngine;

public class SpriteAligner : MonoBehaviour
{
    // Config
    [SerializeField] private float movementThreshold = 0.001f;

    // State
    private Quaternion initialWorldRotation;
    private PlayerGameplayPawn playerPawn;

    void Start()
    {
        // Store the starting global rotation
        initialWorldRotation = transform.rotation;

        playerPawn = GetComponentInParent<PlayerGameplayPawn>();
    }

    void Update()
    {
        // --- Maintain global rotation ---
        transform.rotation = initialWorldRotation;

        if (playerPawn == null)
        {
            return;
        }

        // --- Flip based on horizontal movement input ---
        Vector2 direction = playerPawn.MoveDirection;

        if (direction.sqrMagnitude > movementThreshold * movementThreshold)
        {
            Vector3 scale = transform.localScale;

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
    }
}
