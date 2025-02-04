using UnityEngine;

public class GearController : MonoBehaviour
{

    [SerializeField] float rotationSpeed = 10f;

    private float rotationAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        transform.Rotate(0, 0, rotationAmount);

    }


    public void Rotate(float direction)
    {
        rotationAmount = rotationSpeed * direction;
    }
}
