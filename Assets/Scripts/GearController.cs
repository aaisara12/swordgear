#nullable enable

using UnityEngine;

public class GearController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;

    private float rotationAmount;

    public void Rotate(float direction)
    {
        rotationAmount = rotationSpeed * direction;
    }
    
    private void FixedUpdate()
    {
        transform.Rotate(0, 0, rotationAmount);
    }
}
