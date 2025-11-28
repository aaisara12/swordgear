#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AaronInputDemo
{
    public class Thrower : MonoBehaviour
{
    [SerializeField] private GameObject? projectileDirectionIndicator;
    [SerializeField] private GameObject? projectilePrefab;

    private Vector2 projectileAimDirection;
    private Vector2 queuedProjectileDirection;
    
    private void Update()
    {
        if (projectileDirectionIndicator == null)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        if (queuedProjectileDirection != Vector2.zero)
        {
            StartCoroutine(GetProjectileMoveEnumerator(projectilePrefab, queuedProjectileDirection, 10, 10));
            
            queuedProjectileDirection = Vector2.zero;
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
    
    
    private void OnThrow(InputValue value)
    {
        var newProjectileAimDirection = value.Get<Vector2>().normalized;

        if (newProjectileAimDirection.magnitude == 0)
        {
            queuedProjectileDirection = projectileAimDirection;
        }
        
        projectileAimDirection = newProjectileAimDirection;
    }

    private IEnumerator GetProjectileMoveEnumerator(GameObject prefab, Vector2 direction, float distance, float speed)
    {
        GameObject projectile = Instantiate(prefab, transform.position, Quaternion.identity);
        float projectileRotation = Mathf.Atan2(queuedProjectileDirection.y, queuedProjectileDirection.x) * Mathf.Rad2Deg;
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
