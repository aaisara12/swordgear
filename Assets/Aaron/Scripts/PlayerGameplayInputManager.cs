#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Links player input to the assigned PlayerGameplayPawn
/// </summary>
public class PlayerGameplayInputManager : MonoBehaviour
{
    private PlayerGameplayPawn? pawn;
    private PlayerControls.GameplayActions gameplayActions;
    private Vector2 lastReadAimDirection;
    
    public void AssignPawn(PlayerGameplayPawn pawn)
    {
        this.pawn = pawn;
    }

    private void Awake()
    {
        var playerControls = new PlayerControls();
        playerControls.Enable();
        gameplayActions = playerControls.Gameplay;
        gameplayActions.Enable();
        
        gameplayActions.Attack.performed += HandleAttack;
        
        gameplayActions.ChargeAttack.started += HandleChargeAttackStarted;
        gameplayActions.ChargeAttack.performed += HandleChargeAttackPerformed;
        gameplayActions.ChargeAttack.canceled += HandleChargeAttackCanceled;
        
        gameplayActions.AimedAttack.started += HandleAimedAttackStarted;
        gameplayActions.AimedAttack.performed += HandleAimedAttackPerformed;
        gameplayActions.AimedAttack.canceled += HandleAimedAttackCanceled;
        
        gameplayActions.Move.performed += HandleMove;
        gameplayActions.Move.canceled += HandleMove;
    }

    private void HandleChargeAttackCanceled(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        pawn.CancelChargeAttack();
    }

    private void HandleChargeAttackStarted(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        pawn.BeginChargeAttack();
    }

    private void HandleChargeAttackPerformed(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        pawn.ReleaseChargeAttack();
    }

    private void HandleAimedAttackCanceled(InputAction.CallbackContext obj)
    {
        ToggleAimDirectionUpdate(false);
        lastReadAimDirection = Vector2.zero;
    }
    
    private void HandleAimedAttackPerformed(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        // aisara => We don't use the current input value here because it will be zero (since performed is registered on release when stick moves back to zero)
        pawn.DoAimedAttackInDirection(lastReadAimDirection);
    }

    private void HandleAimedAttackStarted(InputAction.CallbackContext obj)
    {
        ToggleAimDirectionUpdate(true);
    }

    private void ToggleAimDirectionUpdate(bool shouldUpdate)
    {
        if (shouldUpdate)
        {
            StartCoroutine(UpdateAimDirectionCoroutine());
        }
        else
        {
            StopCoroutine(UpdateAimDirectionCoroutine());
        }
    }

    private IEnumerator UpdateAimDirectionCoroutine()
    {
        while (true)
        {
            var throwSwordAction = gameplayActions.AimedAttack;
            lastReadAimDirection = throwSwordAction.ReadValue<Vector2>();
            pawn?.AimInDirection(lastReadAimDirection);
            
            yield return new WaitForEndOfFrame();
        }
    }

    private void HandleMove(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        Vector2 moveDirection = obj.ReadValue<Vector2>();
        pawn.MoveInDirection(moveDirection);
    }

    private void HandleAttack(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        pawn.Attack();
    }

    private void OnDestroy()
    {
        gameplayActions.Attack.performed -= HandleAttack;
        
        gameplayActions.ChargeAttack.started -= HandleChargeAttackStarted;
        gameplayActions.ChargeAttack.performed -= HandleChargeAttackPerformed;
        gameplayActions.ChargeAttack.canceled -= HandleChargeAttackCanceled;
        
        gameplayActions.AimedAttack.started -= HandleAimedAttackStarted;
        gameplayActions.AimedAttack.performed -= HandleAimedAttackPerformed;
        gameplayActions.AimedAttack.canceled -= HandleAimedAttackCanceled;
        
        gameplayActions.Move.performed -= HandleMove;
        gameplayActions.Move.canceled -= HandleMove;
        
        // aisara => I assume that coroutines are cleaned up automatically on destroy so don't need to do it here
    }
}