#nullable enable

using System.Collections;
using UnityEngine;

namespace Testing
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private GameObject? projectilePrefab;
        
        public void ShootInDirection(Vector2 direction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"No projectile prefab assigned - cannot shoot!");
                return;
            }
            
            StartCoroutine(AnimateProjectileCoroutine(projectilePrefab, direction, 10, 10));
        }
        
        private IEnumerator AnimateProjectileCoroutine(GameObject prefab, Vector2 direction, float distance, float speed)
        {
            GameObject projectile = Instantiate(prefab, transform.position, Quaternion.identity);
            float projectileRotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0, 0, projectileRotation - 90);
            
            float distanceTraveled = 0;
            
            while (distanceTraveled < distance)
            {
                var distanceTraveledThisFrame = speed * Time.deltaTime;
                projectile.transform.Translate(direction.normalized * distanceTraveledThisFrame, Space.World);
                distanceTraveled += distanceTraveledThisFrame;
                yield return new WaitForEndOfFrame();
            }
            
            Destroy(projectile);
        }
    }
}