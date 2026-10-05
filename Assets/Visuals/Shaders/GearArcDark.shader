Shader "Swordgear/Gear Arc Dark"
{
    // Dark's gear section. Idle or aimed at, it's the shared cartoon tile in Dark's colour. Active, the tile
    // opens into an inky void — a glowing violet rim, specks twinkling deep inside, and pairs of cartoon eyes
    // that blink and glance about — while two-tone tendrils writhe up out of it and reach past the gear.
    Properties
    {
        [Header(Void)]
        _VoidColor ("Void", Color) = (0.07, 0.02, 0.12, 1)
        _RimColor ("Inner Rim", Color) = (0.42, 0.13, 0.62, 1)
        _GlowColor ("Glow", Color) = (0.82, 0.45, 1.0, 1)
        _EyeColor ("Eyes", Color) = (1.0, 0.93, 0.55, 1)
        _InkColor ("Ink", Color) = (0.03, 0.0, 0.06, 1)
        _Emission ("Glow Emission (HDR)", Range(1, 4)) = 2
        _EyeDensity ("Eye Density", Range(0, 1)) = 0.6

        [Header(Tendrils)]
        _TendrilReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 2.8
        _TendrilSpacing ("Spacing (world units)", Range(1.5, 5)) = 1.8
        _TendrilWidth ("Root Half-Width (world units)", Range(0.1, 0.45)) = 0.3
        _Writhe ("Writhe Speed", Range(0, 10)) = 3.5

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
            #pragma fragment DarkFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half4 _VoidColor; half4 _RimColor; half4 _GlowColor; half4 _EyeColor; half4 _InkColor; \
                half _Emission; half _EyeDensity; \
                half _TendrilReach; half _TendrilSpacing; half _TendrilWidth; half _Writhe;
            #include "GearArcCommon.hlsl"

            // Signed distance to an ellipse of radii r (approximate, good near its edge).
            float SdEllipse(float2 p, float2 r)
            {
                return (length(p / r) - 1.0) * min(r.x, r.y);
            }

            half4 DarkFragment(ArcVaryings input) : SV_Target
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

                // The void: near-black, a glowing violet rim just inside the ink, specks twinkling in its depths.
                half3 rgb = _VoidColor.rgb;
                rgb = lerp(rgb, _RimColor.rgb, EFX_Step(-0.5, f.sdf));
                rgb = lerp(rgb, _GlowColor.rgb * _Emission, EFX_Step(-0.28, f.sdf));
                float2 speckCell = floor(float2(x, y) / 0.55);
                float speckSeed = EFX_Hash21(speckCell + 3.3);
                float2 speckAt = float2(x, y) - (speckCell + 0.5 + (EFX_Hash22(speckCell) - 0.5) * 0.5) * 0.55;
                half twinkle = 0.5 + 0.5 * sin(t * (1.5 + speckSeed * 3.0) + speckSeed * 30.0);
                half speck = step(0.8, speckSeed) * EFX_FillPx(length(speckAt) - 0.05 * twinkle, px);
                rgb = lerp(rgb, _GlowColor.rgb * _Emission, speck);

                // Eyes: a pair per slot, opening out of the dark, blinking now and then and glancing about.
                float eyeSlot = floor(x / 2.2);
                float eyeSeed = EFX_Hash21(float2(eyeSlot, 8.8));
                float2 eyeCentre = float2((eyeSlot + 0.5) * 2.2, thickness * (0.38 + 0.2 * eyeSeed));
                half blink = step(0.92, frac(t * 0.45 + eyeSeed * 3.0));
                half open = (1.0 - blink * 0.9) * step(1.0 - _EyeDensity, eyeSeed) * ArcEndFade(eyeCentre.x, 1.0);
                float2 look = float2(sin(t * 1.3 + eyeSeed * 7.0), cos(t * 0.9 + eyeSeed * 3.0)) * float2(0.07, 0.04);
                float2 fromPair = float2(x, y) - eyeCentre;
                float2 fromEye = float2(abs(fromPair.x) - 0.27, fromPair.y);
                float eye = SdEllipse(fromEye, float2(0.21, max(0.15 * open, 0.01)));
                float pupil = length(fromEye - look) - 0.08;
                half eyeCover = EFX_FillPx(eye, px) * step(0.05, open);
                rgb = lerp(rgb, _EyeColor.rgb * _Emission, eyeCover);
                rgb = lerp(rgb, _InkColor.rgb, eyeCover * EFX_FillPx(pupil, px));

                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-ArcInkWidth, f.sdf));
                half4 abyss = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Tendrils: one per slot, rooted in the void, writhing as a wave runs up them, tapering to a tip.
                float slot = floor(x / _TendrilSpacing);
                float seed = EFX_Hash21(float2(slot, 4.2));
                float centreX = (slot + 0.5) * _TendrilSpacing;
                float rootY = thickness * 0.72;
                float reach = (thickness * 0.28 + _TendrilReach * energy * (0.55 + 0.45 * seed) * (0.85 + 0.15 * sin(t * 2.0 + seed * 9.0)))
                              * ArcEndFade(centreX, 1.2);
                float s = saturate((y - rootY) / max(reach, 1e-3));
                float phase = s * 4.0 - t * _Writhe + seed * 6.28;
                float sway = 0.35 * s;
                float pathX = centreX + sway * sin(phase);
                float slope = 0.35 * (sin(phase) + s * 4.0 * cos(phase)) / max(reach, 1e-3);
                float across = (x - pathX) * rsqrt(1.0 + slope * slope);
                float halfWidth = _TendrilWidth * pow(1.0 - s, 0.8) + 0.02;
                float tendril = max(abs(across) - halfWidth, max(rootY - y, y - rootY - reach));
                half4 tendrils = half4(_InkColor.rgb, EFX_FillPx(tendril - ArcInkWidth, px));
                half3 skin = lerp(_RimColor.rgb, _GlowColor.rgb * 1.3, step(across, -halfWidth * 0.35));   // a lit edge
                tendrils = EFX_Over(half4(skin, EFX_FillPx(tendril, px)), tendrils);
                tendrils.a *= ArcTakeover() * step(0.01, reach);

                return EFX_Over(abyss, EFX_Over(tendrils, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
