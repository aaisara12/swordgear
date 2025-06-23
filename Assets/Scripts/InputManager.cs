using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    [SerializeField] private float idleZoneRadiusPercent = 0.2f;
    [SerializeField] private string gamepadActionName = "Move";
    public static event Action<Vector2> OnPressInIdleZone;
    public static event Action<Vector2> OnPressInMoveZone;
    public static event Action<Vector2> OnReleaseInIdleZone;
    public static event Action<Vector2> OnReleaseInMoveZone;
    public static event Action<Vector2> OnEnterIdleZone;
    public static event Action<Vector2> OnExitIdleZone;
    public static event Action<Vector2> OnDragInIdleZone;
    public static event Action<Vector2> OnDragInMoveZone;

    private InputAction stickAction;
    private bool isPressed;
    private bool wasInIdleZone;
    private Vector2 prevValue = Vector2.zero;

    private void Awake()
    {
        var playerInput = GetComponent<PlayerInput>();
        stickAction = playerInput.actions[gamepadActionName];
    }

    private void OnEnable()
    {
        stickAction.started += OnInputStarted;
        stickAction.canceled += OnInputCanceled;
    }

    private void OnDisable()
    {
        stickAction.started -= OnInputStarted;
        stickAction.canceled -= OnInputCanceled;
    }

    private void Update()
    {
        if (isPressed)
        {
            Vector2 input = stickAction.ReadValue<Vector2>();
            // Debug.Log(input);
            float magnitude = input.magnitude;
            float idleThreshold = idleZoneRadiusPercent;

            bool inIdleZone = magnitude <= idleThreshold;

            if (wasInIdleZone && !inIdleZone)
            {
                OnExitIdleZone?.Invoke(input);
                OnEnterMoveZone(input);
            }
            else if (!wasInIdleZone && inIdleZone)
            {
                OnExitMoveZone(input);
                OnEnterIdleZone?.Invoke(input);
            }

            if (inIdleZone)
            {
                OnDragInIdleZone?.Invoke(input);
            }
            else
            {
                OnDragInMoveZone?.Invoke(input);
            }

            wasInIdleZone = inIdleZone;
            prevValue = input;
        }
    }

    private void OnInputStarted(InputAction.CallbackContext context)
    {
        Vector2 input = stickAction.ReadValue<Vector2>();
        float magnitude = input.magnitude;
        float idleThreshold = idleZoneRadiusPercent;

        isPressed = true;
        wasInIdleZone = magnitude <= idleThreshold;

        if (wasInIdleZone)
        {
            OnPressInIdleZone?.Invoke(input);
        }
        else
        {
            OnPressInMoveZone?.Invoke(input);
        }
    }

    private void OnInputCanceled(InputAction.CallbackContext context)
    {

        // Vector2 input = stickAction.ReadValue<Vector2>();
        Vector2 input = prevValue;
        Debug.Log($"Input cancelled {input}");
        float magnitude = input.magnitude;
        float idleThreshold = idleZoneRadiusPercent;

        if (magnitude <= idleThreshold)
        {
            OnReleaseInIdleZone?.Invoke(input);
        }
        else
        {
            OnReleaseInMoveZone?.Invoke(input);
        }

        isPressed = false;
    }

    private void OnEnterMoveZone(Vector2 input) => OnExitIdleZone?.Invoke(input);
    private void OnExitMoveZone(Vector2 input) => OnEnterIdleZone?.Invoke(input);
}
