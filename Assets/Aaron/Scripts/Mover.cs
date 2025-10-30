#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

public class Mover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Vector2 currentMoveDireciton;

    // Update is called once per frame
    private void Update()
    {
        transform.Translate(new Vector2(currentMoveDireciton.x, currentMoveDireciton.y) * (moveSpeed * Time.deltaTime));
    }


    private void OnMove(InputValue value)
    {
        currentMoveDireciton = value.Get<Vector2>().normalized;
        Debug.Log(currentMoveDireciton);
    }
}
