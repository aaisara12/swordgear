Shader "Swordgear/Gear Arc Earth"
{
    // Earth's gear section. Idle or aimed at, it's the shared cartoon tile in Earth's colour. Active, the tile
    // turns to rumbling cartoon rock — three wavy strata in ink, split by cracks that glow molten amber in
    // pulses running along them — and chunky boulders tumble up out of it, hop past the gear and drop back,
    // kicking up dust puffs as they launch and land.
    //
    // The loose pieces (the boulders and dust) are particles, not this shader: ArcBitsEarth.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Rock)]
        _TopColor ("Top Stratum", Color) = (0.88, 0.68, 0.38, 1)
        _MidColor ("Middle Stratum", Color) = (0.67, 0.45, 0.23, 1)
        _DeepColor ("Deep Stratum", Color) = (0.43, 0.27, 0.14, 1)
        _InkColor ("Ink", Color) = (0.2, 0.1, 0.04, 1)
        _CrackColor ("Glowing Crack", Color) = (1.0, 0.62, 0.15, 1)
        _CrackGlow ("Crack Glow (HDR)", Range(1, 5)) = 2.6
        _Rumble ("Rumble (world units)", Range(0, 0.3)) = 0.06


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
            #pragma fragment EarthFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half4 _TopColor; half4 _MidColor; half4 _DeepColor; half4 _InkColor; \
                half4 _CrackColor; half _CrackGlow; half _Rumble;
            #include "GearArcCommon.hlsl"

            half4 EarthFragment(ArcVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input);
                half4 tile = EFX_Over(ArcNeutral(input, f), ArcHalo(f, input.color.rgb));

                half energy = ArcEnergy();
                [branch] if (energy < 0.001)
                {
                    return tile;
                }

                float t = _Time.y;
                float thickness = ArcThickness();
                float px = ArcPixel(input);

                // The whole active tile rumbles: everything below is drawn from jittered coordinates.
                float2 rumble = (EFX_Hash22(float2(floor(t * 14.0), 1.3)) - 0.5) * 2.0 * _Rumble * energy;
                float x = input.uvWorld.x + rumble.x;
                float y = input.uvWorld.y + rumble.y;
                float sdf = EFX_SdRoundBox(f.p + rumble, f.halfSize, ArcCornerRadius);

                // Strata: three layers with wavy boundaries, inked seams between them.
                float seam1 = y - (thickness * 0.36 + 0.18 * sin(x * 1.3 + 1.7) + 0.1 * sin(x * 3.1));
                float seam2 = y - (thickness * 0.7 + 0.15 * sin(x * 1.1 + 4.2) + 0.08 * sin(x * 2.7 + 1.0));
                half3 rgb = _DeepColor.rgb;
                rgb = lerp(rgb, _MidColor.rgb, EFX_Step(0.0, seam1));
                rgb = lerp(rgb, _TopColor.rgb, EFX_Step(0.0, seam2));
                float seams = min(abs(EFX_FieldToDistance(seam1, x)), abs(EFX_FieldToDistance(seam2, x)));
                rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_Step(0.05, seams));

                // Cracks: a zig-zag across the rock, glowing molten in pulses that run along it.
                float kink = x / 0.9;
                float k = floor(kink);
                float crackY = thickness * lerp(0.2 + 0.6 * EFX_Hash21(float2(k, 21.7)),
                                                0.2 + 0.6 * EFX_Hash21(float2(k + 1.0, 21.7)), frac(kink));
                float crack = EFX_FieldToDistance(abs(y - crackY), x);
                half pulse = 0.55 + 0.45 * sin(t * 6.0 - x * 0.9);
                rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_Step(0.17, crack));
                rgb = lerp(rgb, _CrackColor.rgb * _CrackGlow * pulse, 1.0 - EFX_Step(0.07, crack));

                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-ArcInkWidth, sdf));
                half4 rock = half4(rgb, EFX_Fill(sdf) * ArcTakeover());

                return EFX_Over(rock, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
