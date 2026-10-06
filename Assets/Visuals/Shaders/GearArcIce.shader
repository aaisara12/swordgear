Shader "Swordgear/Gear Arc Ice"
{
    // Ice's gear section. Idle or aimed at, it's the shared cartoon tile in Ice's colour. Active, it becomes a
    // frozen crystal crown:
    //   - The band turns to chunky cartoon ice cut like a gem: a zig-zag girdle line along its middle with big
    //     straight-edged triangular facets either side - frost, pale and cyan in the outer row, cyan, blue and
    //     a little deep blue in the inner row - white light strokes along a few facet edges, a pale rim light
    //     inside the inner outline, and two little groups of cartoon glass shine marks (short parallel
    //     strokes) that land somewhere new each time Ice is granted. The gem glitters: every couple of seconds
    //     a gleam ripples along the band, each facet flashing a couple of tones lighter as it passes, single
    //     facets blink now and then, and the odd four-point glint pops.
    //   - A white snow cap sits along the outer edge: a clean, gently wavy underside shaded pale blue, soft
    //     drifts and small bumps on top, and pointed cartoon icicles hanging from it over the band. The drifts
    //     and icicles boil (change size a little each drawing) rather than the whole outline crawling.
    //   - Three clusters of chunky hexagonal crystals fan out of the snow: a tall middle crystal and one or two
    //     smaller ones leaning well out to the sides. Each crystal is a bold two-tone prism - a cyan lit left
    //     face and a blue right face split by a ridge, a deep blue shadow sliver, a white faceted tip that
    //     gleams, a white highlight stroke - inked in deep ice-blue.
    //   - The crown is always on the move: each cluster punches up out of the snow with a springy overshoot
    //     (stretching thin, then squashing), the side crystals a beat later; while it holds, every crystal bobs
    //     and rocks a little on twos, and pops a little bigger ("tink") while a glint flicks on it. Then it
    //     shivers while a zig-zag crack draws itself across, flashes white for a frame and snaps into three
    //     chunks with a white smash star; the chunks fly apart (never out past the spill's edge), spin and pop,
    //     and it regrows as a new random cluster - one cluster after another round the arc. Granting Ice regrows
    //     them all at once.
    //
    // Fills stay under the bloom threshold so the ink stays crisp; only glints, the smash star, the one white
    // frame before a break and the crystal tips go past white. Every shape is drawn from a continuous distance
    // (never from a field that jumps between cells), so every edge anti-aliases cleanly over one pixel. Cost per
    // pixel: the band, the snow, the icicles and the crown each only run near where they can draw. The band works
    // out one facet row (two along the girdle) and, clear of the facet edges, one facet's tone; the shine marks,
    // glints and icicles only run close to them. The crown looks at two cluster slots of up to three crystals:
    // each crystal is first ruled out cheaply from where its root stands and how far it can lean, then by a box
    // round it before its shape is worked out; a shattering crystal's three chunks are each drawn only near them.
    //
    // The loose pieces (drifting snowflakes) are particles, not this shader: ArcBitsIce.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Palette)]
        _SnowColor ("Snow", Color) = (1, 1, 1, 1)
        _FrostColor ("Frost, the palest ice (outer facets, flashing facets, icicles)", Color) = (0.91, 0.984, 1, 1)
        _PaleColor ("Pale Ice (outer facets, the snow's shade, crystal tips' right facet, icicles' shade)", Color) = (0.749, 0.953, 1, 1)
        _CyanColor ("Cyan Ice (facets, crystals' lit faces, glint rims)", Color) = (0.4, 0.85, 0.93, 1)
        _BlueColor ("Mid Blue Ice (inner facets, crystals' right faces)", Color) = (0.227, 0.608, 0.851, 1)
        _DeepColor ("Deep Blue Ice (a few inner facets, crystals' shadow sliver, the girdle line)", Color) = (0.137, 0.408, 0.659, 1)
        _InkColor ("Ink", Color) = (0.043, 0.184, 0.361, 1)
        _Emission ("Brightness Of The Fills (keep under 0.9 or the white bits bloom)", Range(0.5, 2)) = 0.88
        _TipGleam ("Crystal Tips Brightness (times the fills; above about 1.05 they bloom a little)", Range(0.5, 3)) = 1.3
        _AimGlow ("Extra Brightness When Aimed At (share, 0.2 = a fifth brighter)", Range(0, 2)) = 0.2
        _InkShare ("Ink Width (share of the gear's outline width)", Range(0, 2)) = 0.6

        [Header(Ice Band Facets)]
        _FacetSize ("Facet Size Along The Arc (world units between corners)", Range(0.5, 5)) = 1.3
        _FacetJitter ("Facet Corner Jitter (share of the facet size)", Range(0, 1.5)) = 0.9
        _GirdleHeight ("Girdle Height, where the two rows meet (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.5
        _GirdleWobble ("Girdle Zig-Zag (share of the band)", Range(0, 0.6)) = 0.25
        _GirdleLine ("Girdle Line Width (share of the ink width)", Range(0, 1.5)) = 0.5
        _FacetFrostChance ("Frost Facets In The Outer Row (0-1 share; the rest are pale or cyan)", Range(0, 1)) = 0.15
        _FacetPaleChance ("Pale Facets In The Outer Row (0-1 share)", Range(0, 1)) = 0.15
        _FacetBlueChance ("Blue Facets In The Inner Row (0-1 share; the rest are cyan or deep)", Range(0, 1)) = 0.5
        _FacetDeepChance ("Deep Blue Facets In The Inner Row (0-1 share)", Range(0, 1)) = 0.08
        _EdgeLightChance ("Outer-Row Edges With A White Light Stroke (0-1 share)", Range(0, 1)) = 0.45
        _EdgeLightWidth ("Light Stroke Half-Width (world units)", Range(0, 0.2)) = 0.05
        _InnerRim ("Pale Rim Light Inside The Inner Outline (world units)", Range(0, 0.4)) = 0.08

        [Header(Band Glitter)]
        _RippleEvery ("A Gleam Ripples Along The Band Every (seconds)", Range(0.5, 10)) = 2.2
        _RippleTime ("The Ripple Takes, end to end (seconds)", Range(0.05, 2)) = 0.6
        _RippleHold ("Each Facet Flashes For (seconds)", Range(0.02, 1)) = 0.09
        [IntRange] _RippleLift ("A Flash Lifts A Facet By (tones: deep, blue, cyan, pale, frost, white)", Range(1, 5)) = 2
        _BlinkEvery ("A Facet Blink Chance Every (seconds)", Range(0.1, 5)) = 0.5
        _BlinkChance ("Blink Chance Each Time (0-1, per facet)", Range(0, 1)) = 0.07
        _BlinkLength ("A Blink Lasts (seconds)", Range(0.02, 1)) = 0.17
        _GleamRate ("Ripple And Blinks Drawn At (frames per second)", Range(1, 30)) = 12

        [Header(Shine Marks)]
        _ShineSpotA ("First Mark, Along The Arc (world units from the middle, + is clockwise)", Range(-5, 5)) = -2.4
        _ShineSpotB ("Second Mark, Along The Arc (world units from the middle, + is clockwise)", Range(-5, 5)) = 2.1
        _ShineShuffle ("Marks Move Each Grant By Up To (world units either way)", Range(0, 3)) = 1.0
        _ShineHeight ("Marks Height (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.4
        _ShineLength ("Longest Stroke (world units)", Range(0, 2)) = 0.85
        _ShineWidth ("Stroke Half-Width (world units)", Range(0, 0.3)) = 0.075
        _ShineGap ("Gap Between Strokes (world units)", Range(0, 0.6)) = 0.24
        _ShineSlant ("Stroke Slant (degrees from straight out, + leans clockwise)", Range(-80, 80)) = 38

        [Header(Glints)]
        _GlintEvery ("A Glint Chance Every (seconds)", Range(0.2, 5)) = 1.0
        _GlintChance ("Glint Chance Each Time (0-1)", Range(0, 1)) = 0.5
        _GlintLength ("A Glint Lasts (seconds)", Range(0.05, 1)) = 0.25
        _GlintSize ("Glint Size (world units, centre to point)", Range(0, 1)) = 0.42
        _GlintPlump ("Glint Point Fatness (0.5 needle-thin - 1 a plain diamond)", Range(0.4, 1)) = 0.62
        _GlintBrightness ("Glint Brightness (HDR, above 1 blooms)", Range(1, 6)) = 1.8
        _BandGlintSpacing ("Band Glints, One Spot Every (world units along the arc)", Range(1, 6)) = 2.2
        _TinkPop ("A Glint Pops Its Crystal Bigger By (share, 0.08 = 8%)", Range(0, 0.3)) = 0.08

        [Header(Snow Cap)]
        _CrustDepth ("Snow Depth Into The Band (world units)", Range(0, 1.5)) = 0.42
        _CrustInk ("Snow Outline Width (share of the ink width)", Range(0, 2)) = 0.8
        _CrustLumpSpacing ("Drift Spacing (world units)", Range(0.3, 4)) = 1.55
        _CrustLumpSize ("Drift Height Radius (world units)", Range(0, 1.5)) = 0.4
        _CrustLumpSpread ("Extra Drift Radius At Random (world units, 0 to this)", Range(0, 0.8)) = 0.22
        _CrustLumpStretch ("Drift Width (times its height)", Range(1, 4)) = 2.0
        _CrustLumpRise ("Tallest Drift Above The Band (world units)", Range(0, 1)) = 0.5
        _CrustSmallChance ("Small Bumps Instead Of Drifts (0-1 share)", Range(0, 1)) = 0.35
        _CrustFlatChance ("Flat Stretches, no drift (0-1 share)", Range(0, 1)) = 0.15
        _CrustValley ("Valley Softness Between Drifts (world units)", Range(0, 0.4)) = 0.1
        _CrustWave ("Underside Wave Height (world units)", Range(0, 0.4)) = 0.06
        _CrustWaveLength ("Underside Wave Length (world units)", Range(0.3, 5)) = 2.3
        _CrustShadeHeight ("Shaded Underside Height (world units up from the snow's bottom)", Range(0, 1)) = 0.15
        _CrustOverhang ("Snow Overhangs The Ends By (world units)", Range(0, 0.4)) = 0.12
        _CrustBoil ("Boil, how much drifts and icicles change size each drawing (share)", Range(0, 0.4)) = 0.09
        _CrustBoilRate ("Boil Rate (drawings per second, 8 = on threes)", Range(1, 30)) = 8

        [Header(Icicles)]
        _IcicleSpacing ("One Icicle Spot Every (world units along the arc)", Range(0.3, 3)) = 0.9
        _IcicleChance ("Icicles (0-1 chance per spot)", Range(0, 1)) = 0.55
        _IcicleMinLength ("Shortest Icicle (world units below the snow)", Range(0, 1.5)) = 0.35
        _IcicleMaxLength ("Longest Icicle (world units below the snow)", Range(0, 1.5)) = 0.8
        _IcicleWidth ("Icicle Half-Width At The Top (world units)", Range(0.03, 0.4)) = 0.3

        [Header(Crystal Clusters)]
        [IntRange] _ClusterCount ("Clusters", Range(1, 6)) = 3
        _ClusterJitter ("Cluster Position Jitter (share of the space each has)", Range(0, 0.6)) = 0.12
        _CrystalRoot ("Crystals Grow From (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.94
        _CrystalMinHeight ("Shortest Middle Crystal (world units, root to tip)", Range(0.3, 4)) = 2.2
        _CrystalMaxHeight ("Tallest Middle Crystal (world units, root to tip)", Range(0.3, 4)) = 3.3
        _CrystalReachLimit ("Crystals Never Reach Past, even overshooting (world units out from the band; flying pieces never pass the spill's edge at 3.5)", Range(0.5, 3.5)) = 3.3
        _CrystalWidth ("Middle Crystal Half-Width (world units)", Range(0.1, 1)) = 0.6
        _TipLength ("Tip Length (share of the half-width)", Range(0.3, 4)) = 1.45
        _CentreLean ("Middle Crystal Lean (degrees, either way at random)", Range(0, 30)) = 12
        _TwoSideChance ("Clusters With A Crystal Each Side (0-1 chance, else one side)", Range(0, 1)) = 0.65
        _SideOffset ("Side Crystal Offset (world units along the arc)", Range(0, 1.5)) = 0.5
        _SideHeightMin ("Shortest Side Crystal (share of the middle one)", Range(0.1, 1)) = 0.52
        _SideHeightMax ("Tallest Side Crystal (share of the middle one)", Range(0.1, 1)) = 0.78
        _SideWidth ("Side Crystal Width (share of the middle one)", Range(0.3, 1.2)) = 0.74
        _SideLeanMin ("Least Side Fan-Out (degrees)", Range(0, 60)) = 22
        _SideLeanMax ("Most Side Fan-Out (degrees)", Range(0, 60)) = 40
        _ClusterEndFade ("Crystals Shrink Toward The Ends Over (world units)", Range(0.05, 3)) = 1.2

        [Header(Crystal Faces)]
        _RidgeShift ("Ridge Position (share of the half-width, - is left)", Range(-0.9, 0.9)) = 0
        _ShoulderDip ("Tip Facet Dip At The Ridge (share of the half-width)", Range(0, 1)) = 0.3
        _EdgeShade ("Shadow Sliver Down The Right Edge (share of the half-width)", Range(0, 1)) = 0.36
        _HighlightWidth ("Highlight Stroke Half-Width (share of the half-width)", Range(0, 0.4)) = 0.12

        [Header(Crystal Hold)]
        _HoldBob ("Bob While Holding (share of the height, squashing as it stretches)", Range(0, 0.2)) = 0.04
        _HoldRock ("Rock While Holding (degrees either way)", Range(0, 10)) = 1.8
        _HoldRate ("Bob Speed (bobs per second)", Range(0, 5)) = 1.3
        _HoldFrameRate ("Bob Drawn At (frames per second, 12 = on twos)", Range(1, 30)) = 12

        [Header(Crystal Cycle)]
        _CyclePeriod ("Each Cluster Lives (seconds; one breaks every this divided by Clusters)", Range(1, 20)) = 3.6
        _GrowSpring ("Grow Bounce (radians per second, higher = quicker wobble)", Range(5, 60)) = 24
        _GrowDamping ("Grow Settle (per second, higher = less overshoot)", Range(1, 30)) = 9
        _GrowStagger ("Side Crystals Pop Up Later By (seconds)", Range(0, 0.4)) = 0.12
        _FrameRate ("Grow, Break And Glints Drawn At (frames per second, 0 = smooth)", Range(0, 60)) = 24
        _CrackTime ("Shiver While The Crack Draws Across (seconds)", Range(0, 1)) = 0.34
        _CrackWidth ("Crack Line Half-Width (world units)", Range(0, 0.15)) = 0.035
        _ShiverShake ("Shiver Shake (world units)", Range(0, 0.2)) = 0.05
        _BreakTime ("Pieces Fly For (seconds)", Range(0.05, 1)) = 0.42
        _ChunkHold ("Pieces Keep Their Size For (share of the flight)", Range(0, 0.95)) = 0.6
        _BreakFling ("Pieces Fly Apart (world units; near the spill's outer edge they fly less far out, so none is cut off there)", Range(0, 2)) = 1.25
        _BreakSpin ("Pieces Spin (radians)", Range(0, 4)) = 2
        _SmashSize ("Smash Star Size (share of the middle crystal's width)", Range(0, 4)) = 1.2
        _SmashTime ("Smash Star Shows For (seconds)", Range(0, 0.3)) = 0.09
        _RegrowGap ("Empty Before Regrowing (seconds)", Range(0, 1)) = 0.1

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
                half4 _SnowColor; half4 _FrostColor; half4 _PaleColor; half4 _CyanColor; half4 _BlueColor; half4 _DeepColor; half4 _InkColor; \
                half _Emission; half _TipGleam; half _AimGlow; half _InkShare; \
                float _FacetSize; float _FacetJitter; half _GirdleHeight; half _GirdleWobble; half _GirdleLine; \
                half _FacetFrostChance; half _FacetPaleChance; half _FacetBlueChance; half _FacetDeepChance; \
                half _EdgeLightChance; float _EdgeLightWidth; float _InnerRim; \
                float _RippleEvery; float _RippleTime; float _RippleHold; float _RippleLift; \
                float _BlinkEvery; half _BlinkChance; float _BlinkLength; float _GleamRate; \
                float _ShineSpotA; float _ShineSpotB; float _ShineShuffle; half _ShineHeight; float _ShineLength; float _ShineWidth; float _ShineGap; float _ShineSlant; \
                float _GlintEvery; half _GlintChance; float _GlintLength; float _GlintSize; float _GlintPlump; half _GlintBrightness; float _BandGlintSpacing; float _TinkPop; \
                float _CrustDepth; half _CrustInk; float _CrustLumpSpacing; float _CrustLumpSize; float _CrustLumpSpread; float _CrustLumpStretch; float _CrustLumpRise; \
                half _CrustSmallChance; half _CrustFlatChance; float _CrustValley; float _CrustWave; float _CrustWaveLength; float _CrustShadeHeight; float _CrustOverhang; \
                float _CrustBoil; float _CrustBoilRate; \
                float _IcicleSpacing; half _IcicleChance; float _IcicleMinLength; float _IcicleMaxLength; float _IcicleWidth; \
                float _ClusterCount; half _ClusterJitter; half _CrystalRoot; float _CrystalMinHeight; float _CrystalMaxHeight; float _CrystalReachLimit; \
                float _CrystalWidth; float _TipLength; float _CentreLean; half _TwoSideChance; float _SideOffset; \
                half _SideHeightMin; half _SideHeightMax; half _SideWidth; float _SideLeanMin; float _SideLeanMax; float _ClusterEndFade; \
                half _RidgeShift; half _ShoulderDip; half _EdgeShade; half _HighlightWidth; \
                float _HoldBob; float _HoldRock; float _HoldRate; float _HoldFrameRate; \
                float _CyclePeriod; float _GrowSpring; float _GrowDamping; float _GrowStagger; float _FrameRate; \
                float _CrackTime; float _CrackWidth; float _ShiverShake; float _BreakTime; float _ChunkHold; float _BreakFling; float _BreakSpin; \
                float _SmashSize; float _SmashTime; float _RegrowGap;
            #include "GearArcCommon.hlsl"

            // The crystals work in the arc's own Cartesian frame `p`: world units from the gear's centre, +y out
            // through the arc's middle and +x clockwise round the gear (screen right when the arc is at the top).
            // The band and the snow, which stay close to the band, use an unrolled frame instead (along the arc,
            // out from the centre): near the band it is flat enough, and it needs no trig.

            // A random 0..1 from two numbers and a salt. Unlike EFX_Hash21 it stays random for the larger inputs
            // that come from counting ticks of the clock (pass those through IceTick first).
            float IceRand(float a, float b, float salt)
            {
                float3 p3 = frac(float3(a + salt * 17.17, b + salt * 3.71, a - b * 0.37) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Keeps a count of clock ticks small enough to hash precisely, however long the game has run.
            float IceTick(float n)
            {
                return fmod(n, 1009.0);
            }

            // `time` held to whole frames at `rate` frames per second (0 = smooth).
            float IceFrames(float time, float rate)
            {
                return rate > 0.0 ? floor(time * rate) / rate : time;
            }

            float2 IceRotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            // A point `radius` from the gear's centre, `angle` radians clockwise from the arc's middle.
            float2 IcePolar(float angle, float radius)
            {
                return radius * float2(sin(angle), cos(angle));
            }

            float IceCapsule(float2 p, float2 a, float2 b, float radius)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
                return length(pa - ba * h) - radius;
            }

            // Smooth union: soft valleys where two shapes meet, `k` world units wide.
            float IceSmoothMin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / max(k, 1e-4));
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            // A cartoon four-point glint of radius `size`: |x|^k + |y|^k = 1, k = _GlintPlump, as a distance (the
            // field over its gradient, the gradient's blow-up along the axes capped so the middle stays solid).
            float IceStar(float2 p, float size)
            {
                float k = _GlintPlump;
                float2 q = abs(p) / max(size, 1e-4);
                float2 qk = pow(max(q, 1e-5), k);
                float field = qk.x + qk.y - 1.0;
                float2 gradient = k * pow(max(q, 0.2), k - 1.0);
                return field / length(gradient) * size;
            }

            // A cartoon smash burst: a `points`-pointed star of radius `size` (iq's star distance; `sharp` runs
            // from 2, blunt, to `points`, needle-sharp).
            float IceBurst(float2 p, float size, float points, float sharp)
            {
                float an = 3.14159265 / points;
                float en = 3.14159265 / sharp;
                float2 acs = float2(cos(an), sin(an));
                float2 ecs = float2(cos(en), sin(en));
                float a = atan2(p.x, p.y);
                float bn = a - 2.0 * an * floor(a / (2.0 * an)) - an;
                float2 q = length(p) * float2(cos(bn), abs(sin(bn)));
                q -= size * acs;
                q += ecs * clamp(-dot(q, ecs), 0.0, size * acs.y / ecs.y);
                return length(q) * sign(q.x);
            }

            // An isosceles triangle with its apex at the origin and its base `q.y` up, `q.x` either side.
            float IceTriangle(float2 p, float2 q)
            {
                p.x = abs(p.x);
                float2 a = p - q * saturate(dot(p, q) / dot(q, q));
                float2 b = p - q * float2(saturate(p.x / q.x), 1.0);
                float2 d = min(float2(dot(a, a), -(p.x * q.y - p.y * q.x)), float2(dot(b, b), -(p.y - q.y)));
                return -sqrt(d.x) * sign(d.y);
            }

            // Pops 0 -> 1 -> 0 while a glint is on: `time` seconds into a glint of `duration` seconds.
            half IcePop(float time, float duration)
            {
                float k = saturate(time / max(duration, 1e-3));
                return (half)(sin(k * 3.14159265) * step(time, duration));
            }

            // Draws `shape` (signed distance, world units) over `layer`: flat `rgb` inside an `inkColor` line.
            void IceDrawInk(inout half4 layer, float shape, half3 rgb, half3 inkColor, float inkWidth, float px, half alpha)
            {
                half ink = (1.0 - EFX_FillPx(shape + inkWidth, px)) * step(1e-4, inkWidth);
                layer = EFX_Over(half4(lerp(rgb, inkColor, ink), EFX_FillPx(shape, px) * alpha), layer);
            }

            void IceDraw(inout half4 layer, float shape, half3 rgb, float inkWidth, float px, half alpha)
            {
                IceDrawInk(layer, shape, rgb, _InkColor.rgb, inkWidth, px, alpha);
            }

            // A glint: an HDR white star with a thin cyan rim that grows and shrinks with it.
            void IceGlint(inout half4 layer, float2 q, float size, float px, half alpha)
            {
                float star = size > 0.01 ? IceStar(q, size) : 1e3;
                IceDrawInk(layer, star, _SnowColor.rgb * _GlintBrightness, _CyanColor.rgb,
                           min(_ArcInkWidth * _InkShare * 0.5, size * 0.1), px, alpha);
            }

            // The six tones, darkest first: 0 deep, 1 blue, 2 cyan, 3 pale, 4 frost, 5 snow white.
            half3 IceTone(float index)
            {
                half3 rgb = _DeepColor.rgb;
                rgb = index >= 0.5 ? _BlueColor.rgb : rgb;
                rgb = index >= 1.5 ? _CyanColor.rgb : rgb;
                rgb = index >= 2.5 ? _PaleColor.rgb : rgb;
                rgb = index >= 3.5 ? _FrostColor.rgb : rgb;
                return index >= 4.5 ? _SnowColor.rgb : rgb;
            }

            // ---- The band's facets: a cut-gem band ----
            // A zig-zag girdle runs along the band's middle. Corners alternate: even k on the girdle, odd k just
            // past the outer edge (the upper row) or just past the inner edge (the lower row). Each row is a strip
            // of big triangles between those corners, so the band reads as diamonds and bow-ties, like the side of
            // a cut gem.

            bool IsEven(float k)
            {
                return frac(k * 0.5) < 0.25;
            }

            float FacetAlong(float k, float salt, float halfLength)
            {
                return (k + (EFX_Hash21(float2(k, salt)) - 0.5) * _FacetJitter * 0.5) * _FacetSize - halfLength;
            }

            // Even corner k: on the girdle (both rows share these).
            float2 GirdleCorner(float k, float halfLength)
            {
                float girdle = _ArcShape.y + ArcThickness() * (_GirdleHeight + (EFX_Hash21(float2(k, 29.1)) - 0.5) * _GirdleWobble);
                return float2(FacetAlong(k, 41.3, halfLength), girdle);
            }

            // Odd corner k of a row: `edgeRadius` is the band edge that row's odd corners sit past.
            float2 EdgeCorner(float k, float edgeRadius, float salt, float halfLength)
            {
                return float2(FacetAlong(k, salt, halfLength), edgeRadius);
            }

            // How far `flat` is clockwise of the edge between two corners (negative: anticlockwise of it).
            float FacetSide(float2 flat, float2 cornerA, float2 cornerB)
            {
                float2 dir = normalize(cornerB - cornerA);
                dir *= sign(dir.y);   // run each edge outward
                float2 rel = flat - cornerA;
                return dir.y * rel.x - dir.x * rel.y;
            }

            // The gleam ripple: every few seconds a gleam runs from one end of the band to the other (each its own
            // way). Its position is the same for every facet and every pixel, so it's worked out once.
            struct IceGleam
            {
                float time;      // seconds into this ripple, on the gleam's frames
                bool reverse;    // this one runs from the clockwise end
            };

            IceGleam GetGleam()
            {
                float clock = _Time.y / _RippleEvery + _ArcShape.w * 0.37;
                float cycle = IceTick(floor(clock));
                IceGleam gleam;
                gleam.time = IceFrames(frac(clock) * _RippleEvery, _GleamRate);
                gleam.reverse = IceRand(cycle, 3.0, 31.0) >= 0.5;
                return gleam;
            }

            // How many tones facet m is lifted right now: the gleam rippling along the band, or a lone blink.
            float FacetLift(float m, bool upperRow, float halfLength, IceGleam gleam)
            {
                // The ripple: a facet flashes as the gleam passes. The outer row is a frame behind, so the gleam
                // runs on a slant.
                float share = saturate(m * _FacetSize / (2.0 * halfLength));
                share = gleam.reverse ? 1.0 - share : share;
                float reached = gleam.time - share * _RippleTime - (upperRow ? 1.0 / _GleamRate : 0.0);
                float lift = reached >= 0.0 && reached < _RippleHold ? _RippleLift : 0.0;

                // Blinks: now and then one facet flashes by itself.
                float row = upperRow ? 0.0 : 517.0;
                float blinkClock = _Time.y / _BlinkEvery + EFX_Hash21(float2(m, upperRow ? 3.1 : 5.7)) * 3.0;
                float blinkTick = IceTick(floor(blinkClock));
                float blinkTime = IceFrames(frac(blinkClock) * _BlinkEvery, _GleamRate);
                bool blink = IceRand(m + row, blinkTick, 32.0) < _BlinkChance && blinkTime < _BlinkLength;
                return max(lift, blink ? 2.0 : 0.0);
            }

            // Facet m of a row is the triangle between its edges m - 1 and m.
            half3 FacetTone(float m, bool upperRow, float halfLength, IceGleam gleam)
            {
                float h = EFX_Hash21(float2(m, upperRow ? 17.9 : 23.3));
                // Lit from outside the gear: the outer row frost, pale and cyan, the inner row cyan, blue, deep.
                float upper = h < _FacetFrostChance ? 4.0 : (h < _FacetFrostChance + _FacetPaleChance ? 3.0 : 2.0);
                float lower = h < _FacetDeepChance ? 0.0 : (h < _FacetDeepChance + _FacetBlueChance ? 1.0 : 2.0);
                return IceTone((upperRow ? upper : lower) + FacetLift(m, upperRow, halfLength, gleam));
            }

            // A white light stroke along part of edge `edge` (from corner a to corner b), on some edges only.
            float EdgeLight(float2 flat, float2 a, float2 b, float edge)
            {
                float2 low = a.y < b.y ? a : b;   // the girdle end
                float2 high = a.y < b.y ? b : a;
                float stroke = IceCapsule(flat, lerp(low, high, 0.16), lerp(low, high, 0.46), _EdgeLightWidth);
                return EFX_Hash21(float2(edge, 61.7)) < _EdgeLightChance ? stroke : 1e3;
            }

            // One row's colour at `flat`: the strip of facets, each edge anti-aliased; the outer row's light strokes.
            // `girdleA` and `girdleB` are the girdle corners either side of this pixel's facet pair (even corners
            // kg and kg + 2, kg = k or k - 1).
            half3 FacetRow(float2 flat, float2 girdleA, float2 girdleB, float edgeRadius, float salt, bool upperRow,
                           float halfLength, IceGleam gleam, float px)
            {
                // Corners k - 1 to k + 2: two on the girdle, two past the band's edge, alternating.
                float k = floor((flat.x + halfLength) / _FacetSize);
                bool kEven = IsEven(k);
                float2 edgeA = EdgeCorner(kEven ? k - 1.0 : k, edgeRadius, salt, halfLength);
                float2 edgeB = EdgeCorner(kEven ? k + 1.0 : k + 2.0, edgeRadius, salt, halfLength);
                float2 c0 = kEven ? edgeA : girdleA;
                float2 c1 = kEven ? girdleA : edgeA;
                float2 c2 = kEven ? edgeB : girdleB;
                float2 c3 = kEven ? girdleB : edgeB;
                float side0 = FacetSide(flat, c0, c1);   // world units from each edge's line
                float side1 = FacetSide(flat, c1, c2);
                float side2 = FacetSide(flat, c2, c3);
                float past0 = EFX_FillPx(-side0, px);
                float past1 = EFX_FillPx(-side1, px);
                float past2 = EFX_FillPx(-side2, px);
                half3 rgb;
                [branch] if (past0 * (1.0 - past0) + past1 * (1.0 - past1) + past2 * (1.0 - past2) <= 0.0)
                {
                    // Clear of every edge (the usual case): just the facet it's in.
                    rgb = FacetTone(k + (past2 >= 1.0 ? 2.0 : (past1 >= 1.0 ? 1.0 : (past0 >= 1.0 ? 0.0 : -1.0))),
                                    upperRow, halfLength, gleam);
                }
                else
                {
                    // Within a pixel of an edge: blend the facets across it.
                    rgb = FacetTone(k - 1.0, upperRow, halfLength, gleam);
                    rgb = lerp(rgb, FacetTone(k, upperRow, halfLength, gleam), past0);
                    rgb = lerp(rgb, FacetTone(k + 1.0, upperRow, halfLength, gleam), past1);
                    rgb = lerp(rgb, FacetTone(k + 2.0, upperRow, halfLength, gleam), past2);
                }
                // The light strokes lie along the edges, so only near one (no nearer than its line).
                [branch] if (upperRow && min(abs(side0), min(abs(side1), abs(side2))) < _EdgeLightWidth + px)
                {
                    float strokes = min(EdgeLight(flat, c0, c1, k), min(EdgeLight(flat, c1, c2, k + 1.0), EdgeLight(flat, c2, c3, k + 2.0)));
                    rgb = lerp(rgb, _SnowColor.rgb, EFX_FillPx(strokes, px));
                }
                return rgb;
            }

            // How far `flat` is outward of the girdle (negative: inward of it). g1 and g2 are girdle corners kg and
            // kg + 2 round this pixel; a corner jittered past it brings in the next one out.
            float GirdleSide(float2 flat, float kg, float2 g1, float2 g2, float halfLength)
            {
                float2 a = g1;
                float2 b = g2;
                [branch] if (flat.x < g1.x)
                {
                    a = GirdleCorner(kg - 2.0, halfLength);
                    b = g1;
                }
                else if (flat.x > g2.x)
                {
                    a = g2;
                    b = GirdleCorner(kg + 4.0, halfLength);
                }
                float2 dir = normalize(b - a);
                float2 rel = flat - a;
                return dir.x * rel.y - dir.y * rel.x;
            }

            // ---- Crystals ----

            // A zig-zag offset along a crack or a break line, `along` world units across it. It's continuous, so
            // the crack and the broken edges stay clean; `slope` is how steep its legs are.
            float IceZig(float along, float hw, float seed, out float slope)
            {
                float period = hw * 0.6;
                float height = hw * 0.08;
                slope = 4.0 * height / period;
                return (abs(frac(along / period + seed) - 0.5) - 0.25) * 4.0 * height;
            }

            // The two lines a crystal cracks and breaks along: `cut` is where each crosses the axis, `normal`
            // points toward the tip. Returns how far `q` is above the line (zig-zagged).
            float BreakLine(float2 q, float2 cut, float2 normal, float hw, float seed, out float slope)
            {
                float2 rel = q - cut;
                float2 tangent = float2(normal.y, -normal.x);
                return dot(rel, normal) - IceZig(dot(rel, tangent), hw, seed, slope);
            }

            // One crystal in its own frame: q.x across (+ right), q.y up its axis from the root, world units.
            // Returns the signed distance to its outline; `face` gets its flat colour at q. `crack` (0..1) draws
            // the crack across it before it breaks.
            float CrystalShape(float2 q, float hw, float len, float px, float flip, float seed, half crack, out half3 face)
            {
                float tip = min(hw * _TipLength, len * 0.6);
                float shoulder = len - tip;
                float ridge = hw * _RidgeShift;
                float2 capNormalL = normalize(float2(-tip, ridge + hw));
                float2 capNormalR = normalize(float2(tip, hw - ridge));
                float sdf = max(max(abs(q.x) - hw, -q.y),
                                max(dot(q - float2(-hw, shoulder), capNormalL), dot(q - float2(hw, shoulder), capNormalR)));

                // The tip's facets meet the faces along a shallow V, lowest at the ridge.
                float dip = hw * _ShoulderDip;
                float shoulderY = q.x < ridge ? shoulder - dip * (q.x + hw) / max(ridge + hw, 1e-3)
                                              : shoulder - dip * (hw - q.x) / max(hw - ridge, 1e-3);
                half left = EFX_FillPx(q.x - ridge, px);
                half cap = EFX_FillPx(shoulderY - q.y, px);
                half sliver = EFX_FillPx(hw * (1.0 - _EdgeShade) - q.x, px);

                // A bold two-tone prism: cyan lit face, blue right face with a deep sliver, a white gleaming tip.
                half3 body = lerp(_DeepColor.rgb, _BlueColor.rgb, sliver);
                body = lerp(body, _CyanColor.rgb, left);
                half3 top = lerp(_PaleColor.rgb, _SnowColor.rgb * _TipGleam, left);
                face = lerp(body, top, cap);

                // A white highlight stroke down the lit face, and a dot under it.
                float strokeX = lerp(-hw, ridge, 0.4);
                float stroke = IceCapsule(q, float2(strokeX, len * 0.3), float2(strokeX, shoulder - dip - hw * 0.25), hw * _HighlightWidth);
                stroke = min(stroke, length(q - float2(strokeX, len * 0.3 - hw * 0.45)) - hw * _HighlightWidth);
                face = lerp(face, _SnowColor.rgb, EFX_FillPx(stroke, px));

                [branch] if (crack > 0.0)
                {
                    // The crack draws itself across from the lit edge along the two break lines (the upper a beat
                    // later): an ink zig-zag with chipped white edges.
                    float slopeLow;
                    float slopeHigh;
                    float low = BreakLine(q, float2(0.0, len * 0.36), normalize(float2(-0.45 * flip, 1.0)), hw, seed, slopeLow);
                    float high = BreakLine(q, float2(0.0, len * 0.68), normalize(float2(0.5 * flip, 1.0)), hw, seed + 0.5, slopeHigh);
                    float lowLine = abs(low) * rsqrt(1.0 + slopeLow * slopeLow) - _CrackWidth;
                    float highLine = abs(high) * rsqrt(1.0 + slopeHigh * slopeHigh) - _CrackWidth;
                    lowLine = max(lowLine, (q.x + hw) - crack * 2.4 * hw);
                    highLine = max(highLine, (q.x + hw) - saturate(crack * 1.5 - 0.5) * 2.4 * hw);
                    float crackLine = min(lowLine, highLine);
                    face = lerp(face, _SnowColor.rgb, EFX_FillPx(crackLine - _CrackWidth * 1.3, px));
                    face = lerp(face, _InkColor.rgb, EFX_FillPx(crackLine, px));
                }
                return sdf;
            }

            // Everything about the crown that is the same for every pixel this frame, worked out once.
            struct IceCrown
            {
                float halfLength;   // the arc's half-length along its mid radius
                float midRadius;
                float rootRadius;   // the crystals grow from this far out from the gear's centre
                float reachMax;     // no crystal is longer than this, root to tip
                float hidden;       // nothing of a crystal shows inside this radius (it's in the snow)
                float limit;        // nothing reaches past this radius: the spill's edge, less a hair
                float count;        // clusters
                float slotWidth;    // world units along the arc per cluster
                float frame;        // which grow / break frame it is (the shiver changes each)
                float holdTime;     // the clock, held to the bob's frames
                float near;         // how close to a shape's bounds a pixel has to be to get any of it
                float glintReach;   // how far a glint can draw from its middle
            };

            // Could anything drawn for a crystal reach a pixel `radius` from the gear's centre? The pixel is between
            // `offsetA` and `offsetB` radians clockwise of the crystal's root (offsetA <= offsetB: the root may be
            // anywhere in a range). The crystal grows from `rootRadius` out, leaning between `leanA` and `leanB`
            // radians off straight out (leanA <= 0 <= leanB), and everything drawn for it lies within `spread`
            // world units of its axis and no more than `drop` below its root. A cheap, safe bound (no trig): when
            // it says no, nothing of the crystal is there.
            bool CrystalMayReach(float radius, float offsetA, float offsetB, float rootRadius, float leanA, float leanB,
                                 float spread, float drop)
            {
                // The pixel's distance across the root's radial line is radius * sin(offset); these are never
                // further across (sin x >= x - x^3 / 6 for x >= 0, and the other way round below 0).
                float acrossA = radius * offsetA * (1.0 - offsetA * offsetA / 6.0);
                float acrossB = radius * offsetB * (1.0 - offsetB * offsetB / 6.0);
                // A crystal point this far out is no further up its axis than this: d cos(lean) = y - root + e sin(lean).
                float lean = max(-leanA, leanB);
                float cosLow = max(1.0 - 0.5 * lean * lean, 0.05);
                float up = max(radius - rootRadius + spread * min(lean, 1.0), 0.0) / cosLow;
                // So it's no further across than this, either way (|sin x| <= |x|).
                float highest = max(up * leanB, -drop * leanA) + spread;
                float lowest = min(up * leanA, -drop * leanB) - spread;
                return lean > 1.3 || (acrossA <= highest && acrossB >= lowest);
            }

            // Where a break line (through `cut`, `normal` towards the tip) crosses one side of a crystal's outline:
            // `x` is -hw (the left side) or hw. Up the straight side, else up the slope of the tip to the apex.
            float2 BreakCorner(float2 cut, float2 normal, float x, float shoulder, float2 apex)
            {
                float y = cut.y - normal.x * x / normal.y;
                float2 low = float2(x, shoulder);
                float2 slope = apex - low;
                float along = saturate(-dot(low - cut, normal) / max(dot(slope, normal), 1e-5));
                return y <= shoulder ? float2(x, y) : low + slope * along;
            }

            // The corners of chunk `i` of a shattered crystal (0 the base, 1 the middle, 2 the tip), from the
            // chunk's middle in the crystal's frame: its outline cut by the break lines taken straight (their
            // zig-zag strays up to 0.08 hw either side). Unused corners repeat one; the chunk lies inside them.
            void ChunkCorners(int i, float hw, float len, float shoulder, float ridge, float flip, float2 middle,
                              out float2 h0, out float2 h1, out float2 h2, out float2 h3, out float2 h4, out float2 h5,
                              out float2 h6)
            {
                float2 cutLow = float2(0.0, len * 0.36);
                float2 cutHigh = float2(0.0, len * 0.68);
                float2 normalLow = float2(-0.45 * flip, 1.0);
                float2 normalHigh = float2(0.5 * flip, 1.0);
                float2 apex = float2(ridge, len);
                float2 shoulderL = float2(-hw, shoulder);
                float2 shoulderR = float2(hw, shoulder);
                float2 lowL = BreakCorner(cutLow, normalLow, -hw, shoulder, apex);
                float2 lowR = BreakCorner(cutLow, normalLow, hw, shoulder, apex);
                float2 highL = BreakCorner(cutHigh, normalHigh, -hw, shoulder, apex);
                float2 highR = BreakCorner(cutHigh, normalHigh, hw, shoulder, apex);
                // Which side of each line the shoulders and the apex are (above is towards the tip).
                float3 aboveLow = float3(dot(shoulderL - cutLow, normalLow), dot(shoulderR - cutLow, normalLow), dot(apex - cutLow, normalLow));
                float3 aboveHigh = float3(dot(shoulderL - cutHigh, normalHigh), dot(shoulderR - cutHigh, normalHigh), dot(apex - cutHigh, normalHigh));
                if (i == 0)
                {
                    h0 = float2(-hw, 0.0);
                    h1 = float2(hw, 0.0);
                    h2 = lowL;
                    h3 = lowR;
                    h4 = aboveLow.x <= 0.0 ? shoulderL : lowL;
                    h5 = aboveLow.y <= 0.0 ? shoulderR : lowR;
                    h6 = aboveLow.z <= 0.0 ? apex : lowL;
                }
                else if (i == 1)
                {
                    h0 = lowL;
                    h1 = lowR;
                    h2 = highL;
                    h3 = highR;
                    h4 = aboveLow.x >= 0.0 && aboveHigh.x <= 0.0 ? shoulderL : lowL;
                    h5 = aboveLow.y >= 0.0 && aboveHigh.y <= 0.0 ? shoulderR : lowR;
                    h6 = aboveLow.z >= 0.0 && aboveHigh.z <= 0.0 ? apex : highL;
                }
                else
                {
                    h0 = highL;
                    h1 = highR;
                    h2 = aboveHigh.x >= 0.0 ? shoulderL : highL;
                    h3 = aboveHigh.y >= 0.0 ? shoulderR : highR;
                    h4 = aboveHigh.z >= 0.0 ? apex : highL;
                    h5 = h0;
                    h6 = h1;
                }
                h0 -= middle;
                h1 -= middle;
                h2 -= middle;
                h3 -= middle;
                h4 -= middle;
                h5 -= middle;
                h6 -= middle;
            }

            // One crystal rooted `cr.rootRadius` out at `rootBeta` radians clockwise from the arc's middle, leaning
            // off straight out by the angle whose sine is `sinLean`. `pieces` is 0 while whole, else 0..1 through
            // its shattering (`smash` 1 while the smash star shows); `flash` whitens it; `crack` draws its crack;
            // `glint` (0..1) pops a star on it.
            void DrawCrystal(inout half4 layer, float2 p, float radius, IceCrown cr, float rootBeta, float sinLean,
                             float hw, float len, float shake, half flash, half crack, float pieces, half smash,
                             bool sideCrystal, float seed, half glint, float glintSpot, float px, half bright)
            {
                // Into its own frame: it points rootBeta + asin(sinLean) clockwise of the arc's middle.
                float lean = asin(sinLean);
                float c = cos(rootBeta + lean);
                float s = sin(rootBeta + lean);
                float2 root = IcePolar(rootBeta, cr.rootRadius);
                float2 rel = p - root;
                float2 q = float2(rel.x * c - rel.y * s, rel.x * s + rel.y * c);
                q.x -= shake;
                float flip = seed < 0.5 ? -1.0 : 1.0;   // which way the cracks slant and the chunks go, per crystal
                half3 face = 0.0;
                float sdf = 1e3;   // nothing, unless this pixel is near enough to get some of it
                float size = hw;   // how wide what's drawn is: the ink thins with it, so small bits don't go all ink
                [branch] if (pieces > 0.0)
                {
                    // Shattered: it snaps into three chunks along the crack lines. The tip flies out spinning, the
                    // middle flies off sideways, the base slumps into the snow; late in the flight they shrink
                    // and pop away.
                    float ease = 1.0 - (1.0 - pieces) * (1.0 - pieces);
                    float sc = max(1.0 - smoothstep(_ChunkHold, 1.0, pieces), 0.02);
                    size = hw * sc * 0.6;
                    float shoulder = len - min(hw * _TipLength, len * 0.6);
                    float2 cutLow = float2(0.0, len * 0.36);
                    float2 cutHigh = float2(0.0, len * 0.68);
                    float2 normalLow = normalize(float2(-0.45 * flip, 1.0));
                    float2 normalHigh = normalize(float2(0.5 * flip, 1.0));
                    float2 rootFromCentre = cr.rootRadius * float2(-sin(lean), cos(lean));   // from the gear's centre
                    // Every chunk lies within this of its middle: hw across, and along the axis to its break lines
                    // (0.16 - 0.18 len and 0.45 - 0.5 hw of slant), its shoulders or its tip; the zig-zag adds 0.09 hw.
                    float chunkLength = max(0.18 * len, 0.84 * len - shoulder) + 0.5 * hw;
                    float chunkRadius = sc * (sqrt(hw * hw + chunkLength * chunkLength) + 0.09 * hw);
                    [unroll] for (int i = 0; i < 3; i++)
                    {
                        float2 middle = float2(0.0, len * (i == 0 ? 0.18 : (i == 1 ? 0.52 : 0.84)));
                        float2 fly = (i == 0 ? float2(0.35, -0.15) : (i == 1 ? float2(1.0, 0.3) : float2(-0.65, 0.9)));
                        fly *= float2(flip, 1.0) * _BreakFling * ease;
                        float spin = (i == 1 ? -1.0 : 1.0) * flip * _BreakSpin * ease * (i == 0 ? 0.3 : 1.0);

                        // It never flies past the spill's edge, where the mesh would cut it off flat: if it would,
                        // it flies less far out (back down its crystal's axis), just enough to stay inside.
                        float2 centre = rootFromCentre + middle + fly;
                        float centreRadius = length(centre);
                        [branch] if (centreRadius + chunkRadius > cr.limit)
                        {
                            // How far out it reaches as it's spun: its furthest corner (plus the break lines'
                            // zig-zag), past its middle.
                            float2 h0, h1, h2, h3, h4, h5, h6;
                            ChunkCorners(i, hw, len, shoulder, hw * _RidgeShift, flip, middle, h0, h1, h2, h3, h4, h5, h6);
                            float furthest = max(max(max(length(centre + sc * IceRotate(h0, spin)), length(centre + sc * IceRotate(h1, spin))),
                                                     max(length(centre + sc * IceRotate(h2, spin)), length(centre + sc * IceRotate(h3, spin)))),
                                                 max(max(length(centre + sc * IceRotate(h4, spin)), length(centre + sc * IceRotate(h5, spin))),
                                                     length(centre + sc * IceRotate(h6, spin))));
                            // Bring its middle in (down the axis) by as much as that pokes past the edge.
                            float room = cr.limit - (furthest - centreRadius) - sc * 0.08 * hw;
                            float pull = centre.y - sqrt(max(room * room - centre.x * centre.x, 0.0));
                            fly.y -= clamp(pull, 0.0, max(middle.y + fly.y, 0.0));
                        }

                        [branch] if (length(q - middle - fly) < chunkRadius + cr.near)
                        {
                            float2 qi = IceRotate(q - middle - fly, -spin) / sc + middle;
                            half3 chunkFace;
                            float chunk = CrystalShape(qi, hw, len, px / sc, flip, seed, 0.0, chunkFace);
                            float slope;
                            float aboveLow = BreakLine(qi, cutLow, normalLow, hw, seed, slope);
                            float aboveHigh = BreakLine(qi, cutHigh, normalHigh, hw, seed + 0.5, slope);
                            chunk = i == 0 ? max(chunk, aboveLow) : (i == 1 ? max(chunk, max(-aboveLow, aboveHigh)) : max(chunk, -aboveHigh));
                            chunk *= sc;
                            face = chunk < sdf ? chunkFace : face;
                            sdf = min(sdf, chunk);
                        }
                    }
                }
                else
                {
                    // Whole: only inside a box round it, outside the snow.
                    [branch] if (abs(q.x) < hw + cr.near && q.y > -cr.near && q.y < len + cr.near && radius > cr.hidden - cr.near)
                    {
                        sdf = CrystalShape(q, hw, len, px, flip, seed, crack, face);
                    }
                }
                // Nothing below `hidden` (just inside the snow), so a leaning crystal's root corner can't poke out
                // under the snow.
                sdf = max(sdf, cr.hidden - radius);
                [branch] if (sdf < px)
                {
                    face = lerp(face, _SnowColor.rgb * 1.2, flash);
                    IceDraw(layer, sdf, face * bright, min(_ArcInkWidth * _InkShare, size * 0.3), px, 1.0);
                }

                [branch] if (smash > 0.0 && !sideCrystal && length(q - float2(0.0, len * 0.5)) < hw * _SmashSize + cr.near)
                {
                    // The smash: a white burst where it broke, for a frame or two.
                    float2 burstQ = IceRotate(q - float2(0.0, len * 0.5), seed * 6.28);
                    float burst = IceBurst(burstQ, hw * _SmashSize, 7.0, 3.2);
                    IceDrawInk(layer, burst, _SnowColor.rgb * _GlintBrightness, _CyanColor.rgb,
                               _ArcInkWidth * _InkShare * 0.7, px, smash);
                }

                [branch] if (glint > 0.0)
                {
                    // A glint just under the tip or at the lit shoulder, kept inside the spill.
                    float tip = min(hw * _TipLength, len * 0.6);
                    float glintSize = _GlintSize * glint;
                    float2 spot = glintSpot < 0.5 ? float2(hw * _RidgeShift, len - tip * 0.35) : float2(-hw, len - tip);
                    float2 spotP = root + float2(spot.x * c + spot.y * s, spot.y * c - spot.x * s);
                    glintSize = min(glintSize, max(cr.limit - length(spotP), 0.0));
                    [branch] if (glintSize > 0.01 && length(q - spot) < glintSize + cr.glintReach - _GlintSize)
                    {
                        IceGlint(layer, q - spot, glintSize, px, 1.0);
                    }
                }
            }

            // Cluster slot j: a tall middle crystal with one or two smaller ones fanning out, living through grow -
            // hold - crack - shatter, then regrowing as a new random cluster.
            void DrawCluster(inout half4 layer, float2 p, float radius, float beta, float j, IceCrown cr, float t,
                             float since, float energy, float px, half bright)
            {
                [branch] if (j < 0.0 || j >= cr.count)
                {
                    return;
                }

                // Its own clock: the slots run a beat apart, so one cluster breaks after another round the arc.
                float phase = t / _CyclePeriod + j / cr.count;
                float tau = frac(phase) * _CyclePeriod;
                float gen = IceTick(floor(phase));   // which cluster this is: a new random one each time round
                float age = min(tau - _RegrowGap, since);   // granting Ice regrows every cluster at once
                float breakAt = _CyclePeriod - _BreakTime;
                float crackAt = breakAt - _CrackTime;
                bool doomed = age > 0.25 && tau >= crackAt;
                bool broken = doomed && tau >= breakAt;

                float slotAlong = -cr.halfLength + (j + 0.5 + (IceRand(j, gen, 1.0) - 0.5) * _ClusterJitter) * cr.slotWidth;

                // For the cheap test below: how far a crystal can lean (degrees), how much wider than its width it
                // can get (springing, bobbing and popping for a glint all at once), and what else can stick out
                // past its sides (the shiver, a glint, anti-aliasing).
                float rock = abs(_HoldRock);
                float sideLeanLow = min(_SideLeanMin, _SideLeanMax) - rock;
                float sideLeanHigh = max(_SideLeanMin, _SideLeanMax) + rock;
                float widest = 1.4 * (1.0 + 0.6 * abs(_HoldBob)) * (1.0 + _TinkPop);
                float extra = abs(_ShiverShake) + cr.glintReach + cr.near;

                // Sides first, the middle crystal last, in front.
                [unroll] for (int c = 0; c < 3; c++)
                {
                    float side = c == 0 ? -1.0 : (c == 1 ? 1.0 : 0.0);
                    float hw = c == 2 ? _CrystalWidth : _CrystalWidth * _SideWidth;
                    float rootReach = cr.halfLength - hw - 0.3;   // every root stays in the snow, clear of the ends
                    float id = j * 3.0 + c;
                    float hA = c == 2 ? IceRand(id, gen, 5.0) : 0.0;   // the middle crystal's lean (a side's waits)
                    float centreLean = (hA - 0.5) * 2.0 * _CentreLean;

                    // Rule it out cheaply first, from where its root can stand (a side crystal's shifts by a fifth of
                    // the offset either way) and how far it can lean. A shattering crystal's chunks fly too far to
                    // bound this way; each is tested once it's placed.
                    float rootLow = clamp(slotAlong + side * _SideOffset * (side < 0.0 ? 1.2 : 0.8), -rootReach, rootReach);
                    float rootHigh = clamp(slotAlong + side * _SideOffset * (side < 0.0 ? 0.8 : 1.2), -rootReach, rootReach);
                    float leanLow = c == 2 ? centreLean - rock : (side < 0.0 ? -sideLeanHigh : sideLeanLow);
                    float leanHigh = c == 2 ? centreLean + rock : (side < 0.0 ? -sideLeanLow : sideLeanHigh);
                    bool mayReach = broken
                                    || CrystalMayReach(radius, beta - rootHigh / cr.midRadius, beta - rootLow / cr.midRadius,
                                                       cr.rootRadius, radians(min(leanLow, 0.0)), radians(max(leanHigh, 0.0)),
                                                       hw * widest + extra, cr.glintReach + cr.near);
                    [branch] if (!mayReach)
                    {
                        continue;
                    }
                    // It's there at all: every cluster has its middle crystal, and one or both side ones.
                    bool bothSides = IceRand(j, gen, 3.0) < _TwoSideChance;
                    float loneSide = IceRand(j, gen, 4.0) < 0.5 ? -1.0 : 1.0;
                    bool present = c == 2 || bothSides || side == loneSide;
                    [branch] if (!present)
                    {
                        continue;
                    }

                    hA = c == 2 ? hA : IceRand(id, gen, 5.0);
                    float hB = IceRand(id, gen, 6.0);
                    float along = clamp(slotAlong + side * _SideOffset * (0.8 + 0.4 * hA), -rootReach, rootReach);
                    float middleLength = lerp(_CrystalMinHeight, _CrystalMaxHeight, IceRand(j, gen, 2.0));
                    float lean = c == 2 ? centreLean : side * lerp(_SideLeanMin, _SideLeanMax, hB);
                    float len = c == 2 ? middleLength : middleLength * lerp(_SideHeightMin, _SideHeightMax, hB);

                    // Where the cluster is in its life.
                    float pieces = 0.0;
                    half smash = 0.0;
                    half crack = 0.0;
                    half flash = 0.0;
                    if (broken)
                    {
                        float brokenFor = IceFrames(tau - breakAt, _FrameRate);
                        pieces = max(brokenFor / _BreakTime, 1e-3);
                        smash = brokenFor < _SmashTime ? 1.0 : 0.0;
                    }
                    else if (doomed)
                    {
                        // The crack draws across on twos; the last frame before the break flashes white.
                        float crackFor = IceFrames(tau - crackAt, 12.0);
                        crack = (half)max(saturate(crackFor / max(_CrackTime - 1.0 / 12.0, 1e-3)), 0.15);
                        flash = tau >= breakAt - 1.0 / 24.0 ? 1.0 : 0.0;
                    }
                    half shiver = doomed && pieces <= 0.0 ? 1.0 : 0.0;

                    // Punch up out of the snow on a spring: overshoot, wobble, settle. Squash and stretch with it.
                    float grownFor = IceFrames(age - (c == 2 ? 0.0 : _GrowStagger * (1.0 + c)), _FrameRate);
                    float grow = grownFor > 0.0 ? 1.0 - exp(-_GrowDamping * grownFor) * cos(_GrowSpring * grownFor) : 0.0;
                    float squash = saturate(grow * 3.0) * clamp(1.0 + 0.6 * (1.0 - grow), 0.6, 1.4);

                    // Settled, it bobs and rocks a little, on twos, each crystal on its own beat.
                    float settled = saturate((grownFor - 0.45) * 4.0) * (1.0 - shiver);
                    float bob = sin(6.2831853 * (cr.holdTime * _HoldRate + hA)) * _HoldBob * settled;
                    lean += sin(6.2831853 * (cr.holdTime * _HoldRate * 0.7 + hB)) * _HoldRock * settled;

                    // Glints flick on a crystal now and then while it holds; it pops bigger for them ("tink").
                    half glint = 0.0;
                    float glintSpot = 0.0;
                    [branch] if (grow > 0.8 && pieces <= 0.0 && shiver <= 0.0)
                    {
                        float glintClock = t / _GlintEvery + IceRand(id, gen, 7.0) * 7.0;
                        float glintTick = IceTick(floor(glintClock));
                        float glintTime = IceFrames(frac(glintClock) * _GlintEvery, _FrameRate);
                        [branch] if (glintTime < _GlintLength)   // only while one could be on
                        {
                            glint = IceRand(id, glintTick, 8.0) < _GlintChance ? IcePop(glintTime, _GlintLength) : 0.0;
                            glintSpot = IceRand(id, glintTick, 10.0);
                        }
                    }
                    float tink = 1.0 + _TinkPop * step(0.5, glint);

                    float endFade = saturate((cr.halfLength - abs(along)) / _ClusterEndFade);
                    len = min(len * grow * energy * endFade * (1.0 + bob) * tink, cr.reachMax);   // stay inside the spill
                    hw *= squash * lerp(0.6, 1.0, endFade) * (1.0 - bob * 0.6) * tink;
                    // Don't lean out past the arc's ends.
                    float sinLean = clamp(sin(radians(lean)), (-(cr.halfLength + 0.25) - along) / max(len, 1e-3),
                                          (cr.halfLength + 0.25 - along) / max(len, 1e-3));
                    float shake = 0.0;
                    [branch] if (shiver > 0.0)
                    {
                        shake = (IceRand(id, cr.frame, 9.0) - 0.5) * 2.0 * _ShiverShake;
                    }

                    [branch] if (len > 0.04 && hw > 0.01)
                    {
                        DrawCrystal(layer, p, radius, cr, along / cr.midRadius, sinLean, hw, len, shake, flash, crack, pieces,
                                    smash, c != 2, hB, glint, glintSpot, px, bright);
                    }
                }
            }

            half4 IceFragment(ArcVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input);
                half4 tile = EFX_Over(ArcNeutral(input, f), ArcHalo(f, input.color.rgb));
                float px = ArcPixel(input);

                half energy = ArcEnergy();
                [branch] if (energy < 0.001)
                {
                    return tile;
                }

                float t = _Time.y;
                half takeover = ArcTakeover();
                half bright = _Emission * (1.0 + _AimGlow * _Highlight);
                float inkWidth = _ArcInkWidth * _InkShare;
                float inner = _ArcShape.y;
                float outer = _ArcShape.z;
                float thickness = ArcThickness();
                float midRadius = (inner + outer) * 0.5;
                float halfLength = ArcHalfLength();

                // The arc's flat frame (see above): rotate so the arc's middle points up.
                float centreC = cos(_ArcShape.w);
                float centreS = sin(_ArcShape.w);
                float2 p = float2(input.positionOS.x * centreS - input.positionOS.y * centreC,
                                  input.positionOS.x * centreC + input.positionOS.y * centreS);
                float radius = length(p);
                float beta = atan2(p.x, p.y);          // radians clockwise from the arc's middle
                float along = beta * midRadius;        // world units along the mid radius from the middle

                half4 layer = half4(0.0, 0.0, 0.0, 0.0);

                // The band: big flat facets. Only where the band is.
                [branch] if (f.sdf < px)
                {
                    // Near the band an unrolled frame (along the mid radius, out from the centre) is close enough
                    // to flat, and needs no trig. Only the row the pixel is in, both along the girdle.
                    float2 flat = float2(along, radius);
                    // The girdle's corners round this pixel (the even ones; both rows share them).
                    float girdleK = floor((flat.x + halfLength) / (2.0 * _FacetSize)) * 2.0;
                    float2 girdle1 = GirdleCorner(girdleK, halfLength);
                    float2 girdle2 = GirdleCorner(girdleK + 2.0, halfLength);
                    float girdle = GirdleSide(flat, girdleK, girdle1, girdle2, halfLength);
                    IceGleam gleam = GetGleam();
                    half3 rgb;
                    [branch] if (girdle > px)
                    {
                        rgb = FacetRow(flat, girdle1, girdle2, outer + 0.5, 47.7, true, halfLength, gleam, px);
                    }
                    else if (girdle < -px)
                    {
                        rgb = FacetRow(flat, girdle1, girdle2, inner - 0.5, 53.9, false, halfLength, gleam, px);
                    }
                    else
                    {
                        rgb = lerp(FacetRow(flat, girdle1, girdle2, inner - 0.5, 53.9, false, halfLength, gleam, px),
                                   FacetRow(flat, girdle1, girdle2, outer + 0.5, 47.7, true, halfLength, gleam, px),
                                   EFX_FillPx(-girdle, px));
                    }
                    rgb = lerp(rgb, _DeepColor.rgb, EFX_FillPx(abs(girdle) - inkWidth * _GirdleLine * 0.5, px));

                    // Cartoon glass shine: two spots of short parallel strokes, somewhere new each grant.
                    float shineRadius = inner + thickness * _ShineHeight;
                    float shineReach = _ShineGap + _ShineLength * 0.5 + _ShineWidth + px;   // no stroke reaches further
                    [branch] if (abs(radius - shineRadius) < shineReach)
                    {
                        float grant = IceTick(floor(abs(_FlareTime) * 10.0));
                        float2 dir = float2(sin(radians(_ShineSlant)), cos(radians(_ShineSlant)));
                        float2 across = float2(dir.y, -dir.x);
                        float marks = 1e3;
                        [unroll] for (int m = 0; m < 2; m++)
                        {
                            float spot = (m == 0 ? _ShineSpotA : _ShineSpotB) + (IceRand(grant, m, 33.0) - 0.5) * 2.0 * _ShineShuffle;
                            float2 q = float2((beta - spot / midRadius) * shineRadius, radius - shineRadius);
                            [branch] if (dot(q, q) < shineReach * shineReach)
                            {
                                [unroll] for (int s = 0; s < 3; s++)
                                {
                                    float strokeLength = _ShineLength * (s == 0 ? 1.0 : (s == 1 ? 0.62 : 0.32)) * (m == 1 && s == 2 ? 0.0 : 1.0);
                                    float2 mid = across * (s - 1.0) * _ShineGap;
                                    float stroke = IceCapsule(q, mid - dir * strokeLength * 0.5, mid + dir * strokeLength * 0.5, _ShineWidth);
                                    marks = strokeLength > 0.0 ? min(marks, stroke) : marks;
                                }
                            }
                        }
                        rgb = lerp(rgb, _SnowColor.rgb, EFX_FillPx(marks, px));
                    }

                    // A pale rim light just inside the inner outline, so the side facing the player stays crisp.
                    half rimSide = saturate(-f.p.y / max(thickness * 0.25, 1e-3));
                    rgb = lerp(rgb, _PaleColor.rgb, (1.0 - EFX_FillPx(f.sdf + inkWidth + _InnerRim, px)) * rimSide);
                    rgb *= bright;

                    rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_FillPx(f.sdf + inkWidth, px));
                    layer = EFX_Over(half4(rgb, EFX_FillPx(f.sdf, px) * takeover), layer);

                    // The odd snappy glint on the band.
                    float cell = floor((along + halfLength) / _BandGlintSpacing);
                    float glintClock = t / _GlintEvery + EFX_Hash21(float2(cell, 83.9)) * 5.0;
                    float glintTick = IceTick(floor(glintClock));
                    float glintTime = IceFrames(frac(glintClock) * _GlintEvery, _FrameRate);
                    [branch] if (glintTime < _GlintLength)   // only while one could be on
                    {
                        half pop = IceRand(cell, glintTick, 11.0) < _GlintChance ? IcePop(glintTime, _GlintLength) : 0.0;
                        float glintAlong = (cell + 0.25 + 0.5 * IceRand(cell, glintTick, 12.0)) * _BandGlintSpacing - halfLength;
                        float glintRadius = inner + thickness * (0.2 + 0.4 * IceRand(cell, glintTick, 13.0));
                        float2 glintQ = float2((beta - glintAlong / midRadius) * glintRadius, radius - glintRadius);
                        float glintSize = _GlintSize * 0.8 * pop;
                        [branch] if (glintSize > 0.01 && length(glintQ) < glintSize + 6.0 * px)
                        {
                            IceGlint(layer, glintQ, glintSize, px, takeover);
                        }
                    }
                }

                // The snow cap's shape along the outer edge (unrolled at the outer edge: flat enough for it).
                float sOuter = beta * outer;
                float2 flatOuter = float2(sOuter, radius);
                float halfOuter = halfLength * outer / midRadius;
                float waveK = 6.2831853 / _CrustWaveLength;
                float bottom = outer - _CrustDepth * takeover;
                float wave = _CrustWave * takeover;
                float underside = bottom + wave * sin(sOuter * waveK);
                float boilFrame = IceTick(floor(t * _CrustBoilRate));

                // Icicles hang from the snow's underside over the band, under the snow.
                [branch] if (radius > bottom - _IcicleMaxLength - 0.3 && radius < outer)
                {
                    float spotF = sOuter / _IcicleSpacing;
                    float spotCell = floor(spotF);
                    float nearer = frac(spotF) < 0.5 ? spotCell - 1.0 : spotCell + 1.0;
                    [unroll] for (int i = 0; i < 2; i++)
                    {
                        float id = i == 0 ? spotCell : nearer;
                        float x = (id + 0.5 + (EFX_Hash21(float2(id, 51.3)) - 0.5) * 0.5) * _IcicleSpacing;
                        // Only where one hangs, and only beside it (the widest is 1.25 _IcicleWidth either side).
                        [branch] if (EFX_Hash21(float2(id, 50.1)) < _IcicleChance
                                     && abs(flatOuter.x - x) < 1.25 * _IcicleWidth * takeover + 0.015 + px)
                        {
                            float endFade = saturate((halfOuter - 0.4 - abs(x)) / 0.6);
                            float boil = 1.0 + (IceRand(id, boilFrame, 16.0) - 0.5) * 2.0 * _CrustBoil;
                            float len = lerp(_IcicleMinLength, _IcicleMaxLength, EFX_Hash21(float2(id, 52.7))) * takeover * endFade * boil;
                            float hw = _IcicleWidth * (0.75 + 0.5 * EFX_Hash21(float2(id, 53.9))) * takeover;
                            float top = bottom + wave * sin(x * waveK) + 0.12;   // its root tucked up into the snow
                            float tipY = top - 0.12 - len;
                            float2 q = flatOuter - float2(x, tipY);
                            float icicle = IceTriangle(q, float2(hw, top - tipY)) - 0.015;
                            // Lit left half frost, the right half pale, a white stroke down the lit side.
                            half3 rgb = lerp(_PaleColor.rgb, _FrostColor.rgb, EFX_FillPx(q.x, px));
                            float height = top - tipY;
                            float shine = IceCapsule(q, float2(-hw * 0.16, height * 0.35), float2(-hw * 0.38, height * 0.85), hw * 0.13);
                            rgb = lerp(rgb, _SnowColor.rgb, EFX_FillPx(shine, px));
                            IceDraw(layer, len > 0.05 ? icicle : 1e3, rgb * bright, min(inkWidth * _CrustInk * 0.8, hw * 0.3), px, takeover);
                        }
                    }
                }

                // The crown: the cluster this pixel is over and its nearer neighbour. Only above the roots.
                [branch] if (radius > _ArcShape.y + thickness * _CrystalRoot - 1.0)
                {
                    IceCrown cr;
                    cr.halfLength = halfLength;
                    cr.midRadius = midRadius;
                    cr.rootRadius = inner + thickness * _CrystalRoot;
                    cr.reachMax = outer + _CrystalReachLimit - cr.rootRadius;
                    cr.hidden = outer - _CrustDepth + _CrustWave + 0.04;   // inside the snow, above its underside
                    cr.limit = outer + 3.45;
                    cr.count = round(_ClusterCount);
                    cr.slotWidth = 2.0 * halfLength / cr.count;
                    cr.frame = IceTick(floor(t * (_FrameRate > 0.0 ? _FrameRate : 24.0)));
                    cr.holdTime = IceFrames(t, _HoldFrameRate);
                    cr.near = 4.0 * px;
                    cr.glintReach = _GlintSize + 6.0 * px;

                    float slot = (along + halfLength) / cr.slotWidth;
                    float j = floor(slot);
                    float neighbour = frac(slot) < 0.5 ? j - 1.0 : j + 1.0;
                    float since = t - _FlareTime;
                    DrawCluster(layer, p, radius, beta, min(j, neighbour), cr, t, since, energy, px, bright);
                    DrawCluster(layer, p, radius, beta, max(j, neighbour), cr, t, since, energy, px, bright);
                }

                // The snow cap along the outer edge, over the crystals' roots and the icicles' tops.
                [branch] if (radius > bottom - _CrustWave - 0.2 && radius < outer + _CrustLumpRise * (1.0 + _CrustBoil) + 0.2)
                {
                    float cell = floor(sOuter / _CrustLumpSpacing);
                    float crust = max(radius - outer, underside - radius);   // a band with a wavy underside
                    [unroll] for (int i = -1; i <= 1; i++)
                    {
                        float id = cell + i;
                        float h1 = EFX_Hash21(float2(id, 11.1));
                        float h2 = EFX_Hash21(float2(id, 13.7));
                        float h3 = EFX_Hash21(float2(id, 19.3));
                        float boil = 1.0 + (IceRand(id, boilFrame, 14.0) - 0.5) * 2.0 * _CrustBoil;

                        // A soft drift, a small bump, or nothing, sunk into the snow so only its cap shows above
                        // the band: clipped there, so the underside stays a clean line.
                        float scale = h2 < _CrustFlatChance ? 0.0 : (h2 < _CrustFlatChance + _CrustSmallChance ? 0.45 : 1.0);
                        float lumpAlong = (id + 0.5 + (h1 - 0.5) * 0.4) * _CrustLumpSpacing;
                        float lumpFade = saturate((halfOuter - abs(lumpAlong)) / 0.8);
                        float lumpSize = (_CrustLumpSize + _CrustLumpSpread * h3) * scale * takeover * boil;
                        float lumpRise = _CrustLumpRise * (0.45 + 0.55 * h3) * scale * lumpFade * takeover * boil;
                        float2 lumpQ = flatOuter - float2(lumpAlong, outer + lumpRise - lumpSize);
                        float stretch = _CrustLumpStretch * (0.8 + 0.4 * h1);
                        float lump = length(float2(lumpQ.x / stretch, lumpQ.y)) - lumpSize;   // a wide, low mound
                        lump = max(lump, (underside + 0.03) - radius);
                        crust = scale > 0.0 ? IceSmoothMin(crust, lump, _CrustValley) : crust;
                    }
                    // Ends with the tile, overhanging it a little, its corners rounded off.
                    float endCut = abs(f.p.x) - (f.halfSize.x + _CrustOverhang);
                    crust = -IceSmoothMin(-crust, -endCut, 0.22);

                    // Two tones: white snow on top, a pale-blue shaded underside following the wave.
                    float shadeLine = underside + _CrustShadeHeight;
                    half3 rgb = lerp(_SnowColor.rgb, _PaleColor.rgb, EFX_FillPx(radius - shadeLine, px)) * bright;
                    IceDraw(layer, crust, rgb, inkWidth * _CrustInk, px, takeover);
                }

                return EFX_Over(layer, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
