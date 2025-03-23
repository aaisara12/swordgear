using UnityEngine;
using UnityEngine.InputSystem;

public class WheelController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;
    private Vector2 mouseDelta;
    private InputAction dragAction;
    private InputAction resetAction;

    private void Awake()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component is missing!");
            return;
        }

        dragAction = playerInput.actions["Drag"];
        resetAction = playerInput.actions["Reset"];
    }

    private void OnEnable()
    {
        if (dragAction != null)
            dragAction.performed += OnDrag;
        if (dragAction != null)
            dragAction.canceled += OnDragCancel;
        if (resetAction != null)
            resetAction.performed += OnReset;
    }

    private void OnDisable()
    {
        if (dragAction != null)
            dragAction.performed -= OnDrag;
        if (dragAction != null)
            dragAction.canceled -= OnDragCancel;
        if (resetAction != null)
            resetAction.performed -= OnReset;
    }

    private void OnReset(InputAction.CallbackContext context)
    {
        BallController.Instance.StartMotion();
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
    }

    private void OnDragCancel(InputAction.CallbackContext context)
    {
        mouseDelta = Vector2.zero;
    }

    private void Update()
    {
        if (mouseDelta != Vector2.zero)
        {
            Vector3 rotation = new Vector3(0, 0, mouseDelta.x) * rotationSpeed * Time.deltaTime;
            transform.Rotate(rotation, Space.Self);
        }
    }
}
