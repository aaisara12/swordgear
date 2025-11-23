#nullable enable

using UnityEngine;

public class JoystickVisual
{
    private RectTransform origin;
    private RectTransform knob;
    private RectTransform knobRangeRepresentation;
    private float knobRange;
    private Canvas canvasDisplayingJoystick;

    public float KnobRange
    {
        get => knobRange;
        set
        {
            knobRange = value;
            knobRangeRepresentation.localScale = new Vector3(knobRange * 2, knobRange * 2, 1);
        }
    }
    
    public Vector2 OriginPosition
    {
        get => new Vector2(origin.position.x, origin.position.y) / canvasDisplayingJoystick.scaleFactor;
        set
        {
            origin.position = new Vector3(value.x, value.y, 0f) * canvasDisplayingJoystick.scaleFactor;
            knobRangeRepresentation.position = origin.position;
        }
    }

    public Vector2 KnobPosition
    {
        get => new Vector2(knob.position.x, knob.position.y) / canvasDisplayingJoystick.scaleFactor;
        set => knob.position = new Vector3(value.x, value.y, 0f) * canvasDisplayingJoystick.scaleFactor;
    }

    public JoystickVisual(
        RectTransform origin,
        RectTransform knob,
        RectTransform knobRangeRepresentation,
        float knobRange,
        Canvas canvasDisplayingJoystick)
    {
        this.origin = origin;
        this.knob = knob;
        this.knobRangeRepresentation = knobRangeRepresentation;
        KnobRange = knobRange;
        this.canvasDisplayingJoystick = canvasDisplayingJoystick;
    }

    public void Move(Vector2 newPosition)
    {
        Vector3 worldSpacePosition = new Vector3(newPosition.x, newPosition.y, 0f) * canvasDisplayingJoystick.scaleFactor;
        
        Vector3 moveVector = worldSpacePosition - origin.position;
        
        // Maintains the relative positioning of the knob
        origin.position += moveVector;
        knob.position += moveVector;
        
        knobRangeRepresentation.position = origin.position;
    }

    public void ResetPositions()
    {
        knob.position = origin.position;
        knobRangeRepresentation.position = origin.position;
        knobRangeRepresentation.localScale = new Vector3(KnobRange * 2, KnobRange * 2, 1);
    }
}

