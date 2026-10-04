Shader "Swordgear/Gear Arc Wind"
{
    // Wind's gear section. Idle or aimed at, it's the shared cartoon tile in Wind's colour. Active, the tile
    // becomes a rushing gale — two-tone gust bands snaking along it — raced through by inked white speed lines
    // that run round the arc and out past the gear, while cartoon swirls spin up out of it, curl outward and
    // vanish.
    Properties
    {
        [Header(Gale)]
        _GustSpeed ("Gust Speed (world units / s)", Range(0, 20)) = 7
        _AirColor ("Air", Color) = (0.8, 1.0, 0.82, 1)
        _GustColor ("Gust", Color) = (0.42, 0.85, 0.5, 1)
        _LineColor ("Speed Lines and Swirls", Color) = (1, 1, 1, 1)
        _InkColor ("Ink", Color) = (0.06, 0.3, 0.14, 1)
        _Emission ("Emission", Range(0.5, 3)) = 1.1

        [Header(Speed lines)]
        _LineSpeed ("Speed (world units / s)", Range(0, 40)) = 16
        _LineReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 1.6

        [Header(Swirls)]
        _SwirlReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 2.4
        _SwirlSize ("Size (world units)", Range(0.3, 1.2)) = 0.9
        _SwirlSpin ("Spin (radians / s)", Range(0, 20)) = 7
        _SwirlSpacing ("Spacing (world units)", Range(2.5, 6)) = 3

        [Header(State response)]
        _Swell ("Swell When Aimed (world units)", Range(0, 1)) = 0.35
        _HighlightBoost ("Brightness When Aimed", Range(0, 4)) = 0.9
        _ActiveBoost ("Brightness When Active", Range(0, 4)) = 0.6

        [HideInInspector] _Highlight ("Highlight", Range(0, 1)) = 0
        [HideInInspector] _Active ("Active", Range(0, 1)) = 0
        [HideInInspector] _Fill ("Fill", Range(0, 1)) = 1
        [HideInInspector] _ArcShape ("Arc Shape", Vector) = (0.8, 9.5, 12.5, 0)
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
                half _LineSpeed; half _LineReach; \
                half _SwirlReach; half _SwirlSize; half _SwirlSpin; half _SwirlSpacing;
            #include "GearArcCommon.hlsl"

            // A cartoon swirl: about a turn and a quarter of an Archimedean spiral, spinning, centred on the
            // origin. x = distance to the stroke's centre line (world units), y = 0 at the swirl's eye -> 1 at
            // its tail, so the stroke can swell along its length.
            float2 Swirl(float2 p, float size, float spin)
            {
                const float pitch = 0.13;   // radius gained per radian, in units of size
                const float eye = 1.2;      // where the stroke starts, radians round
                const float tail = 8.6;     // where it ends
                float2 q = p / max(size, 1e-3);
                float theta = atan2(q.y, q.x) - spin;
                float phi = theta + 6.2831853 * round((length(q) / pitch - theta) / 6.2831853);
                phi = clamp(phi, eye, tail);
                float2 onSpiral = pitch * phi * float2(cos(phi + spin), sin(phi + spin));
                return float2(length(q - onSpiral) * size, (phi - eye) / (tail - eye));
            }

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
                float wave = sin(y * 3.2 + sin(flow * 0.9) * 1.4 + flow * 0.35);
                half3 rgb = lerp(_AirColor.rgb, _GustColor.rgb, EFX_Step(0.35, wave)) * _Emission;
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-ArcInkWidth, f.sdf));
                half4 gale = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Speed lines: rounded dashes in lanes, each lane racing round at its own speed. They fill the
                // tile and the first stretch above it.
                const float laneHeight = 0.5;
                const float period = 3.2;
                float lane = floor(y / laneHeight);
                float laneY = (lane + 0.5) * laneHeight;
                float laneSeed = EFX_Hash21(float2(lane, 1.7));
                float run = (x - t * _LineSpeed * (0.7 + 0.6 * laneSeed)) / period + laneSeed * 13.0;
                float dash = floor(run);
                float at = frac(run) * period;
                float dashLength = 0.7 + 1.3 * EFX_Hash21(float2(dash, lane));
                float2 fromDash = float2(at - clamp(at, 0.3, 0.3 + dashLength), y - laneY);
                float streak = length(fromDash) - 0.085;
                half present = step(0.4, EFX_Hash21(float2(dash + 5.1, lane)))
                               * step(laneY, thickness + _LineReach * energy) * step(0.2, laneY)
                               * ArcEndFade(x, 1.0);
                half4 streaks = half4(_InkColor.rgb, EFX_FillPx(streak - ArcInkWidth * 0.6, px));
                streaks = EFX_Over(half4(_LineColor.rgb * _Emission * 1.4, EFX_FillPx(streak, px)), streaks);
                streaks.a *= present * lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf));

                // Swirls: one per slot along the arc, each on its own clock: it spins up out of the tile,
                // curls outward as it grows, and shrinks away.
                float slot = floor(x / _SwirlSpacing);
                float slotSeed = EFX_Hash21(float2(slot, 2.9));
                float life = frac(t * 0.7 + slotSeed);
                float2 swirlCentre = float2((slot + 0.5 + (slotSeed - 0.5) * 0.12) * _SwirlSpacing,
                                            thickness * 0.55 + life * _SwirlReach * energy);
                float swirlSize = _SwirlSize * sin(life * 3.1415927) * ArcEndFade(swirlCentre.x, 1.2) * ArcTakeover();
                float2 swirl = Swirl(float2(x, y) - swirlCentre, swirlSize, t * _SwirlSpin + slotSeed * 6.0);
                float stroke = swirl.x - swirlSize * (0.05 + 0.07 * swirl.y);
                half4 swirls = half4(_InkColor.rgb, EFX_FillPx(stroke - ArcInkWidth * 0.7, px));
                swirls = EFX_Over(half4(_LineColor.rgb * _Emission * 1.5, EFX_FillPx(stroke, px)), swirls);
                swirls.a *= step(0.01, swirlSize);

                return EFX_Over(swirls, EFX_Over(streaks, EFX_Over(gale, tile)));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
