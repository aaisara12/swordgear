#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;


namespace AaronInputDemo
{
    public class OnScreenBrawlStarsJoystick : OnScreenControl, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string m_ControlPath = string.Empty;

        [SerializeField] private RectTransform? m_joystickOrigin;
        [SerializeField] private RectTransform? m_joystickKnob;
        [SerializeField] private RectTransform? m_rangeIndicator1meter;
        
        [SerializeField] private int m_range = 300;
        
        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            
            if (RectTransformUtility.RectangleContainsScreenPoint(m_joystickKnob, eventData.position))
            {
                Vector2 originalPosition = m_joystickKnob.position;
                
                m_joystickKnob.position += new Vector3(eventData.delta.x, eventData.delta.y, 0);
                
                if (Vector2.Distance(eventData.position, m_joystickOrigin.position) > m_range)
                {
                    m_joystickKnob.position = originalPosition;
                }

                float verticalComponent = (m_joystickKnob.position.y - m_joystickOrigin.position.y) / m_range;
                float horizontalComponent = (m_joystickKnob.position.x - m_joystickOrigin.position.x) / m_range;
                
                SendValueToControl(new Vector2(horizontalComponent, verticalComponent));
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // TODO
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));

            Debug.Log("ON POINTER UP");
            
            m_joystickKnob.position = m_joystickOrigin.position;
            
            SendValueToControl(new Vector2(0, 0));
        }

        private void Awake()
        {
            if (AreAllDependenciesNonNull() == false)
            {
                Debug.LogError("Dependencies not set properly, destroying game object.");
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
            if (AreAllDependenciesNonNull() == false)
            {
                return;
            }
            
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            
#if UNITY_EDITOR
            m_rangeIndicator1meter.localScale = new Vector3(m_range * 2, m_range * 2, 1);
            m_joystickKnob.position = m_joystickOrigin.position;
            m_rangeIndicator1meter.position = m_joystickOrigin.position;
#endif
        }

        private bool AreAllDependenciesNonNull()
        {
            bool passNullCheck = true;
            
            if (m_rangeIndicator1meter == null)
            {
                Debug.LogError("Range Indicator is null!");
                passNullCheck = false;
            }

            if (m_joystickKnob == null)
            {
                Debug.LogError("Joystick Knob is null!");
                passNullCheck = false;
            }

            if (m_joystickOrigin == null)
            {
                Debug.LogError("Joystick Origin is null!");
                passNullCheck = false;
            }
            
            return passNullCheck;
        }
    }
}