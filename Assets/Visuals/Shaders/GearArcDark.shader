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

        [Header(Void Rim)]
        _RimDepth ("Inner Rim Depth (world units in from the edge)", Range(0, 2)) = 0.5
        _GlowDepth ("Glow Depth (world units in from the edge)", Range(0, 1)) = 0.28

        [Header(Specks)]
        _SpeckCellSize ("Cell Size (world units, one speck chance per cell)", Range(0.2, 2)) = 0.55
        _SpeckScatter ("Scatter From The Cell Centre (share of a cell)", Range(0, 1)) = 0.5
        _SpeckRarity ("Rarity (0-1 share of cells left empty)", Range(0, 1)) = 0.8
        _SpeckSize ("Radius At Full Twinkle (world units)", Range(0, 0.3)) = 0.05
        _TwinkleSpeed ("Twinkle Speed, Slowest (radians per second)", Range(0, 10)) = 1.5
        _TwinkleSpeedSpread ("Twinkle Speed, Extra For The Fastest (radians per second)", Range(0, 10)) = 3.0

        [Header(Eyes)]
        _EyeSpacing ("Pair Spacing (world units, one pair chance per slot)", Range(0.5, 6)) = 2.2
        _EyeRow ("Height, Lowest (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.38
        _EyeRowSpread ("Height, Extra Range (share of the band)", Range(0, 1)) = 0.2
        _EyeApart ("Each Eye From The Pair's Middle (world units)", Range(0, 1)) = 0.27
        _EyeHalfWidth ("Eye Half-Width (world units)", Range(0.02, 0.6)) = 0.21
        _EyeHalfHeight ("Eye Half-Height When Open (world units)", Range(0.02, 0.6)) = 0.15
        _PupilRadius ("Pupil Radius (world units)", Range(0, 0.3)) = 0.08
        _GlanceReachX ("Glance Distance, Sideways (world units)", Range(0, 0.3)) = 0.07
        _GlanceReachY ("Glance Distance, Up-Down (world units)", Range(0, 0.3)) = 0.04
        _GlanceSpeedX ("Glance Speed, Sideways (radians per second)", Range(0, 6)) = 1.3
        _GlanceSpeedY ("Glance Speed, Up-Down (radians per second)", Range(0, 6)) = 0.9
        _BlinkRate ("Blinks (per second)", Range(0, 3)) = 0.45
        _BlinkOpenShare ("Open Share Of Each Blink Cycle (0-1)", Range(0, 1)) = 0.92
        _BlinkShut ("How Far A Blink Shuts The Eye (0-1)", Range(0, 1)) = 0.9
        _EyeHideBelow ("Hide Eyes Less Open Than (0-1)", Range(0.01, 1)) = 0.05
        _EyeEndFade ("Fade Toward The Arc's Ends (world units)", Range(0.05, 4)) = 1.0

        [Header(Tendrils)]
        _TendrilReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 2.8
        _TendrilSpacing ("Spacing (world units)", Range(1.5, 5)) = 1.8
        _TendrilWidth ("Root Half-Width (world units)", Range(0.1, 0.45)) = 0.3
        _Writhe ("Writhe Speed", Range(0, 10)) = 3.5
        _TendrilRoot ("Root Height (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.72
        _TendrilEndFade ("Fade Toward The Arc's Ends (world units)", Range(0.05, 4)) = 1.2

        [Header(Tendril Length)]
        _TendrilBaseLength ("Length Inside The Band (share of the band)", Range(0, 1)) = 0.28
        _TendrilShortest ("Shortest Reach (share of full reach)", Range(0, 1)) = 0.55
        _TendrilLengthSpread ("Extra Reach For The Longest (share of full reach)", Range(0, 1)) = 0.45
        _StretchBase ("Stretch, Average (share of reach)", Range(0, 1.5)) = 0.85
        _StretchSwing ("Stretch, Swing Either Way (share of reach)", Range(0, 1)) = 0.15
        _StretchSpeed ("Stretch Speed (radians per second)", Range(0, 10)) = 2.0

        [Header(Tendril Shape)]
        _WritheWaves ("Wave Along A Tendril (radians, root to tip)", Range(0, 12)) = 4.0
        _Sway ("Sway At The Tip (world units)", Range(0, 1.5)) = 0.35
        _TendrilTaper ("Taper Curve (1 straight, below 1 fuller, above 1 thinner)", Range(0.1, 3)) = 0.8
        _TendrilTipWidth ("Tip Half-Width (world units)", Range(0, 0.2)) = 0.02
        _TendrilEdgeStart ("Lit Edge Starts At (share of the half-width out from the middle)", Range(0, 1)) = 0.35
        _TendrilEdgeGlow ("Lit Edge Brightness (times Glow)", Range(0, 4)) = 1.3

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
                float _RimDepth; float _GlowDepth; \
                float _SpeckCellSize; float _SpeckScatter; float _SpeckRarity; float _SpeckSize; \
                float _TwinkleSpeed; float _TwinkleSpeedSpread; \
                float _EyeSpacing; float _EyeRow; float _EyeRowSpread; float _EyeApart; \
                float _EyeHalfWidth; float _EyeHalfHeight; float _PupilRadius; \
                float _GlanceReachX; float _GlanceReachY; float _GlanceSpeedX; float _GlanceSpeedY; \
                float _BlinkRate; float _BlinkOpenShare; half _BlinkShut; half _EyeHideBelow; float _EyeEndFade; \
                half _TendrilReach; half _TendrilSpacing; half _TendrilWidth; half _Writhe; \
                float _TendrilRoot; float _TendrilEndFade; \
                float _TendrilBaseLength; float _TendrilShortest; float _TendrilLengthSpread; \
                float _StretchBase; float _StretchSwing; float _StretchSpeed; \
                float _WritheWaves; float _Sway; float _TendrilTaper; float _TendrilTipWidth; \
                float _TendrilEdgeStart; half _TendrilEdgeGlow;
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
                rgb = lerp(rgb, _RimColor.rgb, EFX_Step(-_RimDepth, f.sdf));
                rgb = lerp(rgb, _GlowColor.rgb * _Emission, EFX_Step(-_GlowDepth, f.sdf));
                float2 speckCell = floor(float2(x, y) / _SpeckCellSize);
                float speckSeed = EFX_Hash21(speckCell + 3.3);
                float2 speckAt = float2(x, y) - (speckCell + 0.5 + (EFX_Hash22(speckCell) - 0.5) * _SpeckScatter) * _SpeckCellSize;
                half twinkle = 0.5 + 0.5 * sin(t * (_TwinkleSpeed + speckSeed * _TwinkleSpeedSpread) + speckSeed * 30.0);
                half speck = step(_SpeckRarity, speckSeed) * EFX_FillPx(length(speckAt) - _SpeckSize * twinkle, px);
                rgb = lerp(rgb, _GlowColor.rgb * _Emission, speck);

                // Eyes: a pair per slot, opening out of the dark, blinking now and then and glancing about.
                float eyeSlot = floor(x / _EyeSpacing);
                float eyeSeed = EFX_Hash21(float2(eyeSlot, 8.8));
                float2 eyeCentre = float2((eyeSlot + 0.5) * _EyeSpacing, thickness * (_EyeRow + _EyeRowSpread * eyeSeed));
                half blink = step(_BlinkOpenShare, frac(t * _BlinkRate + eyeSeed * 3.0));
                half open = (1.0 - blink * _BlinkShut) * step(1.0 - _EyeDensity, eyeSeed) * ArcEndFade(eyeCentre.x, _EyeEndFade);
                float2 look = float2(sin(t * _GlanceSpeedX + eyeSeed * 7.0), cos(t * _GlanceSpeedY + eyeSeed * 3.0)) * float2(_GlanceReachX, _GlanceReachY);
                float2 fromPair = float2(x, y) - eyeCentre;
                float2 fromEye = float2(abs(fromPair.x) - _EyeApart, fromPair.y);
                float eye = SdEllipse(fromEye, float2(_EyeHalfWidth, max(_EyeHalfHeight * open, 0.01)));
                float pupil = length(fromEye - look) - _PupilRadius;
                half eyeCover = EFX_FillPx(eye, px) * step(_EyeHideBelow, open);
                rgb = lerp(rgb, _EyeColor.rgb * _Emission, eyeCover);
                rgb = lerp(rgb, _InkColor.rgb, eyeCover * EFX_FillPx(pupil, px));

                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_ArcInkWidth, f.sdf));
                half4 abyss = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Tendrils: one per slot, rooted in the void, writhing as a wave runs up them, tapering to a tip.
                float slot = floor(x / _TendrilSpacing);
                float seed = EFX_Hash21(float2(slot, 4.2));
                float centreX = (slot + 0.5) * _TendrilSpacing;
                float rootY = thickness * _TendrilRoot;
                float reach = (thickness * _TendrilBaseLength + _TendrilReach * energy * (_TendrilShortest + _TendrilLengthSpread * seed)
                                  * (_StretchBase + _StretchSwing * sin(t * _StretchSpeed + seed * 9.0)))
                              * ArcEndFade(centreX, _TendrilEndFade);
                float s = saturate((y - rootY) / max(reach, 1e-3));
                float phase = s * _WritheWaves - t * _Writhe + seed * 6.28;
                float sway = _Sway * s;
                float pathX = centreX + sway * sin(phase);
                float slope = _Sway * (sin(phase) + s * _WritheWaves * cos(phase)) / max(reach, 1e-3);
                float across = (x - pathX) * rsqrt(1.0 + slope * slope);
                float halfWidth = _TendrilWidth * pow(1.0 - s, _TendrilTaper) + _TendrilTipWidth;
                float tendril = max(abs(across) - halfWidth, max(rootY - y, y - rootY - reach));
                half4 tendrils = half4(_InkColor.rgb, EFX_FillPx(tendril - _ArcInkWidth, px));
                half3 skin = lerp(_RimColor.rgb, _GlowColor.rgb * _TendrilEdgeGlow, step(across, -halfWidth * _TendrilEdgeStart));   // a lit edge
                tendrils = EFX_Over(half4(skin, EFX_FillPx(tendril, px)), tendrils);
                tendrils.a *= ArcTakeover() * step(0.01, reach);

                return EFX_Over(abyss, EFX_Over(tendrils, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
