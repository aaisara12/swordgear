using UnityEngine;
using UnityEngine.InputSystem; // Required for InputAction.CallbackContext

public class RotateToPlayerInput : MonoBehaviour
{
    private PlayerControls playerControls;
    private PlayerControls.GameplayActions gameplayActions;

    void Awake()
    {
        playerControls = new PlayerControls();
        gameplayActions = playerControls.Gameplay;
    }

    void OnEnable()
    {
        playerControls.Enable();
        // Subscribe to the Move event
        gameplayActions.Move.performed += HandleMove;
        gameplayActions.Move.canceled += HandleMove; // Also subscribe to 'canceled' to know when they stop
    }

    void OnDisable()
    {
        // Clean up subscriptions to prevent memory leaks
        gameplayActions.Move.performed -= HandleMove;
        gameplayActions.Move.canceled -= HandleMove;
        playerControls.Disable();
    }

    private void HandleMove(InputAction.CallbackContext context)
    {
        // 1. Read the Vector2 value from the input action
        Vector2 moveInput = context.ReadValue<Vector2>();

        // 2. Check if the input is significant (prevents snapping to 0 when keys are released)
        if (moveInput.sqrMagnitude > 0)
        {
            // 3. Calculate the angle relative to the Y-axis
            // Atan2(x, y) results in 0 degrees when moving straight Up (0, 1)
            float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;

            // 4. Apply the rotation to the Z-axis
            // We use -angle because Unity's 2D rotation is counter-clockwise
            transform.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}