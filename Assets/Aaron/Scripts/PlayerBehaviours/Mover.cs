#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

namespace Testing
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
    
        private Vector2 currentMoveDirection;

        // Update is called once per frame
        private void Update()
        {
            transform.Translate(new Vector2(currentMoveDirection.x, currentMoveDirection.y) * (moveSpeed * Time.deltaTime));
        }

        public void Move(Vector2 direction)
        {
            currentMoveDirection = direction.normalized;
        }
        
        private void OnMove(InputValue value)
        {
            Move(value.Get<Vector2>().normalized);
        }
    }
}
