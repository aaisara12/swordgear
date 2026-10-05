Shader "Swordgear/Gear Arc Fire"
{
    // Fire's gear section. Idle or aimed at, it's the shared cartoon tile in Fire's colour. Active, the whole
    // tile catches: cartoon fire — nested red, orange and yellow
    // tongues inside an ink outline — roars up past the gear, every tongue jumping to its own height, while
    // flame bits break off and fly outward.
    //
    // The loose pieces (the flame bits) are particles, not this shader: ArcBitsFire.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Flames)]
        _FlameReach ("Reach Past The Gear (world units)", Range(0, 4)) = 3.3
        _TongueWidth ("Tongue Width (world units)", Range(0.3, 3)) = 1.7
        _Flicker ("Flicker Speed (new heights per second)", Range(0, 12)) = 6
        _Sway ("Sway (tongue widths)", Range(0, 1)) = 0.3
        _OuterColor ("Outer", Color) = (0.95, 0.2, 0.06, 1)
        _MidColor ("Middle", Color) = (1.0, 0.52, 0.07, 1)
        _CoreColor ("Core", Color) = (1.0, 0.9, 0.35, 1)
        _InkColor ("Ink", Color) = (0.3, 0.03, 0.02, 1)
        _Emission ("Emission (HDR)", Range(1, 4)) = 1.2
        _AimedBrightness ("Extra Brightness When Aimed At (share, 0.3 = a third brighter)", Range(0, 2)) = 0.3

        [Header(Reach)]
        _ReachSteady ("Steady Reach (share of Reach Past The Gear)", Range(0, 1.5)) = 0.9
        _ReachThrob ("Throbbing Reach (share of Reach Past The Gear)", Range(0, 1)) = 0.1
        _ReachThrobSpeed ("Throb Speed (radians per second, 6.28 = once a second)", Range(0, 30)) = 9.0
        _EndFade ("Die Down Toward The Ends (world units)", Range(0.05, 5)) = 1.2

        [Header(Tongue Shape)]
        _TongueShape ("Tongue Shape (under 1 bulges the sides, over 1 pinches them)", Range(0.1, 3)) = 0.65
        _TongueMinHeight ("Shortest Tongue (share of the reach)", Range(0, 1)) = 0.3
        _TongueHeightSpread ("Random Extra Height (share of the reach)", Range(0, 1)) = 0.7

        [Header(Tongue Motion)]
        _FlickerRateMin ("Slowest Tongue Flicker (share of Flicker Speed)", Range(0, 2)) = 0.8
        _FlickerRateSpread ("Flicker Spread Between Tongues (share of Flicker Speed)", Range(0, 2)) = 0.4
        _SwaySpeed ("Sway Speed (radians per second, 6.28 = once a second)", Range(0, 10)) = 2.3
        _SwayRipple ("Sway Ripple Along The Arc (radians per world unit)", Range(0, 4)) = 0.8

        [Header(Second Row Of Tongues)]
        _SecondRowHeight ("Second Row Height (share of the first row)", Range(0, 1.5)) = 0.8
        _SecondRowWidth ("Second Row Tongue Width (share of Tongue Width)", Range(0.1, 2)) = 0.71
        _SecondRowShift ("Second Row Shift Along The Arc (share of Tongue Width)", Range(0, 1)) = 0.37
        _SecondRowSpeed ("Second Row Animation Speed (share of the first row)", Range(0, 3)) = 1.13

        [Header(Flame Layers)]
        _OuterFlameBase ("Outer Flame Height Between Tongues (share of the band, 0 inner - 1 outer)", Range(0, 1.5)) = 0.9
        _MidFlameBase ("Middle Flame Height Between Tongues (share of the band, 0 inner - 1 outer)", Range(0, 1.5)) = 0.6
        _MidFlameReach ("Middle Flame Reach (share of the outer flame reach)", Range(0, 1.5)) = 0.6
        _CoreFlameBase ("Core Flame Height Between Tongues (share of the band, 0 inner - 1 outer)", Range(0, 1.5)) = 0.3
        _CoreFlameReach ("Core Flame Reach (share of the outer flame reach)", Range(0, 1.5)) = 0.28
        _FlameFloor ("Flames Rise From (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.5


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
            #pragma fragment FireFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half _FlameReach; half _TongueWidth; half _Flicker; half _Sway; \
                half4 _OuterColor; half4 _MidColor; half4 _CoreColor; half4 _InkColor; half _Emission; \
                half _AimedBrightness; \
                half _ReachSteady; half _ReachThrob; float _ReachThrobSpeed; float _EndFade; \
                half _TongueShape; half _TongueMinHeight; half _TongueHeightSpread; \
                float _FlickerRateMin; float _FlickerRateSpread; float _SwaySpeed; float _SwayRipple; \
                half _SecondRowHeight; float _SecondRowWidth; float _SecondRowShift; float _SecondRowSpeed; \
                half _OuterFlameBase; half _MidFlameBase; half _MidFlameReach; half _CoreFlameBase; half _CoreFlameReach; \
                half _FlameFloor;
            #include "GearArcCommon.hlsl"

            // A row of cartoon flame tongues: the height at x, 0..1. Pointed tips with bulging sides, each
            // tongue jumping between random heights on its own clock, the row swaying from side to side.
            float Tongues(float x, float t, float width, float seed)
            {
                float s = x / width + sin(t * _SwaySpeed + x * _SwayRipple + seed) * _Sway;
                float id = floor(s);
                float across = abs(frac(s) - 0.5) * 2.0;   // 0 at a tongue's centre, 1 between tongues
                float clock = t * _Flicker * (_FlickerRateMin + _FlickerRateSpread * EFX_Hash21(float2(id, seed)))
                              + EFX_Hash21(float2(seed, id)) * 9.0;
                float tick = floor(clock);
                float height = lerp(EFX_Hash21(float2(id, tick + seed)),
                                    EFX_Hash21(float2(id, tick + 1.0 + seed)),
                                    smoothstep(0.0, 1.0, frac(clock)));
                return pow(saturate(1.0 - across), _TongueShape) * (_TongueMinHeight + _TongueHeightSpread * height);
            }

            half4 FireFragment(ArcVaryings input) : SV_Target
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

                // Flames die down toward the arc's ends rather than stopping at a hard radial cut.
                float endFade = ArcEndFade(x, _EndFade);
                float reach = _FlameReach * energy * endFade * (_ReachSteady + _ReachThrob * sin(t * _ReachThrobSpeed));
                float field = max(Tongues(x, t, _TongueWidth, 0.0),
                                  _SecondRowHeight * Tongues(x + _SecondRowShift * _TongueWidth, t * _SecondRowSpeed,
                                                             _TongueWidth * _SecondRowWidth, 17.0));

                // Three nested flames from the inner edge up: yellow core, orange middle, red outer.
                float outerTop = thickness * _OuterFlameBase + reach * field;
                float midTop = thickness * _MidFlameBase + reach * _MidFlameReach * field;
                float coreTop = thickness * _CoreFlameBase + reach * _CoreFlameReach * field;

                float flame = EFX_FieldToDistance(y - outerTop, x);
                flame = max(flame, max(thickness * _FlameFloor - y, abs(f.p.x) - f.halfSize.x));
                float fire = min(f.sdf, flame);   // the burning tile plus the tongues above it

                half3 rgb = _CoreColor.rgb;
                rgb = lerp(rgb, _MidColor.rgb, EFX_Step(0.0, y - coreTop));
                rgb = lerp(rgb, _OuterColor.rgb, EFX_Step(0.0, y - midTop));
                rgb *= _Emission * (1.0 + _AimedBrightness * _Highlight);
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_ArcInkWidth, fire));

                // Above the tile the flames show as soon as they reach; inside it they take over a beat later.
                half4 flames = half4(rgb, EFX_Fill(fire) * lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf)));

                return EFX_Over(flames, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
