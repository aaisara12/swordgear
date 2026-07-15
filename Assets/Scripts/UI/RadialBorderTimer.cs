#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a ring (annulus) arc that sweeps from 0 to 360 degrees as Progress goes from 0 to 1.
/// Used as a radial cooldown/duration border around a UI element (e.g. an ability icon).
/// </summary>
public class RadialBorderTimer : MaskableGraphic
{
    [SerializeField] private float _outerRadius = 165f;
    [SerializeField] private float _innerRadius = 150f;
    [SerializeField] private int _segments = 48;
    [SerializeField] [Range(0f, 1f)] private float _progress;

    public float OuterRadius
    {
        get => _outerRadius;
        set
        {
            if (Mathf.Approximately(_outerRadius, value)) return;
            _outerRadius = value;
            SetVerticesDirty();
        }
    }

    public float InnerRadius
    {
        get => _innerRadius;
        set
        {
            if (Mathf.Approximately(_innerRadius, value)) return;
            _innerRadius = value;
            SetVerticesDirty();
        }
    }

    public float Progress
    {
        get => _progress;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, _progress)) return;
            _progress = clamped;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_progress <= 0.0001f) return;

        float endDeg = 360f * _progress;
        int segs = Mathf.Max(1, Mathf.CeilToInt(_segments * _progress));

        for (int i = 0; i <= segs; i++)
        {
            float angleDeg = Mathf.Lerp(0f, endDeg, (float)i / segs);
            float rad = angleDeg * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);

            // Clockwise-from-top in Unity UI coords: x = sin(a), y = cos(a)
            vh.AddVert(new Vector3(s * _innerRadius, c * _innerRadius), color, Vector2.zero);
            vh.AddVert(new Vector3(s * _outerRadius, c * _outerRadius), color, Vector2.zero);
        }

        for (int i = 0; i < segs; i++)
        {
            int b = i * 2;
            vh.AddTriangle(b, b + 1, b + 3);
            vh.AddTriangle(b, b + 3, b + 2);
        }
    }
}
