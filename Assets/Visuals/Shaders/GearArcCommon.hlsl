// The contract every gear-arc shader shares: the arc mesh's vertex data, the overflow that lets an element
// spill past the gear, the outward swell, the shared cartoon tile, and the arc's state (highlight / active)
// as GearArcVisual drives it through a MaterialPropertyBlock.
//
// Each element's arc shader includes this, declares its own look in GEAR_ARC_MATERIAL_PROPERTIES, and only
// writes a fragment function (or, to work out something that's the same for the whole arc once per vertex,
// a vertex function that calls ArcVertex unchanged and adds its own varyings — as Lightning does for its
// strike schedule). Every return from the fragment goes through GearRecede (GearPresence.hlsl), so the arc
// sits back with the rest of the gear while the player isn't picking. That keeps the ring one object: every
// arc idles as the same calm cartoon tile
// in its element's colour (ArcNeutral) and is outlined the same way when aimed at (ArcHalo); only the
// *active* arc breaks out into its element's own wild, animated look (ArcEnergy), spilling past the band.
//
// Arc mesh channels (GearArcVisual.Rebuild):
//   COLOR      the arc's element colour and alpha (eased on the CPU)
//   TEXCOORD0  u along the arc 0..1, v inner edge 0 -> outer edge 1 (beyond 0..1 in the overflow)
//   TEXCOORD1  the same in world units (length along the mid radius, distance out from the inner edge)
//   TEXCOORD2  overflow: x = radial world units this vertex opens by, y = radians it turns by
// Per-arc block values: _Highlight, _Active (state, 0..1), _ArcShape (sweep in radians, inner
// radius, outer radius, centre angle in radians) and _FlareTime (when the arc last flared, in _Time.y).
#ifndef SWORDGEAR_GEAR_ARC_COMMON_INCLUDED
#define SWORDGEAR_GEAR_ARC_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "ElementFX.hlsl"
#include "GearPresence.hlsl"

#ifndef GEAR_ARC_MATERIAL_PROPERTIES
#define GEAR_ARC_MATERIAL_PROPERTIES
#endif

// The shared tile and the arc states: the same for every arc, so the ring reads as one object. They're shader
// globals set from GearArcArt.asset (its "Shared tile" and "States" sections), so one edit there changes the
// whole ring; the asset's tooltips describe each.
float _ArcInkWidth;
half _ArcInkTone;
float _ArcCornerRadius;
float _ArcHaloWidth;
half _ArcHaloGlow;
float _ArcShinePeriod;
half _ArcShineStrength;
float _ArcShineWidth;
half _ArcShadeTone;
half _ArcShadeShare;
half _ArcLitShare;
half _ArcLitTint;
half _ArcMaxBrightness;
float _ArcSwell;
half _ArcHighlightBoost;
half _ArcActiveBoost;
float _ArcFlarePop;
half _ArcFlareBlaze;
float _ArcFlareDecay;

CBUFFER_START(UnityPerMaterial)
    // Per arc (MaterialPropertyBlock), eased by GearArcVisual.
    half _Highlight;
    half _Active;
    float4 _ArcShape;
    float _FlareTime;

    GEAR_ARC_MATERIAL_PROPERTIES
CBUFFER_END

struct ArcAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 uvWorld : TEXCOORD1;
    float2 overflow : TEXCOORD2;
};

struct ArcVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 uvWorld : TEXCOORD1;
    float2 positionWS : TEXCOORD2;
    float2 positionOS : TEXCOORD3;   // before the swell, so the arc's own frame doesn't move when it swells
};

float ArcSweep() { return max(_ArcShape.x, 1e-3); }
float ArcThickness() { return max(_ArcShape.z - _ArcShape.y, 1e-3); }

// How much of the element's own look shows: 0 idle or merely aimed at, 1 while it's the active imbue.
// Aiming keeps to the shared tile (swell, brighten, halo) so only the active arc goes wild.
half ArcEnergy()
{
    return _Active;
}

// How far the element's own look has taken over the tile itself. It lags ArcEnergy a little, so on a
// switch the element bursts out of the tile first and then swallows it.
half ArcTakeover()
{
    return smoothstep(0.05, 0.9, ArcEnergy());
}

// Half the arc's length along its mid radius, world units: uvWorld.x of its centre.
float ArcHalfLength()
{
    return 0.25 * ArcSweep() * (_ArcShape.y + _ArcShape.z);
}

// The flare when the arc's element is granted: 1 at the grant, gone in about half a second. The stamp is on
// the same clock as _Time.y (Time.time); one in the future (the default) reads as long over.
half ArcFlare()
{
    float since = _Time.y - _FlareTime;
    return since < 0.0 ? 0.0 : exp(-since * _ArcFlareDecay);
}

// The overflow opens fully as soon as the arc is in play; what's drawn in it is up to the fragment.
half ArcSpillOpen()
{
    return saturate(max(_Active, _Highlight) * 4.0);
}

// The size of one pixel in world units, for EFX_FillPx. Radial distance is exactly world units.
float ArcPixel(ArcVaryings input)
{
    return length(float2(ddx(input.uvWorld.y), ddy(input.uvWorld.y)));
}

// 1 along the arc's middle, falling to 0 over `width` world units at each end. `x` is along the arc in world
// units (uvWorld.x). Lets spilled shapes die down toward the ends rather than stop at a hard cut.
float ArcEndFade(float x, float width)
{
    return saturate((ArcHalfLength() - abs(x - ArcHalfLength())) / width);
}

ArcVaryings ArcVertex(ArcAttributes input)
{
    ArcVaryings o;
    float3 position = input.positionOS.xyz;
    float2 uv = input.uv;
    float2 uvWorld = input.uvWorld;

    // The arc is built around the gear's centre, so the radial direction is just the vertex direction.
    float radius = length(position.xy);
    float2 radial = position.xy / max(radius, 1e-4);

    // Overflow rows and end columns sit on the band's edge with zero area until the arc is in play, then
    // open outward, inward and past the ends. Idle arcs cost nothing beyond their band.
    float open = ArcSpillOpen();
    float spill = input.overflow.x * open;
    float turn = input.overflow.y * open;
    float c = cos(turn);
    float s = sin(turn);
    radial = float2(radial.x * c - radial.y * s, radial.x * s + radial.y * c);
    position.xy = radial * (radius + spill);
    uv += float2(turn / ArcSweep(), spill / ArcThickness());
    uvWorld += float2(turn * (_ArcShape.y + _ArcShape.z) * 0.5, spill);
    o.positionOS = position.xy;

    // Aiming at an arc pushes it outward, toward the flick; a grant pops it.
    position.xy += radial * (_ArcSwell * _Highlight + ArcFlare() * _ArcFlarePop);

    o.positionCS = TransformObjectToHClip(position);
    o.positionWS = TransformObjectToWorld(position).xy;
    o.color = input.color;
    o.uv = uv;
    o.uvWorld = uvWorld;
    return o;
}

// The state's brightness multiplier. Above 1 is HDR: that's what blooms.
half ArcStateGlow()
{
    return 1.0 + _Highlight * _ArcHighlightBoost + _Active * _ArcActiveBoost;
}

// Where a pixel sits in the arc's own frame, in world units.
struct ArcFrame
{
    float2 p;          // x along the arc from its centre (at this pixel's radius), y out from the band's middle
    float2 halfSize;   // the band's half-length (at this radius) and half-thickness
    float sdf;         // signed distance to the tile's rounded outline; negative inside
};

ArcFrame ArcGetFrame(ArcVaryings input)
{
    ArcFrame f;
    float radius = length(input.positionOS);
    float midRadius = (_ArcShape.y + _ArcShape.z) * 0.5;
    f.p = float2((input.uv.x - 0.5) * ArcSweep() * radius, radius - midRadius);
    f.halfSize = float2(0.5 * ArcSweep() * radius, ArcThickness() * 0.5);
    f.sdf = EFX_SdRoundBox(f.p, f.halfSize, _ArcCornerRadius);
    return f;
}

// A cartoon glint: a fat and a thin slanted stripe sweeping across the tile, once every _ArcShinePeriod /
// rate seconds, each arc on its own beat (offset by its angle). Returns 0..1.
half ArcShine(ArcFrame f, float rate)
{
    const float sweepShare = 0.25;   // of each period, the part spent crossing the tile
    float phase = frac(_Time.y * rate / max(_ArcShinePeriod, 0.1) + _ArcShape.w * 0.159);
    float across = f.halfSize.x + 1.5;
    float x = f.p.x + f.p.y * 0.6 - lerp(-across, across, phase / sweepShare);
    float fat = 1.0 - EFX_Step(_ArcShineWidth, abs(x));
    float thin = 1.0 - EFX_Step(_ArcShineWidth * 0.32, abs(x - 0.5));
    return max(fat, thin) * step(phase, sweepShare);
}

// The shared idle look: a flat cartoon tile in the element's colour — a shaded inner strip, a lit outer
// strip, the odd glint, and an ink outline — brightening into HDR when aimed at or active.
half4 ArcNeutral(ArcVaryings input, ArcFrame f)
{
    half3 base = input.color.rgb;
    float v = input.uv.y;

    half3 rgb = base;
    rgb = lerp(rgb, base * _ArcShadeTone, 1.0 - EFX_Step(_ArcShadeShare, v));
    rgb = lerp(rgb, lerp(base, 1.0, _ArcLitTint), EFX_Step(_ArcLitShare, v));
    rgb = lerp(rgb, 1.0, ArcShine(f, 1.0) * _ArcShineStrength);

    // Brighten, but only until the brightest channel tops out: past that a pale colour (Wind, Light) clips
    // to plain white and loses its hue. The halo carries the rest of the glow.
    half peak = max(max(base.r, base.g), max(base.b, 0.05));
    rgb *= min(ArcStateGlow(), _ArcMaxBrightness / peak);
    rgb = lerp(rgb, lerp(base, 1.0, 0.6) * 3.0, ArcFlare() * _ArcFlareBlaze);   // a granted arc blazes, past white

    half ink = EFX_Step(-_ArcInkWidth, f.sdf);
    rgb = lerp(rgb, base * _ArcInkTone, ink);
    half alpha = EFX_Fill(f.sdf) * lerp(input.color.a, 1.0, ink * 0.6);
    return half4(rgb, alpha);
}

// The aimed-at mark: a glowing outline just outside the ink, in a pale tint of `color`, HDR so it blooms.
half4 ArcHalo(ArcFrame f, half3 color)
{
    half ring = EFX_Fill(f.sdf - _ArcHaloWidth) * EFX_Step(0.0, f.sdf);
    half3 rgb = lerp(color, 1.0, 0.5) * _ArcHaloGlow;
    return half4(rgb, ring * _Highlight);
}

#endif
