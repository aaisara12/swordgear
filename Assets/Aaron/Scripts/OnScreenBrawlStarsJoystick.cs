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

        private Vector2 m_originalJoystickOriginPosition = Vector2.zero;
        
        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));
            
            if (RectTransformUtility.RectangleContainsScreenPoint(m_joystickKnob, eventData.position))
            {
                m_joystickKnob.position += new Vector3(eventData.delta.x, eventData.delta.y, 0);
                
                if (Vector2.Distance(eventData.position, m_joystickOrigin.position) > m_range)
                {
                    m_joystickOrigin.position += new Vector3(eventData.delta.x, eventData.delta.y, 0);
                    m_rangeIndicator1meter.position = m_joystickOrigin.position;
                }

                float verticalComponent = (m_joystickKnob.position.y - m_joystickOrigin.position.y) / m_range;
                float horizontalComponent = (m_joystickKnob.position.x - m_joystickOrigin.position.x) / m_range;
                
                SendValueToControl(new Vector2(horizontalComponent, verticalComponent));
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));

            m_joystickOrigin.position = eventData.position;
            m_joystickKnob.position = m_joystickOrigin.position;
            m_rangeIndicator1meter.position = m_joystickOrigin.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));

            m_joystickOrigin.position = m_originalJoystickOriginPosition;
            m_joystickKnob.position = m_joystickOrigin.position;
            m_rangeIndicator1meter.position = m_originalJoystickOriginPosition;
            
            SendValueToControl(new Vector2(0, 0));
        }

        private void Awake()
        {
            if (AreAllDependenciesNonNull() == false)
            {
                Debug.LogError("Dependencies not set properly, destroying game object.");
                Destroy(gameObject);
                return;
            }
            
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            
            m_originalJoystickOriginPosition = m_joystickOrigin.position;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            
            if (AreAllDependenciesNonNull() == false)
            {
                return;
            }
            
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));
            m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
            m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
            
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