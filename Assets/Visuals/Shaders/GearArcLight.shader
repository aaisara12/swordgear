Shader "Swordgear/Gear Arc Light"
{
    // Light's gear section: opal. The same stone as Light's notes (Opalite.shader) — a milky body whose
    // colour lives in a pastel sheen drifting across it and in small play-of-colour flecks — but on the arc
    // contract, so it swells, rims and blooms like every other section. Aimed at or active, the flecks
    // blaze into HDR.
    //
    // The sheen runs on world position rather than the arc's UVs, as it does on the notes: as the gear
    // trails the player the sheen slides across the stone, which reads as light catching it.
    Properties
    {
        [Header(Body)]
        _MilkColor ("Milk Colour", Color) = (0.95, 0.93, 0.98, 1)

        [Header(Sheen)]
        _SheenStrength ("Sheen Strength", Range(0, 1)) = 0.55
        _SheenScale ("Sheen Scale (bands per unit)", Range(0.05, 4)) = 0.3
        _SheenSpeed ("Sheen Speed", Range(0, 2)) = 0.18
        _SheenAngle ("Sheen Angle (degrees)", Range(0, 360)) = 35
        _Swirl ("Swirl", Range(0, 1.5)) = 0.45

        [Header(Play of colour)]
        _FleckScale ("Fleck Scale", Range(0.5, 20)) = 1.6
        _FleckThreshold ("Fleck Threshold", Range(0, 1)) = 0.78
        _FleckIntensity ("Fleck Intensity", Range(0, 3)) = 1.1
        _FleckTwinkle ("Fleck Twinkle Speed", Range(0, 3)) = 0.7
        _FleckStateGlow ("Fleck Glow When Aimed / Active", Range(0, 6)) = 2

        [Header(State response)]
        _Swell ("Swell When Aimed (world units)", Range(0, 1)) = 0.35
        _HighlightBoost ("Brightness When Aimed", Range(0, 4)) = 0.6
        _ActiveBoost ("Brightness When Active", Range(0, 4)) = 0.45
        _RimWidth ("Rim Width", Range(0.01, 0.5)) = 0.1
        _RimGlow ("Rim Glow", Range(0, 4)) = 1.0

        [HideInInspector] _Highlight ("Highlight", Range(0, 1)) = 0
        [HideInInspector] _Active ("Active", Range(0, 1)) = 0
        [HideInInspector] _Fill ("Fill", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex ArcVertex
            #pragma fragment LightFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half4 _MilkColor; \
                half _SheenStrength; half _SheenScale; half _SheenSpeed; half _SheenAngle; half _Swirl; \
                half _FleckScale; half _FleckThreshold; half _FleckIntensity; half _FleckTwinkle; half _FleckStateGlow;
            #include "GearArcCommon.hlsl"

            // Pastel rainbow, as in Opalite.shader: a high floor and a small swing keep it opalescent, not neon.
            half3 Pastel(float t, half swing)
            {
                return 0.78 + swing * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            half4 LightFragment(ArcVaryings input) : SV_Target
            {
                float2 p = input.positionWS;
                float t = _Time.y;

                // Sheen: bands along a slowly drifting direction, bent by a gentle swirl.
                float angle = radians(_SheenAngle) + t * 0.05;
                float2 dir = float2(cos(angle), sin(angle));
                float swirl = sin(p.x * 1.7 + t * 0.6) * cos(p.y * 1.3 - t * 0.4) * _Swirl;
                float s = dot(p, dir) * _SheenScale + t * _SheenSpeed + swirl;

                half3 sheen = Pastel(s, 0.22);
                half sheenMix = _SheenStrength * (0.55 + 0.45 * sin(s * 3.1415927 + 1.3));
                half3 rgb = lerp(_MilkColor.rgb, sheen, saturate(sheenMix));

                // Play of colour: sparse saturated flashes that drift and twinkle, flaring when the arc is in play.
                float n = EFX_ValueNoise(p * _FleckScale + float2(t * 0.11, -t * 0.07));
                half twinkle = 0.6 + 0.4 * sin(t * _FleckTwinkle * 6.2831853 + n * 12.0);
                half fleck = smoothstep(_FleckThreshold, 1.0, n) * twinkle;
                half3 fleckColour = Pastel(s * 1.7 + n * 2.0 + 0.25, 0.45);
                rgb += fleckColour * fleck * _FleckIntensity * (1.0 + _FleckStateGlow * max(_Highlight, _Active));

                rgb += sheen * ArcRim(input.uv.y);
                rgb *= ArcStateGlow();
                return half4(rgb, input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
