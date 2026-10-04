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

#endif
