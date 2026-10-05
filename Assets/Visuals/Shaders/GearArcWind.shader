Shader "Swordgear/Gear Arc Wind"
{
    // Wind's gear section. Idle or aimed at, it's the shared cartoon tile in Wind's colour. Active, the tile
    // becomes a rushing gale — two-tone gust bands snaking along it — raced through by inked white speed lines
    // that run round the arc and out past the gear, while cartoon swirls spin up out of it, curl outward and
    // vanish.
    //
    // The loose pieces (the swirls) are particles, not this shader: ArcBitsWind.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Gale)]
        _GustSpeed ("Gust Speed (world units / s)", Range(0, 20)) = 7
        _AirColor ("Air", Color) = (0.8, 1.0, 0.82, 1)
        _GustColor ("Gust", Color) = (0.42, 0.85, 0.5, 1)
        _LineColor ("Speed Lines and Swirls", Color) = (1, 1, 1, 1)
        _InkColor ("Ink", Color) = (0.06, 0.3, 0.14, 1)
        _Emission ("Emission", Range(0.5, 3)) = 1.1

        [Header(Gale bands)]
        _GustBandTightness ("Band Tightness Across (per world unit, more is thinner bands)", Range(0, 12)) = 3.2
        _GustSlant ("Band Slant Along The Arc (per world unit, 0 is straight)", Range(-2, 2)) = 0.35
        _GustSnakeTightness ("Snake Tightness Along The Arc (per world unit, more is shorter wiggles)", Range(0, 4)) = 0.9
        _GustSnakeAmount ("Snake Amount (0 is straight bands)", Range(0, 4)) = 1.4
        _GustThreshold ("Gust Tone Cut-off (-1 all gust, 1 all air)", Range(-1, 1)) = 0.35

        [Header(Speed lines)]
        _LineSpeed ("Speed (world units / s)", Range(0, 40)) = 16
        _LineReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 1.6
        _LaneHeight ("Lane Height (world units)", Range(0.1, 2)) = 0.5
        _LaneSpeedMin ("Slowest Lane (share of Speed)", Range(0, 2)) = 0.7
        _LaneSpeedSpread ("Lane Speed Variety (share of Speed added on top)", Range(0, 2)) = 0.6
        _LineLowest ("Lowest Lane Centre (world units out from the inner edge)", Range(-2, 4)) = 0.2
        _LineEndFade ("Fade Toward The Arc Ends (world units)", Range(0.05, 4)) = 1.0

        [Header(Speed line dashes)]
        _DashSpacing ("Dash Slot Length (world units, at most one dash per slot)", Range(0.5, 10)) = 3.2
        _DashLengthMin ("Shortest Dash (world units)", Range(0, 4)) = 0.7
        _DashLengthSpread ("Dash Length Variety (world units added on top)", Range(0, 4)) = 1.3
        _DashSkipChance ("Empty Slot Chance (0-1 chance)", Range(0, 1)) = 0.4
        _LineHalfWidth ("Line Half-Thickness (world units)", Range(0.01, 0.25)) = 0.085
        _LineInkShare ("Line Outline Width (share of the tile ink width)", Range(0, 2)) = 0.6
        _LineGlow ("Line Brightness (times Emission)", Range(0, 4)) = 1.4


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
            #pragma fragment WindFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half _GustSpeed; half4 _AirColor; half4 _GustColor; half4 _LineColor; half4 _InkColor; half _Emission; \
                float _GustBandTightness; float _GustSlant; float _GustSnakeTightness; float _GustSnakeAmount; float _GustThreshold; \
                half _LineSpeed; half _LineReach; \
                float _LaneHeight; float _LaneSpeedMin; float _LaneSpeedSpread; float _LineLowest; float _LineEndFade; \
                float _DashSpacing; float _DashLengthMin; float _DashLengthSpread; float _DashSkipChance; \
                float _LineHalfWidth; float _LineInkShare; half _LineGlow;
            #include "GearArcCommon.hlsl"

            half4 WindFragment(ArcVaryings input) : SV_Target
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

                // The gale: two tones in bands that snake along the tile and rush round it.
                float flow = x - t * _GustSpeed;
                float wave = sin(y * _GustBandTightness + sin(flow * _GustSnakeTightness) * _GustSnakeAmount
                                 + flow * _GustSlant);
                half3 rgb = lerp(_AirColor.rgb, _GustColor.rgb, EFX_Step(_GustThreshold, wave)) * _Emission;
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_ArcInkWidth, f.sdf));
                half4 gale = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Speed lines: rounded dashes in lanes, each lane racing round at its own speed. They fill the
                // tile and the first stretch above it.
                float lane = floor(y / _LaneHeight);
                float laneY = (lane + 0.5) * _LaneHeight;
                float laneSeed = EFX_Hash21(float2(lane, 1.7));
                float run = (x - t * _LineSpeed * (_LaneSpeedMin + _LaneSpeedSpread * laneSeed)) / _DashSpacing
                            + laneSeed * 13.0;
                float dash = floor(run);
                float at = frac(run) * _DashSpacing;
                float dashLength = _DashLengthMin + _DashLengthSpread * EFX_Hash21(float2(dash, lane));
                float2 fromDash = float2(at - clamp(at, 0.3, 0.3 + dashLength), y - laneY);
                float streak = length(fromDash) - _LineHalfWidth;
                half present = step(_DashSkipChance, EFX_Hash21(float2(dash + 5.1, lane)))
                               * step(laneY, thickness + _LineReach * energy) * step(_LineLowest, laneY)
                               * ArcEndFade(x, _LineEndFade);
                half4 streaks = half4(_InkColor.rgb, EFX_FillPx(streak - _ArcInkWidth * _LineInkShare, px));
                streaks = EFX_Over(half4(_LineColor.rgb * _Emission * _LineGlow, EFX_FillPx(streak, px)), streaks);
                streaks.a *= present * lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf));

                return EFX_Over(streaks, EFX_Over(gale, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
