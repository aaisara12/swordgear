#nullable enable

using UnityEngine;

public class JoystickVisualProvider : MonoBehaviour
{
    [SerializeField] private RectTransform? m_joystickOrigin;
    [SerializeField] private RectTransform? m_joystickKnob;
    [SerializeField] private RectTransform? m_rangeIndicator1meter;
    [SerializeField] private Canvas? m_joystickCanvas;
    [SerializeField] private int m_range = 300;
    
    private JoystickVisual? joystickVisual;

    public JoystickVisual Visual
    {
        get
        {
            if (joystickVisual == null)
            {
                joystickVisual = BuildVisualOrThrow();
            }
            
            return joystickVisual;
        }
    }
    
    private void Awake()
    {
        if (joystickVisual == null)
        {
            joystickVisual = BuildVisualOrThrow();
        }
    }

    private JoystickVisual BuildVisualOrThrow()
    {
        m_joystickOrigin.ThrowIfNull(nameof(m_joystickOrigin));
        m_joystickKnob.ThrowIfNull(nameof(m_joystickKnob));
        m_rangeIndicator1meter.ThrowIfNull(nameof(m_rangeIndicator1meter));
        m_joystickCanvas.ThrowIfNull(nameof(m_joystickCanvas));
        
        return new JoystickVisual(m_joystickOrigin, m_joystickKnob, m_rangeIndicator1meter, m_range, m_joystickCanvas);
    }
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        Visual.KnobRange = m_range;
        Visual.ResetPositions();
#endif
    }
}


