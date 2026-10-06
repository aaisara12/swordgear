Shader "Swordgear/Gear Arc Wind"
{
    // Wind's gear section. Idle or aimed at, it's the shared cartoon tile in Wind's colour. Active, the tile
    // becomes a gust. The band turns flat mint-green air, a deeper green rippling along its inner edge and paler
    // currents streaming through it. Out of it, big cartoon gust ribbons whoosh up past the gear: each starts
    // low inside the band, swoops up out of it in an S and winds into a fat curl at its head (some curl down
    // instead) - flat white with a mint underside and a deep teal ink outline, like the wind in a storybook.
    // A ribbon shoots out in a fraction of a second, its curl winding up as it goes, rides along the gear for a
    // couple of seconds, then its tail chases its head into the curl and it's gone, and another whooshes out;
    // neighbouring ribbons are staggered so there's always a crown of two to four. Near the upstream end they
    // pull out of the end of the arc; near the downstream end they shrink back into the band. Behind them race
    // white speed lines: each shoots out from a point, round-headed with a long tapering tail and a flat green
    // shadow under it, then its tail catches up and it's gone. They race in lanes that follow the arc's curve,
    // thinning out toward the top, and near the downstream end the lanes bend outward so the lines peel off the
    // gear. Every second or two a gust surges: everything holds back for a beat, then lurches ahead, the lines
    // and ribbons stretch and the air flashes paler, then it settles back. Ribbons and lines pop in (with a
    // little overshoot) as the arc activates; the band takes over the tile a beat later. Switching away, they
    // shrink back with their ink and shadows thinning alongside, and fade out over the last stretch.
    //
    // It's costly maths on a big patch of screen, so every piece (the band, each lane of lines, each ribbon and
    // each part of a ribbon) is only worked out where it can show, behind generous bounds that cut nothing drawn.
    //
    // The loose pieces (tumbling lime leaves) are particles, not this shader: ArcBitsWind.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
    Properties
    {
        [Header(Colours)]
        _BandColor ("Band (the air the ribbons stream out of)", Color) = (0.62, 0.92, 0.72, 1)
        _AirColor ("Gust Ribbons", Color) = (0.9, 0.99, 0.94, 1)
        _ShadeColor ("Air Shade (the ribbons' undersides)", Color) = (0.56, 0.89, 0.7, 1)
        _DeepColor ("Deep Shade (along the band's inner edge)", Color) = (0.3, 0.77, 0.56, 1)
        _LineColor ("Speed Lines and Ribbon Shine", Color) = (1, 1, 1, 1)
        _LineShadowColor ("Speed Line Shadow", Color) = (0.36, 0.8, 0.6, 1)
        _InkColor ("Ink", Color) = (0.06, 0.32, 0.24, 1)
        _Emission ("Air Brightness (HDR)", Range(0.5, 2)) = 0.95
        _RibbonGlow ("Gust Ribbon Brightness (HDR)", Range(0.5, 3)) = 1.2
        _LineGlow ("Speed Line Brightness (HDR)", Range(0.5, 3)) = 1.15

        [Header(Flow)]
        [ToggleUI] _Anticlockwise ("Blow Anticlockwise (off = clockwise)", Float) = 0
        _GustSpeed ("Calm Speed (world units / s)", Range(0, 20)) = 6
        _SurgeEvery ("Time Between Surges (seconds, on average)", Range(0.3, 6)) = 1.6
        _SurgeLength ("Surge Length (seconds)", Range(0.05, 2)) = 0.45
        _SurgePeak ("Surge Top Speed (times the calm speed)", Range(1, 6)) = 3
        _SurgeWindUp ("Hold Back Before A Surge (seconds)", Range(0, 0.6)) = 0.15
        _SurgeWindUpSpeed ("Hold Back Speed (times the calm speed)", Range(0, 1)) = 0.4
        _SurgeFlash ("Surge Flash (share of the way the air goes white at a surge's peak)", Range(0, 1)) = 0.3

        [Header(Band)]
        _ShadeHeight ("Deep Shade Height (share of the band, from the inner edge)", Range(0, 0.6)) = 0.2
        _ShadeWave ("Deep Shade Wave Height (world units)", Range(0, 0.5)) = 0.1
        _ShadeWavelength ("Deep Shade Wavelength (world units)", Range(0.5, 8)) = 3.5
        _ShadeSpeed ("Deep Shade Speed (share of the calm speed)", Range(0, 3)) = 1.2
        _WaveLean ("Deep Shade Wave Lean (0 even waves, 0.9 steep fronts)", Range(0, 0.9)) = 0.6
        _CurrentColor ("Band Currents", Color) = (0.8, 0.97, 0.86, 1)
        _CurrentWidth ("Band Current Thickness (world units, at its head)", Range(0, 1)) = 0.5
        _CurrentLength ("Band Current Length (world units)", Range(0.5, 10)) = 4.5
        _CurrentSpacing ("Band Current Spacing (world units along the arc, one in each)", Range(2, 16)) = 7
        _CurrentSpeed ("Band Current Speed (share of the calm speed)", Range(0, 3)) = 1.3
        _CurrentHeights ("Band Current Heights (x and y: shares of the band, 0 inner - 1 outer)", Vector) = (0.42, 0.74, 0, 0)

        [Header(Gust ribbons)]
        _RibbonSpacing ("Ribbon Spacing (world units along the arc, one ribbon in each)", Range(2.5, 10)) = 2.8
        _RibbonSpeed ("Ribbon Drift (share of the calm speed)", Range(0, 2)) = 0.45
        _RibbonLife ("Ribbon Life (seconds)", Range(0.4, 6)) = 2.4
        _RibbonGrow ("Whoosh Out (share of the life)", Range(0.05, 0.6)) = 0.2
        _RibbonFade ("Tail Chases The Head From (share of the life)", Range(0.2, 0.95)) = 0.8
        _RibbonRest ("Gone Before The Next One (share of the life)", Range(0, 0.5)) = 0.04
        _RibbonWidth ("Ribbon Thickness (world units, inside the ink)", Range(0.1, 1)) = 0.52
        _RibbonLength ("Ribbon Length, Root To Curl (world units)", Range(1, 7)) = 3.4
        _RibbonLengthSpread ("Ribbon Length Variety (world units added on top)", Range(0, 3)) = 1.6
        _RibbonSteepness ("Steepest Rise (world units up per unit along, root to head)", Range(0.1, 0.9)) = 0.42
        _RibbonRoot ("Ribbon Root Depth (world units below the band's outer edge)", Range(0, 2.5)) = 0.5
        _RibbonRise ("Lowest Ribbon Head (world units past the band)", Range(0, 2)) = 0.5
        _RibbonRiseSpread ("Ribbon Height Variety (world units added on top)", Range(0, 2)) = 0.7
        _RibbonReach ("Reach Past The Gear (world units, curls included)", Range(1, 3.5)) = 3.3
        _RibbonShine ("Shine Stripe (share of the thickness, along the top)", Range(0, 0.8)) = 0
        _RibbonShade ("Underside Shade (share of the thickness)", Range(0, 0.8)) = 0.3
        _RibbonBandInk ("Ribbon Ink Inside The Band (share of the outline's width)", Range(0, 1)) = 0.5
        _RibbonStretch ("Surge Stretch (share of a ribbon's length added at a surge's peak)", Range(0, 1)) = 0.25

        [Header(Curls)]
        _CurlSize ("Curl Size (world units, radius)", Range(0.3, 1.4)) = 1.0
        _CurlSizeSpread ("Curl Size Variety (share smaller, at most)", Range(0, 0.8)) = 0.3
        _CurlTurns ("Curl Turns", Range(0.5, 1.5)) = 0.9
        _CurlTip ("Curl Tightness (share of its radius left at the end; never tighter than its thickness allows)", Range(0.1, 0.8)) = 0.35
        _CurlTipWidth ("Curl Tip Thickness (share of the ribbon's)", Range(0.1, 1)) = 0.45
        _CurlDownChance ("Curl Downward Chance (0-1 per ribbon)", Range(0, 1)) = 0.2

        [Header(Speed lines)]
        _LaneHeight ("Lane Height (world units)", Range(0.4, 1.5)) = 0.85
        _LineLowest ("Lowest Lane (world units out from the inner edge)", Range(0, 3)) = 1.3
        _LineReach ("Reach Past The Gear (world units)", Range(0, 3.3)) = 2.7
        _LaneSpeedMin ("Slowest Lane (share of the calm speed)", Range(0, 3)) = 0.9
        _LaneSpeedSpread ("Lane Speed Variety (share of the calm speed added on top)", Range(0, 3)) = 0.45
        _LineSpacing ("Line Slot Length (world units, one line in each)", Range(2, 14)) = 8
        _LineThinning ("Thinning Toward The Top (share longer slots at the top of the reach)", Range(0, 3)) = 0.8
        _LineLengthMin ("Shortest Line (world units)", Range(0.5, 8)) = 2.6
        _LineLengthSpread ("Line Length Variety (world units added on top)", Range(0, 8)) = 2.2
        _LineWidth ("Line Thickness At The Head (world units, inside the ink)", Range(0.04, 0.5)) = 0.17
        _LineWidthSpread ("Line Thickness Variety (share thinner, at most)", Range(0, 1)) = 0.3
        _LineLife ("Line Life (seconds)", Range(0.2, 3)) = 0.75
        _LineLift ("Line Lift (world units outward per unit forward, at most)", Range(0, 0.3)) = 0.1
        _LaneWobble ("Line Wobble Across Its Lane (world units)", Range(0, 0.3)) = 0.08
        _LineInkWidth ("Line Ink Width (world units)", Range(0, 0.2)) = 0.06
        _LineShadowDrop ("Shadow Drop (world units, toward the gear)", Range(0, 0.3)) = 0.06
        _SurgeStretch ("Surge Stretch (share of a line's length added at a surge's peak)", Range(0, 2)) = 0.5
        _Boil ("Boil (share of the thickness, redrawn 12 times a second)", Range(0, 0.6)) = 0.1

        [Header(Peel off and ends)]
        _PeelLength ("Peel-Off Stretch (world units before the downstream end)", Range(0.1, 6)) = 3.2
        _PeelLift ("Peel-Off Lift (world units, at the end)", Range(0, 3)) = 1.6
        _EndFade ("Die Down Toward The Ends (world units)", Range(0.05, 4)) = 1.1


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
                half4 _BandColor; half4 _AirColor; half4 _ShadeColor; half4 _DeepColor; half4 _LineColor; half4 _LineShadowColor; \
                half4 _InkColor; half _Emission; half _RibbonGlow; half _LineGlow; \
                float _Anticlockwise; float _GustSpeed; float _SurgeEvery; float _SurgeLength; float _SurgePeak; \
                float _SurgeWindUp; float _SurgeWindUpSpeed; half _SurgeFlash; \
                float _ShadeHeight; float _ShadeWave; float _ShadeWavelength; float _ShadeSpeed; float _WaveLean; \
                half4 _CurrentColor; float _CurrentWidth; float _CurrentLength; float _CurrentSpacing; float _CurrentSpeed; \
                float4 _CurrentHeights; \
                float _RibbonSpacing; float _RibbonSpeed; float _RibbonLife; float _RibbonGrow; float _RibbonFade; \
                float _RibbonRest; float _RibbonWidth; float _RibbonLength; float _RibbonLengthSpread; \
                float _RibbonSteepness; float _RibbonRoot; float _RibbonRise; float _RibbonRiseSpread; float _RibbonReach; \
                float _RibbonShine; float _RibbonShade; float _RibbonBandInk; float _RibbonStretch; \
                float _CurlSize; float _CurlSizeSpread; float _CurlTurns; float _CurlTip; float _CurlTipWidth; \
                float _CurlDownChance; \
                float _LaneHeight; float _LineLowest; float _LineReach; float _LaneSpeedMin; float _LaneSpeedSpread; \
                float _LineSpacing; float _LineThinning; float _LineLengthMin; float _LineLengthSpread; \
                float _LineWidth; float _LineWidthSpread; float _LineLife; float _LineLift; float _LaneWobble; \
                float _LineInkWidth; float _LineShadowDrop; float _SurgeStretch; float _Boil; \
                float _PeelLength; float _PeelLift; float _EndFade;
            #include "GearArcCommon.hlsl"

            #define WIND_TAU 6.2831853

            // ---------- The gusty clock ----------
            // Calm most of the time; once a period, at a random moment, it holds back for a beat (runs slow), then
            // surges (runs fast) so everything riding it lurches ahead together, then cruises on. It's a warp of
            // time, so nothing ever jumps. The time is split into whole 1024 s blocks plus the rest, so a clock
            // that has been running for hours keeps its precision.
            struct GustClock
            {
                float blocks;   // the time in whole 1024 s blocks, seconds
                float rest;     // the remainder, under 1024 s
                float rate;     // the clock's average rate
                float ahead;    // seconds the clock runs ahead of rate * time; bounded, back to 0 each period
                float surge;    // 0 calm .. 1 at a surge's peak
            };

            GustClock GetGustClock(float t)
            {
                GustClock c;
                c.blocks = floor(t / 1024.0) * 1024.0;
                c.rest = t - c.blocks;

                // The hold-back and the surge are smoothsteps the clock steps back and then forward along; a
                // smoothstep's slope peaks at 1.5 / its width, so `share` sets the surge's top speed and `holdShare`
                // the hold-back's bottom speed.
                float period = max(_SurgeEvery, 0.05);
                float halfWindow = max(0.49 * saturate(_SurgeLength / period), 0.01);
                float windUp = clamp(_SurgeWindUp / period, 0.0, max(0.98 - 2.0 * halfWindow, 0.0));
                float share = (_SurgePeak - 1.0) * 2.0 * halfWindow / 1.5;
                float holdShare = (1.0 - _SurgeWindUpSpeed) * windUp / 1.5;

                float u = frac(frac(c.blocks / period) + c.rest / period);
                float n = floor(t / period);
                n -= 256.0 * floor(n / 256.0);
                float earliest = halfWindow + windUp;
                float middle = earliest + (1.0 - halfWindow - earliest) * EFX_Hash21(float2(n, 3.7));
                float s = saturate((u - middle + halfWindow) / (2.0 * halfWindow));
                float w = saturate((u - (middle - halfWindow - windUp)) / max(windUp, 1e-4));

                c.rate = 1.0 + share - holdShare;
                c.ahead = period * (share * s * s * (3.0 - 2.0 * s) - holdShare * w * w * (3.0 - 2.0 * w)
                                    - (share - holdShare) * u);
                c.surge = 4.0 * s * (1.0 - s);
                return c;
            }

            // How far something riding the gusty clock at `speed` (world units per calm second) has travelled,
            // wrapped to 0..wrap. Everything that moves is drawn at (position - Travel), so the numbers stay small.
            float Travel(GustClock c, float speed, float wrap)
            {
                float r = speed * c.rate / wrap;
                return frac(frac(r * c.blocks) + r * c.rest + speed * c.ahead / wrap) * wrap;
            }

            // Where something that lives `life` seconds (on the gusty clock) is in its life (x, 0..1), and which
            // life it's on (y, 0..63, repeating). `offset` shifts it, in lives.
            float2 GustCycle(GustClock c, float life, float offset)
            {
                float r = c.rate / life;
                float x = frac(r * c.blocks / 64.0) * 64.0 + r * c.rest + c.ahead / life + offset;
                float n = floor(x);
                return float2(x - n, n - 64.0 * floor(n / 64.0));
            }

            float EaseOut3(float x) { x = saturate(x); float i = 1.0 - x; return 1.0 - i * i * i; }
            float EaseIn2(float x) { x = saturate(x); return x * x; }
            // Overshoots to about 1.1 before settling on 1: a cartoon pop.
            float EaseOutBack(float x) { x = saturate(x); float i = x - 1.0; return 1.0 + 2.70158 * i * i * i + 1.70158 * i * i; }

            // A travelling wave that leans forward: gentle backs, steep fronts. Returns its height (x) and its
            // slope per unit of `along` (y).
            float2 LeanWave(float along, float wavelength, float shift)
            {
                float k = WIND_TAU / wavelength;
                float theta = k * along + shift;
                float warped = theta - _WaveLean * sin(theta);
                return float2(sin(warped), cos(warped) * (1.0 - _WaveLean * cos(theta)) * k);
            }

            // ---------- Gust ribbons ----------
            struct Ribbon
            {
                float len;          // root to head, along the flow (world units)
                float drop;         // how far the root sits below the head
                float curlRadius;
                float curlDir;      // 1 curls up and back over itself, -1 curls down
                float shrink;       // curl radius lost per radian wound
                float turns;        // radians the curl winds
                float halfWidth;
                float2 drawn;       // the stretch drawn, in progress: 0 the root, 1 the head, 2 the curl's end
            };

            // A ribbon's thickness at `progress`: tapering in from wherever its tail is drawn from, and thinning
            // toward the curl's tip.
            float RibbonTaper(Ribbon r, float progress)
            {
                float fromTail = saturate((progress - r.drawn.x) / 0.4);
                float tip = lerp(1.0, _CurlTipWidth, smoothstep(1.45, 2.0, progress));
                return sqrt(fromTail) * tip;
            }

            // One gust ribbon in its own frame: `q` is the pixel from the ribbon's head, world units (x along the
            // flow, y outward). The body is an S of two equal arcs, from the root (level, `drop` below the head and
            // `len` behind it) up to the head (level again); then it winds into a curl of `curlRadius` that starts
            // at the head, heads forward and rolls back over itself (under itself if curlDir is -1), tightening as
            // it goes. Returns the distance to its middle line (x, world units), its half-thickness there (y) and
            // how far the pixel sits toward its top side (z, world units). `reach` is the farthest from the middle
            // line a pixel can still be coloured (the half-thickness, the ink and a pixel): a part of the ribbon
            // whose circle (or the curl's ring) lies farther than that from the pixel isn't measured, which leaves
            // what's drawn unchanged.
            float3 GustRibbon(float2 q, Ribbon r, float reach)
            {
                float3 best = float3(1e5, 0.0, 0.0);
                float bendAngle = 2.0 * atan(r.drop / r.len);          // each arc of the S turns this far
                float bend = r.len / (2.0 * max(sin(bendAngle), 1e-3)); // and has this radius

                // Which arc to measure: the two meet halfway, where the normal line splits the plane between them
                // (only one is measured, unless just one is drawn at all).
                float side = dot(q + 0.5 * float2(r.len, r.drop), float2(cos(bendAngle), sin(bendAngle)));

                // The lower arc, from the root curving up (centred above the root).
                float2 relLow = q - float2(-r.len, bend - r.drop);
                float fromLow = length(relLow);
                [branch] if (r.drawn.x < 0.5 && (side < 0.0 || r.drawn.y <= 0.5) && abs(fromLow - bend) < reach)
                {
                    float angle = atan2(relLow.x, -relLow.y);
                    float closest = clamp(angle, 2.0 * bendAngle * r.drawn.x, 2.0 * bendAngle * min(r.drawn.y, 0.5));
                    float width = r.halfWidth * RibbonTaper(r, 0.5 * closest / bendAngle);
                    float d = length(relLow - bend * float2(sin(closest), -cos(closest)));
                    best = float3(d, width, bend - fromLow);
                }

                // The upper arc, levelling out into the head (centred below the head).
                float2 relUp = q + float2(0.0, bend);
                float fromUp = length(relUp);
                [branch] if (r.drawn.x < 1.0 && r.drawn.y > 0.5 && (side >= 0.0 || r.drawn.x >= 0.5)
                             && abs(fromUp - bend) < reach)
                {
                    float angle = atan2(relUp.x, relUp.y);
                    float closest = clamp(angle, bendAngle * (2.0 * max(r.drawn.x, 0.5) - 2.0),
                                          bendAngle * (2.0 * min(r.drawn.y, 1.0) - 2.0));
                    float width = r.halfWidth * RibbonTaper(r, 1.0 + 0.5 * closest / bendAngle);
                    float d = length(relUp - bend * float2(sin(closest), cos(closest)));
                    if (d - width < best.x - best.y)
                    {
                        best = float3(d, width, fromUp - bend);
                    }
                }

                // The curl: an Archimedean spiral starting at the head. Check the two windings either side of the
                // pixel. Its drawn stretch lies in a ring round the curl's middle, from its last winding's radius
                // to its first's.
                float2 rel = float2(q.x, q.y * r.curlDir - r.curlRadius);
                float radius = length(rel);
                float first = max(r.drawn.x - 1.0, 0.0) * r.turns;
                float last = (r.drawn.y - 1.0) * r.turns;
                [branch] if (r.drawn.y > 1.0 && radius < r.curlRadius - r.shrink * first + reach
                             && radius > r.curlRadius - r.shrink * last - reach)
                {
                    float theta = atan2(rel.x, -rel.y);   // 0 straight below the curl's middle (at the head)
                    float k = floor(((r.curlRadius - radius) / max(r.shrink, 1e-4) - theta) / WIND_TAU);
                    [unroll] for (int j = 0; j < 2; j++)
                    {
                        float phi = clamp(theta + WIND_TAU * (k + j), first, last);
                        float onRadius = r.curlRadius - r.shrink * phi;
                        float width = r.halfWidth * RibbonTaper(r, 1.0 + phi / max(r.turns, 1e-3));
                        float d = length(rel - onRadius * float2(sin(phi), -cos(phi)));
                        if (d - width < best.x - best.y)
                        {
                            best = float3(d, width, (onRadius - radius) * r.curlDir);
                        }
                    }
                }
                return best;
            }

            // ---------- Speed lines ----------
            // A cone with round ends: radius r1 at the origin, r2 at (0, h). Exact signed distance.
            float SdTaper(float2 p, float r1, float r2, float h)
            {
                p.x = abs(p.x);
                float b = (r1 - r2) / h;
                float a = sqrt(saturate(1.0 - b * b));
                float k = dot(p, float2(-b, a));
                if (k < 0.0) return length(p) - r1;
                if (k > a * h) return length(p - float2(0.0, h)) - r2;
                return dot(p, float2(a, b)) - r1;
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

                GustClock clock = GetGustClock(_Time.y);
                float flow = _Anticlockwise > 0.5 ? 1.0 : -1.0;   // the arc's own frame runs anticlockwise
                float a = f.p.x * flow;        // along the flow from the arc's middle, world units at this radius
                float y = input.uvWorld.y;     // out from the band's inner edge, world units
                float thickness = ArcThickness();
                float halfLength = f.halfSize.x;
                float px = ArcPixel(input);
                float pop = EaseOutBack(energy / 0.85);          // shapes pop in (with an overshoot) as it activates
                // As the arc winds down the shapes shrink away: their ink and the lines' shadows thin with them (but
                // don't overshoot with the pop), and over the last stretch they fade out, so no dark ink threads are
                // left racing over the tile. Both are 1 once the arc is well in play.
                float shrink = min(pop, 1.0);
                half fade = saturate(energy * 4.0);
                float boilTick = floor(frac(clock.rest * 0.1875) * 64.0);   // 12 a second, 64 different
                half inBand = EFX_FillPx(f.sdf, px);
                float ink = _ArcInkWidth;

                // The speed lines' lanes bend outward near the downstream end (the inner ones most), so the lines
                // peel off the gear. `lanes` is the height in lane terms. Its slope is taken here, outside branches.
                float peel = saturate((a - (halfLength - _PeelLength)) / max(_PeelLength, 0.01));
                float laneTop = thickness + _LineReach + _LaneHeight;
                float lift = min(_PeelLift * peel * peel, 0.9 * laneTop);
                float lanes = (y - lift) / (1.0 - lift / laneTop);
                float laneScale = max(length(float2(ddx(lanes), ddy(lanes))) / max(px, 1e-6), 1e-3);

                // Everything below is worked out only where it can show: each layer, lane, current, ribbon and
                // ribbon part is skipped for a pixel outside a bound on everything it could colour (kept generous,
                // so nothing drawn is cut). The derivatives are all taken above, before any of these branches.

                // ---- The band: flat mint-green air with a deeper shade rippling along its inner edge, in the tile's
                // outline (inked in Wind's own ink). Only the band shows it: its alpha is 0 from half a pixel out.
                half4 band = 0.0;
                [branch] if (f.sdf < px)
                {
                    // The deep shade's wavy top edge, worked out only near it: a pixel well clear of the wave is
                    // plain air above it or plain shade below it.
                    float shadeMid = thickness * _ShadeHeight;
                    float deep = y - shadeMid;
                    float waveSlope = abs(_ShadeWave) * WIND_TAU / max(_ShadeWavelength, 1e-3) * (1.0 + abs(_WaveLean));
                    [branch] if (abs(deep) < abs(_ShadeWave) + px * (1.0 + waveSlope))
                    {
                        float2 wave = LeanWave(a - Travel(clock, _GustSpeed * _ShadeSpeed, _ShadeWavelength),
                                               _ShadeWavelength, 0.0);
                        float shadeTop = shadeMid + _ShadeWave * wave.x;
                        deep = (y - shadeTop) * rsqrt(1.0 + _ShadeWave * _ShadeWave * wave.y * wave.y);
                    }
                    half3 bandAir = lerp(_BandColor.rgb, 1.0, _SurgeFlash * clock.surge);   // flashing paler in a surge
                    half3 bandRgb = lerp(bandAir, _DeepColor.rgb, EFX_FillPx(deep, px));

                    // Lighter currents streaming through it: round-headed, tapering behind, no ink. A current is no
                    // thicker than its head, and the deep shade hides it, so it's only worked out near its own lane
                    // and above the shade.
                    float current = 1e5;
                    float currentHalf = 0.5 * _CurrentWidth * pop;
                    [unroll] for (int c = 0; c < 2; c++)
                    {
                        float laneY = thickness * (c == 0 ? _CurrentHeights.x : _CurrentHeights.y);
                        [branch] if (abs(y - laneY) < currentHalf + px && deep > -0.5 * px)
                        {
                            float slotLength = max(_CurrentSpacing, _CurrentLength + 0.5);
                            float along = a - Travel(clock, _GustSpeed * _CurrentSpeed * (1.0 + 0.25 * c), slotLength * 16.0)
                                          + c * 0.5 * slotLength;
                            float slot = floor(along / slotLength);
                            float key = slot - 16.0 * floor(slot / 16.0);
                            float2 seed = EFX_Hash22(float2(key + 3.1 * c, 7.7));
                            float len = _CurrentLength * lerp(0.7, 1.0, seed.x);
                            float headX = 0.25 + len + (slotLength - 0.5 - len) * seed.y;
                            float2 local = float2(y - laneY, headX - (along - slot * slotLength));
                            current = min(current, SdTaper(local, currentHalf, 0.0, len));
                        }
                    }
                    half3 currentRgb = lerp(_CurrentColor.rgb, 1.0, _SurgeFlash * clock.surge);
                    bandRgb = lerp(bandRgb, currentRgb, EFX_FillPx(current, px) * EFX_FillPx(-deep, px)) * _Emission;
                    bandRgb = lerp(bandRgb, _InkColor.rgb, 1.0 - EFX_FillPx(f.sdf + ink, px));
                    band = half4(bandRgb, inBand * ArcTakeover());
                }

                // ---- Speed lines, in lanes that follow the arc, one line per slot, each living its own short life.
                float lineEnd = saturate((halfLength - 0.2 - abs(a)) / max(_EndFade, 0.01));
                float lane0 = floor((lanes - _LineLowest) / _LaneHeight - 0.35);
                float2 best = 1e5;   // the lines (x) and their shadows (y)
                float inkWidth = 0.0;
                float lineInk = _LineInkWidth * shrink;
                float shadowDrop = _LineShadowDrop * shrink;
                // The farthest a line (its shadow and ink included) can colour from the stroke down its middle, and
                // how much farther down its tail can trail below its head as the line slopes up toward it, world
                // units. A lane whose line can't reach the pixel is skipped.
                float lineReach = 0.5 * _LineWidth * (1.0 + _Boil) * pop + lineInk + shadowDrop + px;
                float lineTrail = _LineLift * (_LineLengthMin + _LineLengthSpread) * (1.0 + clock.surge * _SurgeStretch);
                [unroll] for (int l = 0; l < 2; l++)
                {
                    float lane = lane0 + l;
                    float laneY = _LineLowest + lane * _LaneHeight;
                    float fromLane = lanes - laneY;
                    [branch] if (lineEnd > 0.0 && lane >= 0.0 && laneY <= thickness + _LineReach + 0.01
                                 && fromLane < _LaneWobble + laneScale * lineReach
                                 && fromLane > -(_LaneWobble + laneScale * (lineReach + lineTrail)))
                    {
                        float laneSeed = EFX_Hash21(float2(lane, 1.7));
                        float height = saturate((laneY - thickness) / max(_LineReach, 0.01));
                        float slotLength = _LineSpacing * (1.0 + _LineThinning * height);
                        float speed = _GustSpeed * (_LaneSpeedMin + _LaneSpeedSpread * laneSeed);
                        float along = a - Travel(clock, speed, slotLength * 16.0) + frac(lane * 0.618) * slotLength;
                        float slot = floor(along / slotLength);
                        float key = slot - 16.0 * floor(slot / 16.0);
                        float x = along - slot * slotLength;   // within the slot

                        float2 life = GustCycle(clock, _LineLife, EFX_Hash21(float2(key, lane + 2.9)));
                        float2 seedA = EFX_Hash22(float2(key + life.y * 0.71, lane + 0.37));

                        // The line shoots out from a point (its head racing ahead), then its tail catches up.
                        float gap = 0.6;
                        float len = min((_LineLengthMin + _LineLengthSpread * seedA.x) * (1.0 + clock.surge * _SurgeStretch),
                                        slotLength - gap);
                        float start = 0.5 * gap + (slotLength - gap - len) * seedA.y;
                        float headX = start + len * EaseOut3(life.x / 0.5);
                        float tailX = start + len * EaseIn2((life.x - 0.22) / 0.78);
                        float shown = max(headX - tailX, 1e-3);

                        // Lengthwise first: sloping, the line spans no more than it's shown along the lane.
                        [branch] if (life.x < 0.999 && x < headX + lineReach && x > headX - shown - lineReach)
                        {
                            float2 seedB = EFX_Hash22(float2(lane - 7.3, key + life.y * 1.31));
                            float boil = EFX_Hash21(float2(key + boilTick * 0.137, lane + 3.3));
                            float halfWidth = 0.5 * _LineWidth * (1.0 - _LineWidthSpread * seedB.x)
                                              * (1.0 + _Boil * (boil - 0.5) * 2.0) * pop;
                            halfWidth = min(halfWidth, 0.45 * shown);
                            float wobble = (seedB.y - 0.5) * 2.0 * _LaneWobble;
                            float slope = _LineLift * frac(seedB.y * 5.37);

                            float2 q = float2(x - headX, (lanes - laneY - wobble) / laneScale);
                            float2 dir = float2(1.0, slope) * rsqrt(1.0 + slope * slope);
                            float2 local = float2(q.x * dir.y - q.y * dir.x, -(q.x * dir.x + q.y * dir.y));   // across, back

                            // Measured only within its reach of the stroke from its head back to its tail.
                            float lineNear = halfWidth + lineInk + shadowDrop + px;
                            [branch] if (abs(local.x) < lineNear && local.y > -lineNear && local.y < shown + lineNear)
                            {
                                float lineSdf = SdTaper(local, halfWidth, 0.0, shown);
                                // The shadow stops short of the tail, so the two never split into a forked tail.
                                float shadowSdf = SdTaper(local - shadowDrop * dir, halfWidth, 0.0, 0.7 * shown);
                                if (min(lineSdf, shadowSdf) < min(best.x, best.y))
                                {
                                    // The ink thins toward the tail too, so the tail fades to a hair rather than a
                                    // dark thread.
                                    inkWidth = lineInk * sqrt(saturate(2.5 * (1.0 - local.y / shown)));
                                }
                                best = min(best, float2(lineSdf, shadowSdf));
                            }
                        }
                    }
                }

                // White line over its flat shadow, one ink outline round both (only where there's a line at all).
                half4 gust = band;
                [branch] if (min(best.x, best.y) - inkWidth < 0.5 * px)
                {
                    half lineFill = EFX_FillPx(best.x, px);
                    half shadowFill = EFX_FillPx(best.y, px);
                    half inkFill = EFX_FillPx(min(best.x, best.y) - inkWidth, px);
                    half3 lineRgb = lerp(_LineShadowColor.rgb * _Emission, _LineColor.rgb * _LineGlow, lineFill);
                    lineRgb = lerp(_InkColor.rgb, lineRgb, saturate(max(lineFill, shadowFill) / max(inkFill, 1e-4)));
                    gust = EFX_Over(half4(lineRgb, inkFill * lineEnd * fade), band);
                }

                // ---- Gust ribbons, one per cell along the arc, drawn over the lines. Positions are measured at a
                // fixed radius (the crown's base) so a ribbon keeps its shape at every height, and each is drawn in a
                // frame true to world units around its head. A ribbon reaches back over up to two cells behind its
                // own, so a pixel checks its own cell's ribbon and the next two's, back to front (the upstream one
                // in front).
                float radius = length(input.positionOS);
                float refRadius = _ArcShape.z + 1.0;
                float aRef = a / max(radius, 1e-3) * refRadius;
                float halfRef = 0.5 * ArcSweep() * refRadius;
                float toRef = refRadius / max(radius, 1e-3);   // world units here -> at the reference radius
                // Where no ribbon can reach, none is set up: below the lowest root (or a downward curl's lowest dip,
                // when its rise is cut short by the steepest rise), above the highest curl, and past the downstream
                // end (a ribbon has shrunk away before its curl gets there). `ribbonMargin` is the most a ribbon
                // colours past its middle line: its thickness (with the pop's and the whoosh's overshoots and the
                // boil), ink and a pixel.
                float ribbonMargin = 0.5 * _RibbonWidth * 1.11 * pop * (1.0 + _Boil) + ink + px;
                float lowestRise = min(0.0, _RibbonSteepness - _RibbonRoot);
                float ribbonLow = min(-_RibbonRoot, lowestRise - 0.05);
                ribbonLow = _CurlDownChance > 0.0 ? min(ribbonLow, lowestRise - 2.0 * _CurlSize) : ribbonLow;
                float ribbonHigh = max(_RibbonReach - 0.5 * _RibbonWidth - ink, 2.0 * _CurlSize);
                [branch] if (y > thickness + ribbonLow - ribbonMargin && y < thickness + ribbonHigh + ribbonMargin
                             && aRef < halfRef - 0.1 + _CurlSize * max(toRef - 1.0, 0.0) + ribbonMargin * toRef)
                {
                    float spacing = _RibbonSpacing;
                    float ribbonTravel = Travel(clock, _GustSpeed * _RibbonSpeed, spacing * 16.0);
                    float alongR = aRef - ribbonTravel;
                    float cell = floor(alongR / spacing);
                    // Thinner inside the band; and thinning with the shapes as the arc winds down.
                    half inkHere = ink * lerp(_RibbonBandInk, 1.0, saturate(f.sdf / 0.3 + 1.0)) * shrink;
                    // For each ribbon's first look below (reference-radius units, less the drift): how far past its
                    // head its curl can reach this pixel, and the span its head must lie in for it not to have
                    // shrunk away at an end, whatever size its curl turns out.
                    float curlLeast = _CurlSize * (1.0 - _CurlSizeSpread);
                    float aheadReach = (_CurlSize + ribbonMargin) * toRef;
                    float headLast = halfRef - 0.1 - curlLeast + 0.01 - ribbonTravel;
                    float headFirst = 2.2 - halfRef - 0.01 - ribbonTravel;

                    [unroll] for (int i = 2; i >= 0; i--)
                    {
                        float id = cell + i;
                        float key = id - 16.0 * floor(id / 16.0);
                        // Neighbouring cells are a golden-ratio share of a life apart, so lives stay staggered (never
                        // all ending together).
                        float2 life = GustCycle(clock, _RibbonLife, frac(key * 0.618034));
                        float2 seedA = EFX_Hash22(float2(key + 0.37, life.y));

                        // The head (and curl) stay in the ribbon's own cell, further back the bigger its curl; the
                        // root reaches back up to two cells (a root sits lower, where the same angle spans fewer world
                        // units, hence the 1.2). A ribbon whose root would reach back past the upstream end is
                        // shorter, rooted at the end, so near there the gust pulls out of the end as it drifts on.
                        float headShare = 0.15 + 0.85 * seedA.y;
                        float lenLong = (_RibbonLength + _RibbonLengthSpread * seedA.x) * (1.0 + _RibbonStretch * clock.surge);

                        // A first look, before the rest of its dice are rolled: where along the arc it can lie at all,
                        // whatever size its curl turns out (the bigger the curl, the further back its head), and
                        // whether it's anywhere but shrunk away at an end.
                        float headLo = id * spacing + headShare * max(spacing - 1.2 * _CurlSize, 0.0);
                        float headInHi = headShare * max(spacing - 1.2 * curlLeast, 0.0);
                        float headHi = id * spacing + headInHi;
                        float lenHi = max(min(min(lenLong, (2.0 * spacing + headInHi) / 1.2), headHi + ribbonTravel + halfRef - 0.5), 1.0);
                        [branch] if (alongR - headLo > -(lenHi + ribbonMargin) * toRef && alongR - headHi < aheadReach
                                     && headLo < headLast && headHi > headFirst)
                        {
                            // Its life: whoosh out (the head races along it, winding the curl), ride, then the tail
                            // chases the head into the curl, then a moment with nothing before the next.
                            float p = life.x;
                            float2 drawn = float2(2.0 * EaseIn2((p - _RibbonFade) / max(1.0 - _RibbonRest - _RibbonFade, 0.01)),
                                                  2.0 * EaseOut3(p / _RibbonGrow));
                            drawn.x = min(drawn.x, drawn.y);
                            float2 seedB = EFX_Hash22(float2(life.y + 11.3, key));
                            float2 seedC = EFX_Hash22(float2(key * 1.7 + life.y, 9.1));

                            Ribbon r;
                            r.curlRadius = _CurlSize * (1.0 - _CurlSizeSpread * seedB.x);
                            r.curlDir = seedB.y < _CurlDownChance ? -1.0 : 1.0;
                            r.halfWidth = 0.5 * _RibbonWidth;
                            r.drawn = drawn;
                            float headIn = headShare * max(spacing - 1.2 * r.curlRadius, 0.0);
                            float head = id * spacing + headIn;
                            float headAt = head + ribbonTravel;   // along the arc, at the reference radius
                            r.len = min(lenLong, (2.0 * spacing + headIn) / 1.2);
                            r.len = max(min(r.len, headAt + halfRef - 0.5), 1.0);

                            // Die down toward the arc's ends: near the downstream end (or with its head right at the
                            // upstream end) the whole ribbon shrinks back into the band.
                            float room = min(halfRef - 0.1 - (headAt + r.curlRadius), headAt + halfRef - 2.2);
                            float presence = saturate(room / max(_EndFade, 0.01));

                            // Heads sit at different heights, low enough that the curl stays within the reach and the
                            // S never gets steeper than the steepest rise; a downward curl sits higher so it doesn't
                            // dive into the band.
                            float rise = (_RibbonRise + _RibbonRiseSpread * seedC.x + (r.curlDir > 0.0 ? 0.0 : 1.5 * r.curlRadius));
                            float top = _RibbonReach - (r.curlDir > 0.0 ? 2.0 * r.curlRadius : 0.0) - r.halfWidth - ink;
                            rise = min(min(rise, max(top, 0.0)), _RibbonSteepness * r.len - _RibbonRoot) * presence;
                            r.drop = max(rise + _RibbonRoot, 0.05);
                            r.curlRadius *= lerp(0.5, 1.0, presence);
                            float sized = EaseOutBack(p / (0.6 * _RibbonGrow)) * lerp(0.4, 1.0, presence) * pop;

                            float2 q = float2((alongR - head) * radius / refRadius, y - (thickness + rise));

                            // Skip the maths for pixels nowhere near this ribbon (its thickness taken at the most its
                            // boil can make it).
                            float margin = r.halfWidth * sized * (1.0 + _Boil) + ink + px;
                            float curlLow = r.curlDir > 0.0 ? 0.0 : -2.0 * r.curlRadius;
                            bool nearby = q.x > -r.len - margin && q.x < r.curlRadius + margin
                                        && q.y > min(-r.drop, curlLow) - margin && q.y < curlLow + 2.0 * r.curlRadius + margin;
                            [branch] if (nearby && drawn.x < 1.999 && presence > 0.0)
                            {
                                float boil = EFX_Hash21(float2(key + boilTick * 0.137, 4.4));

                                // The curl winds its turns, tightening, but never so tight its windings touch.
                                r.turns = max(_CurlTurns, 0.1) * WIND_TAU;
                                r.shrink = max(r.curlRadius * (1.0 - _CurlTip) / r.turns,
                                               (r.halfWidth * (1.0 + _CurlTipWidth) + 2.0 * ink + 0.15) / WIND_TAU);
                                r.turns = min(r.turns, (r.curlRadius - 0.1) / r.shrink);
                                r.halfWidth *= sized * (1.0 + _Boil * (boil - 0.5) * 2.0);

                                float3 shape = GustRibbon(q, r, r.halfWidth + inkHere + px);
                                float fillSdf = shape.x - shape.y;
                                [branch] if (fillSdf - inkHere < 0.5 * px)   // anything to colour at all
                                {
                                    half cover = EFX_FillPx(fillSdf - inkHere, px);
                                    half fill = EFX_FillPx(fillSdf, px);
                                    half shine = EFX_FillPx(shape.y * (1.0 - 2.0 * _RibbonShine) - shape.z, px);
                                    half shade = EFX_FillPx(shape.z + shape.y * (1.0 - 2.0 * _RibbonShade), px);
                                    half3 air = lerp(_AirColor.rgb, 1.0, _SurgeFlash * clock.surge);   // flashing paler in a surge
                                    half3 rgb = lerp(lerp(air, _ShadeColor.rgb, shade), _LineColor.rgb, shine) * _RibbonGlow;
                                    rgb = lerp(_InkColor.rgb, rgb, saturate(fill / max(cover, 1e-4)));
                                    gust = EFX_Over(half4(rgb, cover * fade), gust);
                                }
                            }
                        }
                    }
                }

                return EFX_Over(gust, tile);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
