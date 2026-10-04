Shader "Swordgear/Gear Arc Fire"
{
    // Fire's gear section. Idle or aimed at, it's the shared cartoon tile in Fire's colour. Active, the whole
    // tile catches: cartoon fire — nested red, orange and yellow
    // tongues inside an ink outline — roars up past the gear, every tongue jumping to its own height, while
    // flame bits break off and fly outward.
    Properties
    {
        [Header(Flames)]
        _FlameReach ("Reach Past The Gear (world units)", Range(0, 4)) = 3.3
        _TongueWidth ("Tongue Width (world units)", Range(0.3, 3)) = 1.7
        _Flicker ("Flicker Speed", Range(0, 12)) = 6
        _Sway ("Sway", Range(0, 1)) = 0.3
        _OuterColor ("Outer", Color) = (0.95, 0.2, 0.06, 1)
        _MidColor ("Middle", Color) = (1.0, 0.52, 0.07, 1)
        _CoreColor ("Core", Color) = (1.0, 0.9, 0.35, 1)
        _InkColor ("Ink", Color) = (0.3, 0.03, 0.02, 1)
        _Emission ("Emission (HDR)", Range(1, 4)) = 1.2

        [Header(Flame bits)]
        _BitDensity ("Density", Range(0, 1)) = 0.45
        _BitSpeed ("Rise Speed (world units / s)", Range(0, 8)) = 3

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
            #pragma fragment FireFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half _FlameReach; half _TongueWidth; half _Flicker; half _Sway; \
                half4 _OuterColor; half4 _MidColor; half4 _CoreColor; half4 _InkColor; half _Emission; \
                half _BitDensity; half _BitSpeed;
            #include "GearArcCommon.hlsl"

            // A row of cartoon flame tongues: the height at x, 0..1. Pointed tips with bulging sides, each
            // tongue jumping between random heights on its own clock, the row swaying from side to side.
            float Tongues(float x, float t, float width, float seed)
            {
                float s = x / width + sin(t * 2.3 + x * 0.8 + seed) * _Sway;
                float id = floor(s);
                float across = abs(frac(s) - 0.5) * 2.0;   // 0 at a tongue's centre, 1 between tongues
                float clock = t * _Flicker * (0.8 + 0.4 * EFX_Hash21(float2(id, seed)))
                              + EFX_Hash21(float2(seed, id)) * 9.0;
                float tick = floor(clock);
                float height = lerp(EFX_Hash21(float2(id, tick + seed)),
                                    EFX_Hash21(float2(id, tick + 1.0 + seed)),
                                    smoothstep(0.0, 1.0, frac(clock)));
                return pow(saturate(1.0 - across), 0.65) * (0.3 + 0.7 * height);
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
                float endFade = ArcEndFade(x, 1.2);
                float reach = _FlameReach * energy * endFade * (0.9 + 0.1 * sin(t * 9.0));
                float field = max(Tongues(x, t, _TongueWidth, 0.0),
                                  0.8 * Tongues(x + 0.37 * _TongueWidth, t * 1.13, _TongueWidth * 0.71, 17.0));

                // Three nested flames from the inner edge up: yellow core, orange middle, red outer.
                float outerTop = thickness * 0.9 + reach * field;
                float midTop = thickness * 0.6 + reach * 0.6 * field;
                float coreTop = thickness * 0.3 + reach * 0.28 * field;

                float flame = EFX_FieldToDistance(y - outerTop, x);
                flame = max(flame, max(thickness * 0.5 - y, abs(f.p.x) - f.halfSize.x));
                float fire = min(f.sdf, flame);   // the burning tile plus the tongues above it

                half3 rgb = _CoreColor.rgb;
                rgb = lerp(rgb, _MidColor.rgb, EFX_Step(0.0, y - coreTop));
                rgb = lerp(rgb, _OuterColor.rgb, EFX_Step(0.0, y - midTop));
                rgb *= _Emission * (1.0 + 0.3 * _Highlight);
                rgb = lerp(rgb, _InkColor.rgb, max(EFX_Step(-ArcInkWidth, fire), ArcChargeEdge(x)));

                // Above the tile the flames show as soon as they reach; inside it they take over a beat later,
                // and only where the imbue hasn't drained.
                half4 flames = half4(rgb, EFX_Fill(fire) * lerp(ArcTakeoverAt(x), 1.0, EFX_Step(0.0, f.sdf)));

                // Flame bits: blobs that break off the tips and fly outward, shrinking as they go.
                float2 bitCell = float2(x / 1.0, (y - t * _BitSpeed) / 1.3);
                float2 cell = floor(bitCell);
                float seed = EFX_Hash21(cell + 31.7);
                float2 centre = (cell + 0.5 + (EFX_Hash22(cell) - 0.5) * 0.5) * float2(1.0, 1.3) + float2(0.0, t * _BitSpeed);
                float lift = saturate((centre.y - thickness) / max(_FlameReach + 1.0, 0.1));
                float radius = 0.34 * (1.0 - lift) * step(1.0 - _BitDensity, seed) * ArcTakeover() * ArcEndFade(centre.x, 1.2);
                float2 d = float2(x, y) - centre;
                float bit = length(float2(d.x, d.y > 0.0 ? d.y : d.y * 0.55)) - radius;   // an egg, trailing downward
                half3 bitColor = lerp(_CoreColor.rgb, _MidColor.rgb, EFX_Step(-radius * 0.45, bit)) * _Emission * 1.3;
                // Gated to the bit's own disc: a cell's edge is a jump in the field, which would otherwise fringe.
                half4 bits = half4(bitColor, EFX_Fill(bit) * step(length(d), radius * 2.0) * step(thickness * 0.8, y));

                return EFX_Over(bits, EFX_Over(flames, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
