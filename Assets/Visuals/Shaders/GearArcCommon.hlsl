// The contract every gear-arc shader shares: the arc mesh's vertex data, the outward swell, and the arc's
// state (highlight / active / fill) as GearArcVisual drives it through a MaterialPropertyBlock.
//
// Each element's arc shader includes this, declares its own look in GEAR_ARC_MATERIAL_PROPERTIES, and only
// writes a fragment function. That keeps the states identical across elements: an aimed-at Fire arc and an
// aimed-at Ice arc swell and brighten the same way, so the ring reads as one object.
//
// Arc mesh channels (GearArcVisual.Rebuild):
//   COLOR      the arc's element colour and alpha (eased on the CPU)
//   TEXCOORD0  u along the arc 0..1, v inner edge 0 -> outer edge 1
//   TEXCOORD1  the same in world units (arc length, thickness), for patterns that mustn't stretch
#ifndef SWORDGEAR_GEAR_ARC_COMMON_INCLUDED
#define SWORDGEAR_GEAR_ARC_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "ElementFX.hlsl"

#ifndef GEAR_ARC_MATERIAL_PROPERTIES
#define GEAR_ARC_MATERIAL_PROPERTIES
#endif

CBUFFER_START(UnityPerMaterial)
    // State, per arc (MaterialPropertyBlock). 0..1, eased by GearArcVisual.
    half _Highlight;
    half _Active;
    half _Fill;

    // Shared look.
    float _Swell;
    half _HighlightBoost;
    half _ActiveBoost;
    half _RimWidth;
    half _RimGlow;

    GEAR_ARC_MATERIAL_PROPERTIES
CBUFFER_END

struct ArcAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 uvWorld : TEXCOORD1;
};

struct ArcVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 uvWorld : TEXCOORD1;
    float2 positionWS : TEXCOORD2;
};

ArcVaryings ArcVertex(ArcAttributes input)
{
    ArcVaryings o;
    float3 position = input.positionOS.xyz;

    // The arc is built around the gear's centre, so the radial direction is just the vertex direction.
    // Aiming at an arc pushes it outward, toward the flick.
    float2 radial = position.xy / max(length(position.xy), 1e-4);
    position.xy += radial * (_Swell * _Highlight);

    o.positionCS = TransformObjectToHClip(position);
    o.positionWS = TransformObjectToWorld(position).xy;
    o.color = input.color;
    o.uv = input.uv;
    o.uvWorld = input.uvWorld;
    return o;
}

// The state's brightness multiplier. Above 1 is HDR: that's what blooms.
half ArcStateGlow()
{
    return 1.0 + _Highlight * _HighlightBoost + _Active * _ActiveBoost;
}

// Bright edges on both sides of the band, strongest when aimed at or active.
half ArcRim(float v)
{
    return EFX_Rim(v, _RimWidth) * _RimGlow * (0.5 + 0.5 * max(_Highlight, _Active));
}

#endif
