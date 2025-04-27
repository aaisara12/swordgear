using UnityEngine;

public class FloatingTextHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 2f);
        transform.localPosition += new Vector3(0, 0.5f, 0);
    }

}
