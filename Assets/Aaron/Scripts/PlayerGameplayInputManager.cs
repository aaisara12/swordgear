#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Links player input to the assigned PlayerGameplayPawn
/// </summary>
public class PlayerGameplayInputManager : MonoBehaviour
{
    private PlayerGameplayPawn? pawn;
    private PlayerControls.GameplayActions gameplayActions;
    private Vector2 lastReadAimDirection;
    private Vector2 lastReadAttackDirection;

    private Coroutine? attackDirectionCoroutine;
    private Coroutine? aimedAttackDirectionCoroutine;
    
    public void LinkPawn(PlayerGameplayPawn pawn)
    {
        this.pawn = pawn;
        gameplayActions.Enable();
    }
    
    public void UnlinkCurrentPawn()
    {
        pawn = null;
        gameplayActions.Disable();
    }

    private void Awake()
    {
        var playerControls = new PlayerControls();
        playerControls.Enable();
        gameplayActions = playerControls.Gameplay;

        gameplayActions.Attack.started += HandleAttackStarted;
        gameplayActions.Attack.performed += HandleAttackPerformed;
        gameplayActions.Attack.canceled += HandleAttackCanceled;
        
        gameplayActions.ChargeAttack.started += HandleChargeAttackStarted;
        gameplayActions.ChargeAttack.performed += HandleChargeAttackPerformed;
        gameplayActions.ChargeAttack.canceled += HandleChargeAttackCanceled;
        
        gameplayActions.AimedAttack.started += HandleAimedAttackStarted;
        gameplayActions.AimedAttack.performed += HandleAimedAttackPerformed;
        gameplayActions.AimedAttack.canceled += HandleAimedAttackCanceled;
        
        gameplayActions.Move.performed += HandleMove;
        gameplayActions.Move.canceled += HandleMove;
    }
    
    private void HandleAttackStarted(InputAction.CallbackContext obj)
    {
        ToggleAttackDirectionUpdate(true);
    }
    
    private void HandleAttackPerformed(InputAction.CallbackContext obj)
    {
        if (pawn == null)
        {
            return;
        }
        
        pawn.Attack(lastReadAttackDirection);
        ToggleAttackDirectionUpdate(false);
    }
    
    private void HandleAttackCanceled(InputAction.CallbackContext obj)
    {
        ToggleAttackDirectionUpdate(false);
        lastReadAttackDirection = Vector2.zero;
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
        if (pawn == null)
        {
            return;
        }
        
        pawn.StopAiming();
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
        pawn.StopAiming();
        ToggleAimDirectionUpdate(false);
    }

    private void HandleAimedAttackStarted(InputAction.CallbackContext obj)
    {
        ToggleAimDirectionUpdate(true);
    }

    private void ToggleAimDirectionUpdate(bool shouldUpdate)
    {
        if (shouldUpdate)
        {
            aimedAttackDirectionCoroutine = StartCoroutine(UpdateAimDirectionCoroutine());
        }
        else
        {
            if (aimedAttackDirectionCoroutine == null)
            {
                return;
            }
            
            StopCoroutine(aimedAttackDirectionCoroutine);

            aimedAttackDirectionCoroutine = null;
        }
    }
    
    private void ToggleAttackDirectionUpdate(bool shouldUpdate)
    {
        if (shouldUpdate)
        {
            attackDirectionCoroutine = StartCoroutine(UpdateAttackDirectionCoroutine());
        }
        else
        {
            if (attackDirectionCoroutine == null)
            {
                return;
            }
            
            StopCoroutine(attackDirectionCoroutine);

            attackDirectionCoroutine = null;
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

    private IEnumerator UpdateAttackDirectionCoroutine()
    {
        while (true)
        {
            var attackAction = gameplayActions.Attack;
            
            // aisara => Attack input is actually a composite input of a button press and an "optional" directional input. The button press is on Z and the direcitonal input is on X-Y
            lastReadAttackDirection = attackAction.ReadValue<Vector3>();
            Debug.Log(lastReadAttackDirection);
            
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

    private void OnDestroy()
    {
        gameplayActions.Attack.started -= HandleAttackStarted;
        gameplayActions.Attack.performed -= HandleAttackPerformed;
        gameplayActions.Attack.canceled -= HandleAttackCanceled;
        
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