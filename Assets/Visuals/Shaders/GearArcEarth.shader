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
        _RumbleRate ("Rumble Shakes (per second)", Range(1, 60)) = 14.0

        [Header(Strata)]
        _LowerSeamHeight ("Lower Seam Height, deep to middle (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.36
        _UpperSeamHeight ("Upper Seam Height, middle to top (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.7
        _SeamInkWidth ("Seam Ink Width (world units, each side of the seam)", Range(0, 0.3)) = 0.05

        [Header(Lower seam waves)]
        _LowerSeamWave ("Big Wave Height (world units)", Range(0, 1)) = 0.18
        _LowerSeamWaveFreq ("Big Wave Tightness (per world unit, higher = shorter waves)", Range(0, 6)) = 1.3
        _LowerSeamWaveShift ("Big Wave Shift (radians, slides the wave along the arc)", Range(0, 6.3)) = 1.7
        _LowerSeamRipple ("Ripple Height (world units)", Range(0, 0.5)) = 0.1
        _LowerSeamRippleFreq ("Ripple Tightness (per world unit, higher = shorter ripples)", Range(0, 10)) = 3.1

        [Header(Upper seam waves)]
        _UpperSeamWave ("Big Wave Height (world units)", Range(0, 1)) = 0.15
        _UpperSeamWaveFreq ("Big Wave Tightness (per world unit, higher = shorter waves)", Range(0, 6)) = 1.1
        _UpperSeamWaveShift ("Big Wave Shift (radians, slides the wave along the arc)", Range(0, 6.3)) = 4.2
        _UpperSeamRipple ("Ripple Height (world units)", Range(0, 0.5)) = 0.08
        _UpperSeamRippleFreq ("Ripple Tightness (per world unit, higher = shorter ripples)", Range(0, 10)) = 2.7
        _UpperSeamRippleShift ("Ripple Shift (radians, slides the ripples along the arc)", Range(0, 6.3)) = 1.0

        [Header(Cracks)]
        _CrackKinkLength ("Zig-Zag Segment Length (world units)", Range(0.1, 4)) = 0.9
        _CrackLowest ("Lowest Zig-Zag Corner (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.2
        _CrackSpread ("Zig-Zag Corner Height Range (share of the band above the lowest corner)", Range(0, 1)) = 0.6
        _CrackInkWidth ("Crack Ink Width (world units, each side of the crack)", Range(0, 0.5)) = 0.17
        _CrackCoreWidth ("Glowing Core Width (world units, each side of the crack)", Range(0, 0.5)) = 0.07

        [Header(Crack glow pulses)]
        _PulseBase ("Average Brightness (share of Crack Glow)", Range(0, 1)) = 0.55
        _PulseSwing ("Brightness Swing (share of Crack Glow, above and below the average)", Range(0, 1)) = 0.45
        _PulseSpeed ("Pulse Speed (radians per second)", Range(0, 30)) = 6.0
        _PulseTightness ("Pulse Tightness (per world unit, higher = more pulses along the crack)", Range(0, 5)) = 0.9


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
                half4 _CrackColor; half _CrackGlow; half _Rumble; float _RumbleRate; \
                half _LowerSeamHeight; half _UpperSeamHeight; float _SeamInkWidth; \
                float _LowerSeamWave; float _LowerSeamWaveFreq; float _LowerSeamWaveShift; \
                float _LowerSeamRipple; float _LowerSeamRippleFreq; \
                float _UpperSeamWave; float _UpperSeamWaveFreq; float _UpperSeamWaveShift; \
                float _UpperSeamRipple; float _UpperSeamRippleFreq; float _UpperSeamRippleShift; \
                float _CrackKinkLength; half _CrackLowest; half _CrackSpread; \
                float _CrackInkWidth; float _CrackCoreWidth; \
                half _PulseBase; half _PulseSwing; float _PulseSpeed; float _PulseTightness;
            #include "GearArcCommon.hlsl"

            half4 EarthFragment(ArcVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input);
                half4 tile = EFX_Over(ArcNeutral(input, f), ArcHalo(f, input.color.rgb));

                half energy = ArcEnergy();
                [branch] if (energy < 0.001)
                {
                    return GearRecede(tile);
                }

                float t = _Time.y;
                float thickness = ArcThickness();
                float px = ArcPixel(input);

                // The whole active tile rumbles: everything below is drawn from jittered coordinates.
                float2 rumble = (EFX_Hash22(float2(floor(t * _RumbleRate), 1.3)) - 0.5) * 2.0 * _Rumble * energy;
                float x = input.uvWorld.x + rumble.x;
                float y = input.uvWorld.y + rumble.y;
                float sdf = EFX_SdRoundBox(f.p + rumble, f.halfSize, _ArcCornerRadius);

                // Strata: three layers with wavy boundaries, inked seams between them.
                float seam1 = y - (thickness * _LowerSeamHeight
                                   + _LowerSeamWave * sin(x * _LowerSeamWaveFreq + _LowerSeamWaveShift)
                                   + _LowerSeamRipple * sin(x * _LowerSeamRippleFreq));
                float seam2 = y - (thickness * _UpperSeamHeight
                                   + _UpperSeamWave * sin(x * _UpperSeamWaveFreq + _UpperSeamWaveShift)
                                   + _UpperSeamRipple * sin(x * _UpperSeamRippleFreq + _UpperSeamRippleShift));
                half3 rgb = _DeepColor.rgb;
                rgb = lerp(rgb, _MidColor.rgb, EFX_Step(0.0, seam1));
                rgb = lerp(rgb, _TopColor.rgb, EFX_Step(0.0, seam2));
                float seams = min(abs(EFX_FieldToDistance(seam1, x)), abs(EFX_FieldToDistance(seam2, x)));
                rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_Step(_SeamInkWidth, seams));

                // Cracks: a zig-zag across the rock, glowing molten in pulses that run along it.
                float kink = x / _CrackKinkLength;
                float k = floor(kink);
                float crackY = thickness * lerp(_CrackLowest + _CrackSpread * EFX_Hash21(float2(k, 21.7)),
                                                _CrackLowest + _CrackSpread * EFX_Hash21(float2(k + 1.0, 21.7)), frac(kink));
                float crack = EFX_FieldToDistance(abs(y - crackY), x);
                half pulse = _PulseBase + _PulseSwing * sin(t * _PulseSpeed - x * _PulseTightness);
                rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_Step(_CrackInkWidth, crack));
                rgb = lerp(rgb, _CrackColor.rgb * _CrackGlow * pulse, 1.0 - EFX_Step(_CrackCoreWidth, crack));

                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_ArcInkWidth, sdf));
                half4 rock = half4(rgb, EFX_Fill(sdf) * ArcTakeover());

                return GearRecede(EFX_Over(rock, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
