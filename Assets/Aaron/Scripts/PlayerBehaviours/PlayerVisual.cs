#nullable enable

using System;
using System.Collections;
using UnityEngine;

namespace Testing
{
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private float snapRotationDurationSeconds = 0.1f;
        [SerializeField] private float normalRotationDurationSeconds = 0.25f;
        [SerializeField] private Transform? playerVisual;
        
        private Coroutine? currentRotationCoroutine;
        
        public void SnapTowardsDirection(Vector2 direction)
        {
            if (playerVisual == null)
            {
                return;
            }
            
            if (direction == Vector2.zero)
            {
                return;
            }
            
            if (currentRotationCoroutine != null)
            {
                StopCoroutine(currentRotationCoroutine);
            }

            currentRotationCoroutine =
                StartCoroutine(SmoothRotateTowardsDirectionCoroutine(direction, snapRotationDurationSeconds, playerVisual));
        }

        public void RotateTowardsDirection(Vector2 direction)
        {
            if (playerVisual == null)
            {
                return;
            }
            
            if (direction == Vector2.zero)
            {
                return;
            }
            
            if (currentRotationCoroutine != null)
            {
                StopCoroutine(currentRotationCoroutine);
            }

            currentRotationCoroutine =
                StartCoroutine(SmoothRotateTowardsDirectionCoroutine(direction, normalRotationDurationSeconds, playerVisual));
        }
        
        private IEnumerator SmoothRotateTowardsDirectionCoroutine(Vector2 direction, float durationSeconds, Transform playerVisual)
        {
            if (direction == Vector2.zero)
            {
                yield break;
            }

            float timePassed = 0f;
            var startRotation = playerVisual.rotation;
            var targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
            
            while (timePassed < durationSeconds)
            {
                double t = Math.Cbrt(timePassed/durationSeconds);
                playerVisual.rotation = Quaternion.Slerp(startRotation, targetRotation, (float)t);
                timePassed += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            playerVisual.rotation = targetRotation;
        }
    }
}