#nullable enable

using System;
using UnityEngine;

namespace Testing
{
    public class ShootDirectionVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject? projectileDirectionIndicator;

        private Vector2 projectileAimDirection;
        
        public void SetShootDirection(Vector2 direction)
        {
            projectileAimDirection = direction.normalized;
        }
        
        private void Update()
        {
            if (projectileDirectionIndicator == null)
            {
                return;
            }
            
            if (projectileAimDirection.magnitude == 0)
            {
                projectileDirectionIndicator.SetActive(false);
                return;
            }
            
            projectileDirectionIndicator.SetActive(true);
            
            float rotationAngle = Mathf.Atan2(projectileAimDirection.y, projectileAimDirection.x) * Mathf.Rad2Deg;
            projectileDirectionIndicator.transform.rotation = Quaternion.Euler(0, 0, rotationAngle - 90);
        }
    }
}