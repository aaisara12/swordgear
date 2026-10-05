Shader "Swordgear/Gear Arc Light"
{
    // Light's gear section. Idle or aimed at, it's the shared cartoon tile in Light's pearl colour. Active,
    // the tile turns to cartoon opal — Opalite's pastel sheen cut into flat bands with white seams, flowing
    // across it — while pastel music notes float up out of it past the gear, wobbling as they rise, and
    // sparkles pop around them: Light is a harp.
    //
    // The sheen runs on world position, as Opalite's does on the notes: as the gear trails the player the
    // bands slide across the stone, which reads as light catching it.
    //
    // The loose pieces (the notes and sparkles) are particles, not this shader: ArcBitsLight.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Opal)]
        _MilkColor ("Milk Colour", Color) = (0.95, 0.93, 0.98, 1)
        _SheenScale ("Sheen Scale (bands per unit)", Range(0.05, 4)) = 0.3
        _SheenSpeed ("Sheen Speed", Range(0, 2)) = 0.5
        _SheenAngle ("Sheen Angle (degrees)", Range(0, 360)) = 35
        _Swirl ("Swirl", Range(0, 1.5)) = 0.45
        _Bands ("Colour Bands Per Cycle", Range(3, 8)) = 5
        _InkColor ("Ink", Color) = (0.32, 0.22, 0.45, 1)
        _Emission ("Emission (HDR)", Range(1, 3)) = 1.25


        [Header(State response)]
        _Swell ("Swell When Aimed (world units)", Range(0, 1)) = 0.35
        _HighlightBoost ("Brightness When Aimed", Range(0, 4)) = 0.9
        _ActiveBoost ("Brightness When Active", Range(0, 4)) = 0.6

        [HideInInspector] _Highlight ("Highlight", Range(0, 1)) = 0
        [HideInInspector] _Active ("Active", Range(0, 1)) = 0
        [HideInInspector] _ArcShape ("Arc Shape", Vector) = (0.8, 9.5, 12.5, 0)
        [HideInInspector] _FlareTime ("Flare Time", Float) = -100
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
                half4 _MilkColor; half _SheenScale; half _SheenSpeed; half _SheenAngle; half _Swirl; half _Bands; \
                half4 _InkColor; half _Emission;
            #include "GearArcCommon.hlsl"

            // Pastel rainbow, as in Opalite.shader: a high floor and a small swing keep it opalescent, not neon.
            half3 Pastel(float t, half swing)
            {
                return 0.78 + swing * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            half4 LightFragment(ArcVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input);
                half4 tile = EFX_Over(ArcNeutral(input, f), ArcHalo(f, input.color.rgb));

                half energy = ArcEnergy();
                [branch] if (energy < 0.001)
                {
                    return tile;
                }

                float t = _Time.y;
                float x = input.uvWorld.x;     // along the arc, world units
                float y = input.uvWorld.y;     // out from the band's inner edge, world units
                float thickness = ArcThickness();
                float px = ArcPixel(input);

                // Opal: Opalite's drifting, swirling sheen, cut into flat pastel bands with white seams.
                float2 p = input.positionWS;
                float angle = radians(_SheenAngle) + t * 0.05;
                float swirl = sin(p.x * 1.7 + t * 0.6) * cos(p.y * 1.3 - t * 0.4) * _Swirl;
                float s = (dot(p, float2(cos(angle), sin(angle))) * _SheenScale + t * _SheenSpeed + swirl) * _Bands;
                half3 rgb = lerp(_MilkColor.rgb, Pastel(floor(s) / _Bands, 0.3), 0.8) * _Emission;
                float seam = min(frac(s), 1.0 - frac(s));
                rgb = lerp(rgb, 1.6, 1.0 - EFX_Step(0.06, seam));
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-ArcInkWidth, f.sdf));
                half4 opal = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                return EFX_Over(opal, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
