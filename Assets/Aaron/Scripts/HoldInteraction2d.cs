using UnityEngine;
using UnityEngine.InputSystem;

namespace AaronInputDemo
{
    public class HoldInteraction2d : IInputInteraction
    {
        public float holdDuration = 0.03f;
        
        public void Process(ref InputInteractionContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            
            Debug.Log($"Process: Direction {direction} - State {context.phase} - Expired {context.timerHasExpired}");
            
            if (context.timerHasExpired)
            {
                context.PerformedAndStayStarted();
                return;
            }

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    if (context.ControlIsActuated())
                    {
                        context.Started();
                        context.SetTimeout(holdDuration);
                    }
                    break;

                case InputActionPhase.Started:
                    if (context.ControlIsActuated())
                    {
                        context.SetTimeout(holdDuration);
                    }
                    else
                    {
                        context.Canceled();
                    }
                    break;
            }
        }
        
        public void Reset()
        {
        }
    }
}