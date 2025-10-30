#nullable enable

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AaronInputDemo
{
    public class Attacker : MonoBehaviour
    {
        [SerializeField] private GameObject? attackIndicator;
        [SerializeField] private float cooldownSeconds;
    
        private DateTimeOffset lastAttackTime;
        private Coroutine? activeAttackCoroutine;
    
        private void Awake()
        {
            if (attackIndicator == null)
            {
                Debug.LogError("Attack indicator is not set");
                return;
            }
            
            attackIndicator.SetActive(false);
        }
        
        private void OnAttack(InputValue value)
        {
            if (attackIndicator == null)
            {
                Debug.LogError("Attack indicator not set");
                return;
            }
            
            if (cooldownSeconds < 0)
            {
                Debug.LogError("Attacker cooldown seconds can't be less than 0");
                return;
            }
    
            if (lastAttackTime.AddSeconds(cooldownSeconds) > DateTimeOffset.Now)
            {
                // Attack on cooldown
                Debug.Log("Attack is on cooldown.");
                return;
            }
            
            attackIndicator.SetActive(true);
    
            if (activeAttackCoroutine != null)
            {
                // Reset animation state in case it's not finished yet
                StopCoroutine(activeAttackCoroutine);
                attackIndicator.SetActive(false);
            }
            
            activeAttackCoroutine = StartCoroutine(GetAttackForSecondsEnumerator(attackIndicator, cooldownSeconds));
        }
    
        private IEnumerator GetAttackForSecondsEnumerator(GameObject attackIndicatorRef, float seconds)
        {
            lastAttackTime = DateTimeOffset.UtcNow;
            
            attackIndicatorRef.SetActive(true);
            
            yield return new WaitForSeconds(seconds);
            
            attackIndicatorRef.SetActive(false);
        }
    }
}
