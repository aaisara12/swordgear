#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Serialization;


namespace AaronInputDemo
{
    public class OnScreenMovementJoystick : OnScreenControl, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string m_ControlPath = string.Empty;

        [SerializeField] private RectTransform? m_joystickOrigin;
        [SerializeField] private RectTransform? m_joystickOriginHome;
        [SerializeField] private RectTransform? m_joystickKnob;
        [SerializeField] private RectTransform? m_rangeIndicator1meter;
        [SerializeField] private Canvas? m_joystickCanvas;
        
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
            m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));
            m_joystickCanvas.ThrowIfNull(nameof(m_joystickCanvas));
            
            m_joystickKnob.position = eventData.position;
            
            float scaledRange = m_range * m_joystickCanvas.scaleFactor;
            if (Vector2.Distance(eventData.position, m_joystickOrigin.position) > scaledRange)
            {
                Vector3 directionFromPointerToOrigin = (m_joystickOrigin.position - new Vector3(eventData.position.x, eventData.position.y, 0)).normalized;
                m_joystickOrigin.position = directionFromPointerToOrigin * scaledRange + m_joystickKnob.position;
                m_rangeIndicator1meter.position = m_joystickOrigin.position;
            }
            
            float verticalComponent = (m_joystickKnob.position.y - m_joystickOrigin.position.y) / scaledRange;
            float horizontalComponent = (m_joystickKnob.position.x - m_joystickOrigin.position.x) / scaledRange;
            
            SendValueToControl(new Vector2(horizontalComponent, verticalComponent));
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
            m_joystickOriginHome.ThrowIfNull(nameof(m_joystickOriginHome));

            m_joystickOrigin.position = m_joystickOriginHome.position;
            m_joystickKnob.position = m_joystickOrigin.position;
            m_rangeIndicator1meter.position = m_joystickOrigin.position;
            
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

            if (m_joystickOriginHome == null)
            {
                Debug.LogError("Joystick Origin Home is null!");
                passNullCheck = false;
            }

            if (m_joystickCanvas == null)
            {
                Debug.LogError("Joystick Canvas is null!");
                passNullCheck = false;
            }
            
            return passNullCheck;
        }
    }
}