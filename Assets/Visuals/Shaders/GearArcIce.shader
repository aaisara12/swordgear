Shader "Swordgear/Gear Arc Ice"
{
    // Ice's gear section. Idle or aimed at, it's the shared cartoon tile in Ice's colour. Active, the tile
    // freezes solid — big flat facets split by bright cracks, glass shine
    // stripes racing across — and a crown of cartoon crystal spikes punches out past the gear in a wave that
    // runs round the arc, with twinkle stars popping between them.
    //
    // The loose pieces (the twinkle stars) are particles, not this shader: ArcBitsIce.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Crystal)]
        _SpikeReach ("Spike Reach Past The Gear (world units)", Range(0, 4)) = 2.4
        _SpikeSpacing ("Spike Spacing (world units)", Range(0.4, 3)) = 1.15
        _WaveSpeed ("Spike Wave Speed", Range(0, 12)) = 5
        _LightColor ("Lit Face", Color) = (0.82, 0.97, 1.0, 1)
        _MidColor ("Body", Color) = (0.5, 0.84, 1.0, 1)
        _ShadeColor ("Shaded Face", Color) = (0.27, 0.58, 0.94, 1)
        _InkColor ("Ink", Color) = (0.06, 0.18, 0.42, 1)
        _Emission ("Emission", Range(0.5, 2)) = 1.0

        [Header(Light)]
        _ShineSpeed ("Shine Stripe Speed (world units / s)", Range(0, 20)) = 9

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
            #pragma fragment IceFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half _SpikeReach; half _SpikeSpacing; half _WaveSpeed; \
                half4 _LightColor; half4 _MidColor; half4 _ShadeColor; half4 _InkColor; half _Emission; \
                half _ShineSpeed;
            #include "GearArcCommon.hlsl"

            half4 IceFragment(ArcVaryings input) : SV_Target
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
                float reach = _SpikeReach * energy;

                // Spikes: one per cell along the arc, rooted inside the tile's outer part. Each cell checks its
                // neighbours too, so a leaning spike isn't cut off at its own cell's edge.
                float baseY = thickness * 0.7;
                float id0 = floor(x / _SpikeSpacing);
                float spikes = 1e4;
                float side = 0.0;   // which face of the nearest spike: < 0 lit, > 0 shaded
                [unroll] for (int k = -1; k <= 1; k++)
                {
                    float id = id0 + k;
                    float h1 = EFX_Hash21(float2(id, 3.1));
                    float h2 = EFX_Hash21(float2(id, 7.7));
                    float centre = (id + 0.5 + (h1 - 0.5) * 0.4) * _SpikeSpacing;
                    float pulse = 0.55 + 0.45 * sin(t * _WaveSpeed - id * 1.1);   // a wave running round the arc
                    float grow = ArcEndFade(centre, 1.0);                        // smaller toward the ends
                    float height = thickness * 0.3 + reach * (0.45 + 0.55 * h2) * pulse * grow;
                    float halfWidth = _SpikeSpacing * (0.38 + 0.17 * h1) * (0.4 + 0.6 * grow);
                    float rise = (y - baseY) / max(height, 1e-3);               // 0 at the root, 1 at the tip
                    float axis = centre + (h2 - 0.5) * 0.5 * height * rise;     // leaning
                    float spike = max(abs(x - axis) - halfWidth * (1.0 - rise), baseY - y);
                    float nearer = step(spike, spikes);
                    side = lerp(side, x - axis, nearer);
                    spikes = min(spikes, spike);
                }
                spikes = max(EFX_FieldToDistance(spikes, x), abs(f.p.x) - f.halfSize.x);   // none past the ends
                float ice = min(f.sdf, spikes);   // the frozen tile plus the spikes out of it

                // The frozen tile: big slanted facets in three tones, split by bright cracks.
                float facetCoord = (x * 0.8 + y * 1.3) / 1.7;
                float tone = EFX_Hash21(float2(floor(facetCoord), 5.3));
                half3 rgb = lerp(_ShadeColor.rgb, _MidColor.rgb, step(0.33, tone));
                rgb = lerp(rgb, _LightColor.rgb, step(0.7, tone));
                float crack = 1.0 - EFX_Step(0.05, min(frac(facetCoord), 1.0 - frac(facetCoord)) * 1.7);
                rgb = lerp(rgb, _LightColor.rgb * 1.6, crack);

                // Spikes over it: a lit face and a shaded face either side of a crisp ridge.
                half3 spikeColor = lerp(_LightColor.rgb, _ShadeColor.rgb, EFX_Step(0.0, side));
                rgb = lerp(rgb, spikeColor, EFX_Fill(spikes));

                // Glass shine: a fat and a thin stripe racing across everything, again and again.
                float shine = frac((x + y * 0.7 - t * _ShineSpeed) / 7.0) * 7.0;
                float stripes = max(1.0 - EFX_Step(0.35, shine), EFX_Step(0.6, shine) * (1.0 - EFX_Step(0.72, shine)));
                rgb *= _Emission * (1.0 + 0.2 * _Highlight);
                rgb = lerp(rgb, 2.2, stripes * 0.85);   // the shine is the part that blooms

                // Ink round the whole silhouette and round each spike, so they read as crystals growing out.
                half ink = max(EFX_Step(-ArcInkWidth, ice), EFX_Step(-ArcInkWidth * 0.8, spikes) * EFX_Fill(spikes));
                rgb = lerp(rgb, _InkColor.rgb, ink);

                half4 frozen = half4(rgb, EFX_Fill(ice) * lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf)));

                return EFX_Over(frozen, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
