// Shared building blocks for the element shaders (gear arcs, hub, vignette, bursts).
//
// Everything here is pure maths on positions and time, so a shader can make an element "live" without
// textures: the gear arcs are procedural meshes, and Ice and Earth have no pack textures to lean on anyway.
// Keep functions cheap: these run on phones, on large screen areas (the vignette) as well as small ones.
#ifndef SWORDGEAR_ELEMENT_FX_INCLUDED
#define SWORDGEAR_ELEMENT_FX_INCLUDED

float EFX_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 EFX_Hash22(float2 p)
{
    float n = EFX_Hash21(p);
    return float2(n, EFX_Hash21(p + n + 17.17));
}

// Smooth value noise in [0, 1].
float EFX_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = EFX_Hash21(i);
    float b = EFX_Hash21(i + float2(1, 0));
    float c = EFX_Hash21(i + float2(0, 1));
    float d = EFX_Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Four octaves of value noise, roughly in [0, 1]. Fixed octave count so the loop unrolls on mobile.
float EFX_Fbm(float2 p)
{
    float sum = 0.0;
    float amp = 0.5;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        sum += amp * EFX_ValueNoise(p);
        p = p * 2.03 + float2(17.1, 9.2);
        amp *= 0.5;
    }
    return sum / 0.9375;
}

// Cellular noise. x = distance to the nearest cell point, y = distance to the edge between the two nearest
// cells (small on cell borders): facets and cracks come from y.
float2 EFX_Voronoi(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    float f1 = 8.0;
    float f2 = 8.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y);
            float2 r = offset + EFX_Hash22(cell + offset) - f;
            float d = dot(r, r);
            if (d < f1)
            {
                f2 = f1;
                f1 = d;
            }
            else if (d < f2)
            {
                f2 = d;
            }
        }
    }
    f1 = sqrt(f1);
    f2 = sqrt(f2);
    return float2(f1, f2 - f1);
}

// Glow along both radial edges of a band, where v runs 0 (inner edge) to 1 (outer edge).
float EFX_Rim(float v, float width)
{
    return smoothstep(width, 0.0, v) + smoothstep(1.0 - width, 1.0, v);
}

// A soft 0..1 pulse at `rate` cycles per second.
float EFX_Pulse(float time, float rate)
{
    return 0.5 + 0.5 * sin(time * rate * 6.2831853);
}

// ---------- Cartoon shapes ----------
// The polish pass is drawn as cartoon: flat colour regions with crisp edges and ink outlines rather than
// soft gradients. These turn shape maths into edges that stay one pixel sharp, and anti-aliased, at any zoom.
// They use screen-space derivatives, so call them outside any branch that varies per pixel.

// Anti-aliased step: 0 below `edge`, 1 above, blended across one pixel.
float EFX_Step(float edge, float x)
{
    return saturate((x - edge) / max(fwidth(x), 1e-5) + 0.5);
}

// Coverage of a shape from its signed distance (negative inside), anti-aliased across one pixel.
float EFX_Fill(float sdf)
{
    return 1.0 - EFX_Step(0.0, sdf);
}

// Signed distance to a box of half-size b, corners rounded by r, centred on the origin.
float EFX_SdRoundBox(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

// Turns an implicit field (zero on a shape's edge, negative inside) into an approximate signed distance in
// the units of `unit`, a coordinate measured in world units. Outlines drawn from it keep one width however
// steep the field gets, which a raw field can't: a flame tongue's sides would get thin ink and its tip thick.
float EFX_FieldToDistance(float field, float unit)
{
    float gradient = length(float2(ddx(field), ddy(field)));
    float unitsPerPixel = length(float2(ddx(unit), ddy(unit)));
    return field / max(gradient, 1e-6) * unitsPerPixel;
}

// A four-point twinkle star (a diamond with concave sides) of radius `size`; negative inside.
float EFX_SdStar4(float2 p, float size)
{
    float2 q = abs(p) / max(size, 1e-4);
    return (sqrt(q.x) + sqrt(q.y) - 1.0) * size;
}

// Straight-alpha "over": `top` composited onto `bottom`.
half4 EFX_Over(half4 top, half4 bottom)
{
    half a = top.a + bottom.a * (1.0 - top.a);
    half3 rgb = (top.rgb * top.a + bottom.rgb * bottom.a * (1.0 - top.a)) / max(a, 1e-4);
    return half4(rgb, a);
}

#endif
