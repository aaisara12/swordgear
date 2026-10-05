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
        _WaveSpeed ("Spike Wave Speed (radians / s)", Range(0, 12)) = 5
        _LightColor ("Lit Face", Color) = (0.82, 0.97, 1.0, 1)
        _MidColor ("Body", Color) = (0.5, 0.84, 1.0, 1)
        _ShadeColor ("Shaded Face", Color) = (0.27, 0.58, 0.94, 1)
        _InkColor ("Ink", Color) = (0.06, 0.18, 0.42, 1)
        _Emission ("Emission", Range(0.5, 2)) = 1.0

        [Header(Spike Shape)]
        _SpikeRootHeight ("Spikes Grow From (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.7
        _SpikeBaseHeight ("Height Every Spike Has (share of the band thickness)", Range(0, 2)) = 0.3
        _SpikeHeightMin ("Shortest Spike (share of the reach)", Range(0, 1)) = 0.45
        _SpikeHeightSpread ("Extra Height At Random (share of the reach, 0 to this)", Range(0, 1)) = 0.55
        _SpikeWidth ("Spike Half-Width (share of the spacing)", Range(0, 1)) = 0.38
        _SpikeWidthSpread ("Extra Half-Width At Random (share of the spacing, 0 to this)", Range(0, 0.5)) = 0.17
        _SpikeJitter ("Position Jitter (share of the spacing)", Range(0, 1)) = 0.4
        _SpikeLean ("Lean (tip tilt, share of the spike height)", Range(0, 2)) = 0.5
        _SpikeInkShare ("Spike Outline Width (share of the outer outline)", Range(0, 2)) = 0.8

        [Header(Spike Wave)]
        _WaveMiddle ("Wave Middle (share of a spike full height)", Range(0, 1)) = 0.55
        _WaveSwing ("Wave Swing (share of full height, up and down)", Range(0, 1)) = 0.45
        _WaveLag ("Wave Lag Between Neighbours (radians)", Range(0, 6.3)) = 1.1

        [Header(Spikes Near The Ends)]
        _SpikeEndFade ("Spikes Shrink Over, At Each End (world units)", Range(0.01, 6)) = 1.0
        _SpikeEndWidth ("Width Right At The Ends (share of full width)", Range(0, 1)) = 0.4
        _SpikeEndWidthGain ("Width Gained Away From The Ends (share of full width)", Range(0, 1)) = 0.6

        [Header(Frozen Tile Facets)]
        _FacetSlantAlong ("Facet Slant, Along The Arc (weight)", Range(-3, 3)) = 0.8
        _FacetSlantOut ("Facet Slant, Out From The Gear (weight)", Range(-3, 3)) = 1.3
        _FacetSize ("Facet Size (world units, before the slant weights)", Range(0.2, 6)) = 1.7
        _FacetShadeChance ("Shaded Facets (0-1 chance)", Range(0, 1)) = 0.33
        _FacetLitFrom ("Lit Facets Above (0-1 threshold, 1 minus this is the lit chance)", Range(0, 1)) = 0.7
        _CrackWidth ("Crack Width Each Side (world units, before the slant weights)", Range(0, 0.5)) = 0.05
        _CrackGlow ("Crack Brightness (times the lit face colour)", Range(0, 4)) = 1.6

        [Header(Light)]
        _ShineSpeed ("Shine Stripe Speed (world units / s)", Range(0, 20)) = 9
        _ShineSlant ("Shine Stripe Slant (0 is straight across the band)", Range(-3, 3)) = 0.7
        _ShineRepeat ("Shine Repeats Every (world units)", Range(1, 30)) = 7.0
        _ShineFatWidth ("Fat Stripe Width (world units)", Range(0, 3)) = 0.35
        _ShineThinStart ("Thin Stripe Starts (world units from the fat stripe start)", Range(0, 6)) = 0.6
        _ShineThinEnd ("Thin Stripe Ends (world units from the fat stripe start)", Range(0, 6)) = 0.72
        _ShineBrightness ("Shine Brightness (HDR, above 1 blooms)", Range(0, 6)) = 2.2
        _ShineStrength ("Shine Strength (0-1 cover)", Range(0, 1)) = 0.85
        _AimGlow ("Extra Brightness When Aimed At (share, on top of Emission)", Range(0, 2)) = 0.2

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
                half _SpikeRootHeight; half _SpikeBaseHeight; half _SpikeHeightMin; half _SpikeHeightSpread; \
                half _SpikeWidth; half _SpikeWidthSpread; half _SpikeJitter; half _SpikeLean; half _SpikeInkShare; \
                half _WaveMiddle; half _WaveSwing; float _WaveLag; \
                float _SpikeEndFade; half _SpikeEndWidth; half _SpikeEndWidthGain; \
                float _FacetSlantAlong; float _FacetSlantOut; float _FacetSize; half _FacetShadeChance; half _FacetLitFrom; \
                float _CrackWidth; half _CrackGlow; \
                half _ShineSpeed; float _ShineSlant; float _ShineRepeat; \
                float _ShineFatWidth; float _ShineThinStart; float _ShineThinEnd; \
                half _ShineBrightness; half _ShineStrength; half _AimGlow;
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
                float baseY = thickness * _SpikeRootHeight;
                float id0 = floor(x / _SpikeSpacing);
                float spikes = 1e4;
                float side = 0.0;   // which face of the nearest spike: < 0 lit, > 0 shaded
                [unroll] for (int k = -1; k <= 1; k++)
                {
                    float id = id0 + k;
                    float h1 = EFX_Hash21(float2(id, 3.1));
                    float h2 = EFX_Hash21(float2(id, 7.7));
                    float centre = (id + 0.5 + (h1 - 0.5) * _SpikeJitter) * _SpikeSpacing;
                    float pulse = _WaveMiddle + _WaveSwing * sin(t * _WaveSpeed - id * _WaveLag);   // a wave running round the arc
                    float grow = ArcEndFade(centre, _SpikeEndFade);                        // smaller toward the ends
                    float height = thickness * _SpikeBaseHeight + reach * (_SpikeHeightMin + _SpikeHeightSpread * h2) * pulse * grow;
                    float halfWidth = _SpikeSpacing * (_SpikeWidth + _SpikeWidthSpread * h1) * (_SpikeEndWidth + _SpikeEndWidthGain * grow);
                    float rise = (y - baseY) / max(height, 1e-3);               // 0 at the root, 1 at the tip
                    float axis = centre + (h2 - 0.5) * _SpikeLean * height * rise;     // leaning
                    float spike = max(abs(x - axis) - halfWidth * (1.0 - rise), baseY - y);
                    float nearer = step(spike, spikes);
                    side = lerp(side, x - axis, nearer);
                    spikes = min(spikes, spike);
                }
                spikes = max(EFX_FieldToDistance(spikes, x), abs(f.p.x) - f.halfSize.x);   // none past the ends
                float ice = min(f.sdf, spikes);   // the frozen tile plus the spikes out of it

                // The frozen tile: big slanted facets in three tones, split by bright cracks.
                float facetCoord = (x * _FacetSlantAlong + y * _FacetSlantOut) / _FacetSize;
                float tone = EFX_Hash21(float2(floor(facetCoord), 5.3));
                half3 rgb = lerp(_ShadeColor.rgb, _MidColor.rgb, step(_FacetShadeChance, tone));
                rgb = lerp(rgb, _LightColor.rgb, step(_FacetLitFrom, tone));
                float crack = 1.0 - EFX_Step(_CrackWidth, min(frac(facetCoord), 1.0 - frac(facetCoord)) * _FacetSize);
                rgb = lerp(rgb, _LightColor.rgb * _CrackGlow, crack);

                // Spikes over it: a lit face and a shaded face either side of a crisp ridge.
                half3 spikeColor = lerp(_LightColor.rgb, _ShadeColor.rgb, EFX_Step(0.0, side));
                rgb = lerp(rgb, spikeColor, EFX_Fill(spikes));

                // Glass shine: a fat and a thin stripe racing across everything, again and again.
                float shine = frac((x + y * _ShineSlant - t * _ShineSpeed) / _ShineRepeat) * _ShineRepeat;
                float stripes = max(1.0 - EFX_Step(_ShineFatWidth, shine), EFX_Step(_ShineThinStart, shine) * (1.0 - EFX_Step(_ShineThinEnd, shine)));
                rgb *= _Emission * (1.0 + _AimGlow * _Highlight);
                rgb = lerp(rgb, _ShineBrightness, stripes * _ShineStrength);   // the shine is the part that blooms

                // Ink round the whole silhouette and round each spike, so they read as crystals growing out.
                half ink = max(EFX_Step(-_ArcInkWidth, ice), EFX_Step(-_ArcInkWidth * _SpikeInkShare, spikes) * EFX_Fill(spikes));
                rgb = lerp(rgb, _InkColor.rgb, ink);

                half4 frozen = half4(rgb, EFX_Fill(ice) * lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf)));

                return EFX_Over(frozen, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
