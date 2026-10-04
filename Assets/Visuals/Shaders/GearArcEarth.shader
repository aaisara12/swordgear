Shader "Swordgear/Gear Arc Earth"
{
    // Earth's gear section. Idle or aimed at, it's the shared cartoon tile in Earth's colour. Active, the tile
    // turns to rumbling cartoon rock — three wavy strata in ink, split by cracks that glow molten amber in
    // pulses running along them — and chunky boulders tumble up out of it, hop past the gear and drop back,
    // kicking up dust puffs as they launch and land.
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

        [Header(Boulders)]
        _BoulderReach ("Hop Height Past The Gear (world units)", Range(0, 3.5)) = 2.6
        _BoulderSize ("Size (world units)", Range(0.2, 0.6)) = 0.42
        _BoulderSpacing ("Spacing (world units)", Range(1.5, 5)) = 1.9
        _BoulderRate ("Hops Per Second", Range(0.2, 4)) = 1.3
        _DustColor ("Dust", Color) = (0.95, 0.87, 0.7, 1)

        [Header(State response)]
        _Swell ("Swell When Aimed (world units)", Range(0, 1)) = 0.35
        _HighlightBoost ("Brightness When Aimed", Range(0, 4)) = 0.9
        _ActiveBoost ("Brightness When Active", Range(0, 4)) = 0.6

        [HideInInspector] _Highlight ("Highlight", Range(0, 1)) = 0
        [HideInInspector] _Active ("Active", Range(0, 1)) = 0
        [HideInInspector] _Fill ("Fill", Range(0, 1)) = 1
        [HideInInspector] _Urgency ("Urgency", Range(0, 1)) = 0
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
                half4 _CrackColor; half _CrackGlow; half _Rumble; \
                half _BoulderReach; half _BoulderSize; half _BoulderSpacing; half _BoulderRate; half4 _DustColor;
            #include "GearArcCommon.hlsl"

            float2 Rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

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

                rgb = lerp(rgb, _InkColor.rgb, max(EFX_Step(-ArcInkWidth, sdf), ArcChargeEdge(input.uvWorld.x)));
                half4 rock = half4(rgb, EFX_Fill(sdf) * ArcTakeoverAt(input.uvWorld.x));

                // Boulders: one per slot, hopping on its own clock, tumbling as it goes.
                float slot = floor(input.uvWorld.x / _BoulderSpacing);
                float seed = EFX_Hash21(float2(slot, 6.1));
                float life = frac(t * _BoulderRate * (0.7 + 0.6 * seed) + seed);
                float hop = 4.0 * life * (1.0 - life);
                float centreX = (slot + 0.5 + (seed - 0.5) * 0.1) * _BoulderSpacing;
                float endFade = ArcEndFade(centreX, 1.2);
                float2 centre = float2(centreX, thickness * 0.72 + hop * _BoulderReach * energy * (0.6 + 0.4 * seed) * endFade);
                float size = _BoulderSize * (0.75 + 0.4 * EFX_Hash21(float2(slot, 9.9))) * ArcTakeover() * endFade;
                float2 q = Rotate(input.uvWorld - centre, t * (seed - 0.5) * 6.0 + seed * 6.28);
                float boulder = EFX_SdRoundBox(q, float2(size, size * (0.65 + 0.3 * seed)), size * 0.35);
                boulder = max(boulder, dot(q, float2(0.7071, 0.7071)) - size * 0.72);   // a chipped corner
                half lit = step(0.15, dot(q, float2(-0.6, 0.8)));
                half4 boulders = half4(_InkColor.rgb, EFX_FillPx(boulder - ArcInkWidth * 0.8, px));
                boulders = EFX_Over(half4(lerp(_MidColor.rgb, _TopColor.rgb, lit), EFX_FillPx(boulder, px)), boulders);
                boulders.a *= step(0.01, size);

                // Dust: three puffs at the boulder's launch spot, swelling and thinning just after it leaves
                // and as it lands.
                half kick = saturate(max(1.0 - life * 4.0, (life - 0.82) * 5.5));
                float puffRadius = (0.16 + 0.28 * (1.0 - kick)) * endFade;
                float2 dustAt = input.uvWorld - float2(centreX, thickness + 0.05);
                float dust = min(min(length(dustAt - float2(-0.36, 0.0)), length(dustAt - float2(0.36, 0.0))),
                                 length(dustAt - float2(0.0, 0.16))) - puffRadius;
                half4 puffs = half4(_InkColor.rgb, EFX_FillPx(dust - ArcInkWidth * 0.6, px));
                puffs = EFX_Over(half4(_DustColor.rgb, EFX_FillPx(dust, px)), puffs);
                puffs.a *= kick * ArcTakeover() * step(0.01, puffRadius);

                return EFX_Over(boulders, EFX_Over(puffs, EFX_Over(rock, tile)));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
