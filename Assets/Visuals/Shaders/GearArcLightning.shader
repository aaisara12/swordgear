Shader "Swordgear/Gear Arc Lightning"
{
    // Lightning's gear section. Idle or aimed at, it's the shared cartoon tile in Lightning's colour. Active,
    // the tile turns into a cartoon storm cloud that keeps throwing yellow bolts:
    //  - The cloud: one inked mass in flat slate-periwinkle tones. A crown of round puffs bulges past the gear's
    //    outer edge (its outline is all scallops meeting in sharp V cusps), each crown puff with a flat-cut lit
    //    cap. Across the middle sit a few wide, flat billows inked only along their tops, and along the inner
    //    edge big gentle scallops carry a deep shadow band. Each end rounds off in one big puff. The puffs boil
    //    in place like a hand-drawn cloud: redrawn on twos, each redraw nudging and resizing every puff a
    //    little, with a slow swell underneath.
    //  - Crackles: short yellow zig-zags flicker on and off in quick bursts, slanted across the seams between
    //    the billows.
    //  - Strikes: two strike channels, so a bolt is out most of the time. Each strike is a bold, chunky cartoon
    //    bolt in the lightning-sign shape — a lean out of the cloud, a sharp jog back, a long lean to a point
    //    (sometimes two steps of it, or a thin fork splitting off the jog) — yellow with a white-hot core in
    //    dark ink, tilted a little at random and thrown from behind the crown out past the gear. Just before it
    //    lands the crown puffs over its root draw in; as it lands they pop out and settle with a wobble, and
    //    their caps light a warm pale yellow for a frame. It lands with a fat impact star at its tip, holds, blinks
    //    for a frame, comes back as a second drawing and is gone. A just-granted arc strikes from its middle at
    //    once, on the first channel (that channel's own strike of the beat is dropped); the other channel keeps
    //    clear of it like any strike.
    //  - Coming alive, the crown bursts out of the tile first and the rest of the cloud follows the takeover;
    //    dying down, the crown shrinks back into the tile, its boil and pops with it, and fades with the bolts.
    // Every edge is an analytic distance (circles, ellipses, straight segments meeting in sharp mitred corners),
    // so it anti-aliases cleanly at any zoom. The arena's tonemapping renders mid tones noticeably darker and
    // more saturated than the swatches, so the cloud colours are picked lighter and greyer than they end up.
    //
    // Cost: the strike schedule is the same for the whole arc, so it's worked out per vertex (LightningVertex
    // wraps the shared ArcVertex) and handed over unblended, not redone per pixel. Per pixel, each row of puffs
    // and the crackle are only built where they can reach.
    //
    // The loose pieces (sparks popping round the crown, mini bolts flicking outward) are particles, not this
    // shader: ArcBitsLightning.prefab in Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by
    // GearArcArt.
    Properties
    {
        [Header(Cloud Colours)]
        _CloudDeep ("Underside Shadow", Color) = (0.282, 0.29, 0.392, 1)
        _CloudMid ("Billows (the cloud's middle)", Color) = (0.376, 0.388, 0.537, 1)
        _CloudLight ("Crown", Color) = (0.525, 0.541, 0.722, 1)
        _CloudTop ("Crown's Lit Caps", Color) = (0.71, 0.725, 0.859, 1)
        _FlashColor ("Strike-Lit Caps (the crown caps round a strike's root, for a frame)", Color) = (0.851, 0.808, 0.62, 1)
        _InkColor ("Ink", Color) = (0.078, 0.071, 0.18, 1)
        _CloudInkWidth ("Outline Ink Width (world units)", Range(0, 0.3)) = 0.09
        _PuffInkWidth ("Ink Strokes Inside The Cloud (world units)", Range(0, 0.2)) = 0.06
        _CrownInkStart ("Crown Puff Ink Starts (share of its height above its centre; 0 = half round, 0.7 = just the top)", Range(-1, 1)) = 0.1
        _InkArcStart ("Billow Ink Starts (share of its height above its centre; 0 = half round, 0.7 = just the top)", Range(-1, 1)) = 0.5
        _LitCap ("Crown Cap Depth (share of a puff's height)", Range(0, 1)) = 0.3
        _LitCapCut ("Crown Cap Cut Off Flat At (share of a puff's height above its centre)", Range(-1, 1)) = 0.15

        [Header(Cloud Crown)]
        _CrownSpacing ("Crown Puff Spacing (world units)", Range(0.4, 3)) = 1.3
        _CrownSize ("Crown Puff Height (world units, centre to top)", Range(0.2, 1.4)) = 0.7
        _CrownSizeRandom ("Crown Puff Random Extra Height (world units)", Range(0, 0.6)) = 0.25
        _CrownWidth ("Crown Puff Width (times its height)", Range(0.6, 2)) = 1.15
        _CrownDrop ("Crown Puff Centres (world units in from the gear's outer edge)", Range(-0.5, 1)) = 0.15

        [Header(Cloud Billows)]
        _LumpSpacing ("Billow Spacing (world units)", Range(0.4, 4)) = 2.2
        _LumpSize ("Billow Height (world units, centre to top)", Range(0.2, 1.5)) = 0.8
        _LumpSizeRandom ("Billow Random Extra Height (world units)", Range(0, 0.6)) = 0.2
        _LumpWidth ("Billow Width (times its height)", Range(0.6, 2.5)) = 1.4
        _LumpHeight ("Billow Centres (world units out from the gear's inner edge)", Range(0, 3)) = 0.95

        [Header(Cloud Underside And Ends)]
        _UnderSpacing ("Inner Scallop Spacing (world units)", Range(0.3, 3)) = 2.1
        _UnderSize ("Inner Scallop Radius (world units)", Range(0.1, 2)) = 1.4
        _UnderRaise ("Inner Scallop Centres (world units out from the gear's inner edge)", Range(0, 2)) = 1.2
        _ShadowDepth ("Underside Shadow Depth (world units)", Range(0, 1.5)) = 0.42
        _EndPuffSize ("End Puff Radius (world units)", Range(0.5, 2)) = 1.45
        _EndPuffBulge ("End Puff Bulge Past The Band's End (world units, up to 0.6)", Range(0, 0.6)) = 0.35
        _CloudEndTaper ("Puffs Shrink Toward The Arc's Ends Over (world units)", Range(0.1, 3)) = 1.2
        _PuffMinSize ("Drop A Shrinking Puff Below (share of its full size)", Range(0, 1)) = 0.55

        [Header(Boil)]
        _BoilRate ("Boil Redraws Per Second (12 = on twos, 8 = on threes)", Range(1, 24)) = 12
        _BoilWobble ("Boil Nudge Per Redraw (world units)", Range(0, 0.2)) = 0.1
        _BoilJitter ("Boil Size Change Per Redraw (world units, either way)", Range(0, 0.2)) = 0.08
        _Swell ("Puff Swell (world units)", Range(0, 0.4)) = 0.1
        _SwellSpeed ("Puff Swell Speed (radians per second, 6.28 = once a second)", Range(0, 10)) = 2.2

        [Header(Strike Throw)]
        _ThrowPop ("Crown Pops Out As A Bolt Lands (world units)", Range(0, 0.5)) = 0.2
        _ThrowRadius ("Pop Reaches Along The Arc (world units from the bolt's root)", Range(0, 3)) = 0.9
        _ThrowSettle ("Pop Settles Over (s, with one wobble)", Range(0.02, 0.5)) = 0.18
        _Inhale ("Crown Draws In Before A Strike (world units)", Range(0, 0.3)) = 0.08
        _InhaleTime ("Draws In For (s before the bolt lands)", Range(0, 0.3)) = 0.1

        [Header(Crackles)]
        _CrackleColor ("Crackle", Color) = (1, 0.8, 0.16, 1)
        _CrackleGlow ("Crackle Brightness (HDR)", Range(1, 4)) = 1.2
        _CrackleHeight ("Crackle Height (world units out from the gear's inner edge)", Range(0, 3)) = 1.55
        _CrackleHeightRandom ("Crackle Random Height (world units, either way)", Range(0, 1)) = 0.15
        _CrackleLength ("Crackle Length (world units)", Range(0.2, 2)) = 1.3
        _CrackleTilt ("Crackle Slant (degrees from along the arc)", Range(0, 90)) = 45
        _CrackleTiltRandom ("Crackle Random Slant (degrees, either way)", Range(0, 45)) = 12
        _CrackleSwing ("Crackle Zig-Zag Swing (world units, either way)", Range(0, 0.5)) = 0.17
        _CrackleWidth ("Crackle Half-Width (world units)", Range(0.01, 0.2)) = 0.075
        _CrackleInkWidth ("Crackle Ink Width (world units)", Range(0, 0.15)) = 0.05
        _CrackleFlicker ("Crackle Redraws Per Second", Range(4, 30)) = 12
        _CrackleBurstEvery ("Average Time Between A Seam's Bursts (s)", Range(0.2, 5)) = 0.6
        _CrackleBurstLength ("Burst Length (s)", Range(0.05, 1)) = 0.32
        _CrackleOnChance ("Lit During A Burst (0-1 chance per redraw)", Range(0, 1)) = 0.7
        _CrackleEndMargin ("No Crackles This Close To The Arc's Ends (world units)", Range(0, 3)) = 1.0

        [Header(Bolts)]
        _BoltColor ("Bolt", Color) = (1, 0.8, 0.16, 1)
        _CoreColor ("Bolt Core (and the crackles' and stars' centres)", Color) = (1, 1, 1, 1)
        _BoltGlow ("Bolt Brightness (HDR)", Range(1, 4)) = 1.1
        _CoreGlow ("Core Brightness (HDR)", Range(1, 5)) = 1.2
        _BoltWidth ("Bolt Half-Width At Its Root (world units)", Range(0.05, 0.5)) = 0.27
        _BoltInkWidth ("Bolt Ink Width (world units)", Range(0, 0.25)) = 0.1
        _BoltCoreInset ("White-Hot Core Inset From The Bolt's Edge (world units)", Range(0, 0.4)) = 0.19
        _BoltReachMin ("Shortest Strike Past The Gear (world units)", Range(0.5, 3.3)) = 2.55
        _BoltReachMax ("Longest Strike Past The Gear (world units; past about 2.85 the impact star clips)", Range(0.5, 3.3)) = 2.85
        _BoltRoot ("Bolt Root, Hidden In The Cloud (world units out from the gear's inner edge)", Range(0, 3)) = 2.2
        _BoltLean ("Zig-Zag Lean (world units sideways per long stroke)", Range(0, 1.2)) = 0.5
        _BoltJog ("Zig-Zag Jog Back (world units sideways per short stroke)", Range(0, 1.2)) = 0.6
        _TwoStepChance ("Two-Step Bolts (0-1 chance per strike)", Range(0, 1)) = 0.3
        _BoltTilt ("Random Tilt (degrees either way; leans inward near the arc's ends)", Range(0, 40)) = 12
        _BoltEndMargin ("Keep Strikes This Far From The Arc's Ends (world units)", Range(0, 3)) = 1.3
        _ForkChance ("Fork (0-1 chance per single-step bolt)", Range(0, 1)) = 0.25
        _ForkLength ("Fork Length (world units)", Range(0.2, 1.5)) = 0.7

        [Header(Strike Timing)]
        _StrikeRate ("Strikes Per Second, Per Channel (there are two)", Range(0.5, 6)) = 2.0
        _StrikeHold ("Strike Hold (s)", Range(0.05, 0.45)) = 0.28
        _BlinkAt ("Strike Blinks Off At (s after it lands; a second drawing follows)", Range(0, 0.3)) = 0.11
        _BlinkLength ("Blink Length (s)", Range(0, 0.1)) = 0.035
        _StrikeSeparation ("Two Strikes At Once Stay This Far Apart (world units)", Range(0, 4)) = 2.0
        _FlashTime ("Strike-Lit Caps Show For (s)", Range(0, 0.2)) = 0.05
        _FlashRadius ("Strike-Lit Caps Reach Along The Arc (world units from the root)", Range(0, 4)) = 1.1

        [Header(Impact Star)]
        _ImpactColor ("Impact Star", Color) = (1, 0.8, 0.16, 1)
        _ImpactGlow ("Impact Star Brightness (HDR)", Range(1, 4)) = 1.1
        _ImpactSize ("Impact Star Radius (world units)", Range(0, 1)) = 0.52
        _ImpactPop ("Impact Star First-Frame Pop (times its radius)", Range(1, 2)) = 1.15
        _ImpactPoints ("Impact Star Points", Range(4, 12)) = 6
        _ImpactValley ("Impact Star Valleys (share of its radius; higher = fatter points)", Range(0.2, 0.8)) = 0.55
        _ImpactTime ("Impact Star Shows For (s)", Range(0, 0.3)) = 0.1
        _ImpactInkWidth ("Impact Star Ink Width (world units)", Range(0, 0.15)) = 0.07

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
            #pragma vertex LightningVertex
            #pragma fragment LightningFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half4 _CloudDeep; half4 _CloudMid; half4 _CloudLight; half4 _CloudTop; half4 _FlashColor; half4 _InkColor; \
                float _CloudInkWidth; float _PuffInkWidth; float _CrownInkStart; float _InkArcStart; float _LitCap; float _LitCapCut; \
                float _CrownSpacing; float _CrownSize; float _CrownSizeRandom; float _CrownWidth; float _CrownDrop; \
                float _LumpSpacing; float _LumpSize; float _LumpSizeRandom; float _LumpWidth; float _LumpHeight; \
                float _UnderSpacing; float _UnderSize; float _UnderRaise; float _ShadowDepth; float _EndPuffSize; float _EndPuffBulge; \
                float _CloudEndTaper; float _PuffMinSize; \
                float _BoilRate; float _BoilWobble; float _BoilJitter; float _Swell; float _SwellSpeed; \
                float _ThrowPop; float _ThrowRadius; float _ThrowSettle; float _Inhale; float _InhaleTime; \
                half4 _CrackleColor; half _CrackleGlow; float _CrackleHeight; float _CrackleHeightRandom; float _CrackleLength; \
                float _CrackleTilt; float _CrackleTiltRandom; float _CrackleSwing; float _CrackleWidth; float _CrackleInkWidth; \
                float _CrackleFlicker; float _CrackleBurstEvery; float _CrackleBurstLength; half _CrackleOnChance; float _CrackleEndMargin; \
                half4 _BoltColor; half4 _CoreColor; half _BoltGlow; half _CoreGlow; \
                float _BoltWidth; float _BoltInkWidth; float _BoltCoreInset; float _BoltReachMin; float _BoltReachMax; \
                float _BoltRoot; float _BoltLean; float _BoltJog; half _TwoStepChance; float _BoltTilt; float _BoltEndMargin; \
                half _ForkChance; float _ForkLength; \
                float _StrikeRate; float _StrikeHold; float _BlinkAt; float _BlinkLength; float _StrikeSeparation; \
                float _FlashTime; float _FlashRadius; \
                half4 _ImpactColor; half _ImpactGlow; float _ImpactSize; float _ImpactPop; float _ImpactPoints; float _ImpactValley; \
                float _ImpactTime; float _ImpactInkWidth;
            #include "GearArcCommon.hlsl"

            #define LIGHTNING_TAU 6.2831853
            #define LIGHTNING_DEGREE 0.01745329

            // Per-pixel scratch shared by the helpers below (set once in the fragment; the same for every pixel).
            static float gBoilTime;   // the boil clock: time, stepped to the boil's redraws
            static float gBoilTick;   // which redraw it is
            static float gAgeA;       // the two strike channels' ages (s): negative while a strike is still coming
            static float gAgeB;
            static float gPhiA;       // where they root, radians from the arc's centre
            static float gPhiB;
            static float gGrant;      // 1 while a just-granted arc's first strike lights every cap

            // The arc's varyings plus its strike schedule, worked out once per vertex (every vertex gets the same
            // answer, and it isn't blended) instead of once per pixel.
            struct LightningVaryings
            {
                ArcVaryings arc;
                nointerpolation float4 strikeA : TEXCOORD4;   // channel A: age (s), root (radians), seed; w = gGrant
                nointerpolation float4 strikeB : TEXCOORD5;   // channel B: age, root, seed; w = the half-angle strikes keep within
            };

            // Folds a count that grows with time into a small range before it's hashed: hashes of big numbers run
            // out of float precision and repeat (in the editor the shader clock is the time since it started).
            float Fold(float n, float span)
            {
                return n - span * floor(n / span);
            }

            // Signed distance to an ellipse of radii `rad` centred on the origin (an approximation that is
            // close to exact near the edge, which is all the anti-aliasing and ink need).
            float SdEllipse(float2 p, float2 rad)
            {
                float k0 = length(p / rad);
                float k1 = max(length(p / (rad * rad)), 1e-5);
                return k0 * (k0 - 1.0) / k1;
            }

            // ---------- Strike throw ----------
            // How far a strike `age` s old, rooted at `rootPhi`, pushes out a puff centred at `phi` (on a row of
            // radius `rowRadius`): drawn in just before it lands, popped out as it lands, then settling back with
            // one small overshoot. On ones (24 a second), so it snaps.
            float ThrowFrom(float phi, float rootPhi, float age, float rowRadius)
            {
                float a = floor(age * 24.0) / 24.0;
                float landed = 2.0 / 24.0;
                float settle = (a - landed) / max(_ThrowSettle, 0.01);
                float pop = (a >= -_InhaleTime && a < 0.0) ? -_Inhale : 0.0;
                pop = (a >= 0.0 && a < landed) ? _ThrowPop : pop;
                pop = (a >= landed && settle < 1.0) ? _ThrowPop * exp(-3.0 * settle) * cos(settle * 4.712) : pop;
                float near = saturate(1.0 - (abs(phi - rootPhi) * rowRadius - _ThrowRadius) / 0.6);
                return pop * near;
            }

            // 1 while a crown puff centred at `phi` has its cap lit by a strike landing near it.
            float CapFlash(float phi, float rowRadius)
            {
                float a = (gAgeA >= 0.0 && gAgeA < _FlashTime && abs(phi - gPhiA) * rowRadius < _FlashRadius) ? 1.0 : 0.0;
                float b = (gAgeB >= 0.0 && gAgeB < _FlashTime && abs(phi - gPhiB) * rowRadius < _FlashRadius) ? 1.0 : 0.0;
                return max(max(a, b), gGrant);
            }

            // ---------- Puffs ----------
            // A puff is a round (or wide, flat) bump on the cloud. Only its upper arc is inked, and the cloud
            // under it is already its tone, so a row of them reads as one mass with a scalloped top edge rather
            // than a row of balls.
            struct Puff
            {
                float sd;      // signed distance to its outline, world units
                float cap;     // signed distance to it nudged down: inside the puff but outside this = its lit cap
                float capCut;  // signed distance to the flat line its lit cap stops at (negative above it)
                float upper;   // 1 on its upper arc, 0 from its middle down: only the upper arc is inked
                float phi;     // its centre, radians from the arc's centre
                float id;      // its slot in the row
            };

            // One puff of a row: slot `id` of a row spaced `spacing` along the arc at `rowRadius`, centred
            // `centreY` out from the gear's inner edge, `width` times as wide as it is tall. (phi, r, y) is the
            // pixel. It boils: every redraw nudges it and changes its size a little, and it swells slowly.
            // `boilShare` scales the boil and swell, `throwShare` how much the strikes' throw moves it.
            Puff MakePuff(float phi, float r, float y, float id, float spacing, float rowRadius, float centreY,
                          float size, float sizeRandom, float width, float inkStart, float seed, float boilShare,
                          float throwShare)
            {
                Puff o;
                float h = EFX_Hash21(float2(id, seed));
                float along = (id + 0.5 + (h - 0.5) * 0.3) * spacing;
                float2 wobble = (EFX_Hash22(float2(id + seed, gBoilTick)) - 0.5) * 2.0 * _BoilWobble * boilShare;
                float jitter = (EFX_Hash21(float2(id * 1.37 + seed, gBoilTick + 0.61)) - 0.5) * 2.0 * _BoilJitter * boilShare;
                o.phi = (along + wobble.x) / rowRadius;
                float swell = _Swell * boilShare * sin(Fold(gBoilTime * _SwellSpeed + h * LIGHTNING_TAU, LIGHTNING_TAU));
                float radius = size + sizeRandom * EFX_Hash21(float2(id, seed + 1.7)) + swell + jitter;
                radius += throwShare * (ThrowFrom(o.phi, gPhiA, gAgeA, rowRadius) + ThrowFrom(o.phi, gPhiB, gAgeB, rowRadius));
                // Shrink toward the arc's ends, so the cloud rounds off inside the mesh; a puff that would be a
                // little wart is dropped instead.
                float halfLength = 0.5 * ArcSweep() * rowRadius;
                float taper = saturate((halfLength - abs(along)) / _CloudEndTaper);
                bool dropped = taper < _PuffMinSize;
                radius = max(radius * taper, 1e-3);
                float2 rad = float2(radius * width, radius);
                float2 d = float2((phi - o.phi) * r, y - centreY - wobble.y);
                o.sd = dropped ? 1e4 : SdEllipse(d, rad);
                o.cap = SdEllipse(d + float2(0.0, _LitCap * radius), rad);
                o.capCut = _LitCapCut * radius - d.y;
                o.upper = saturate((d.y / radius - inkStart) * 3.0);
                o.id = id;
                return o;
            }

            // Paints one puff over `rgb`: its tone, its lit cap (where `capOn`), and ink along its upper arc
            // only, thinning away down its sides — a cartoon cloud's strokes.
            void PaintPuff(inout half3 rgb, Puff p, half3 bodyColor, half3 capColor, half capOn, float px)
            {
                half inside = EFX_FillPx(p.sd, px);
                half cap = (1.0 - EFX_FillPx(p.cap, px)) * EFX_FillPx(p.capCut, px) * capOn;
                half3 body = lerp(bodyColor, capColor, cap);
                half ink = inside - EFX_FillPx(p.sd + _PuffInkWidth * p.upper, px);
                rgb = lerp(rgb, lerp(body, _InkColor.rgb, ink), inside);
            }

            // Paints a row's three nearest puffs back to front: even slots sit behind odd ones, so wherever two
            // overlap, the front one's ink shows which is which.
            void PaintRow(inout half3 rgb, Puff a, Puff b, Puff c, half3 bodyColor, half3 capA, half3 capB, half3 capC,
                          half capOn, float px)
            {
                bool evenMiddle = frac(b.id * 0.5) < 0.25;
                [flatten] if (evenMiddle)
                {
                    PaintPuff(rgb, b, bodyColor, capB, capOn, px);
                    PaintPuff(rgb, a, bodyColor, capA, capOn, px);
                    PaintPuff(rgb, c, bodyColor, capC, capOn, px);
                }
                else
                {
                    PaintPuff(rgb, a, bodyColor, capA, capOn, px);
                    PaintPuff(rgb, c, bodyColor, capC, capOn, px);
                    PaintPuff(rgb, b, bodyColor, capB, capOn, px);
                }
            }

            // ---------- Zig-zags ----------
            // Signed distance (negative inside) to a cartoon zig-zag stroke: straight segments through
            // pts[0..count], meeting in sharp mitred corners, its half-width running straight from wid[i] at each
            // point to the next (0 = a sharp point). Each segment owns the pixels between the mitre lines at its
            // two corners, so the outline is sharp at every corner and continuous across it.
            float SdZigZag(float2 p, float2 pts[6], float wid[6], int count)
            {
                float d = 1e4;
                bool pastStart = true;
                [unroll] for (int i = 0; i < 5; i++)
                {
                    if (i < count)
                    {
                        float2 a = pts[i];
                        float2 ab = pts[i + 1] - a;
                        float len = max(length(ab), 1e-4);
                        float2 dir = ab / len;
                        bool pastEnd = false;
                        if (i + 1 < count)
                        {
                            float2 nextDir = normalize(pts[i + 2] - pts[i + 1]);
                            pastEnd = dot(p - pts[i + 1], dir + nextDir) > 0.0;
                        }
                        float2 q = p - a;
                        float along = dot(q, dir);
                        float across = abs(dot(q, float2(-dir.y, dir.x)));
                        float taper = (wid[i + 1] - wid[i]) / len;
                        float side = (across - wid[i] - taper * along) * rsqrt(1.0 + taper * taper);
                        // Round off past the stroke's two ends, so a pointed end's ink doesn't run on in a spike.
                        side = (i == 0 && along < 0.0) ? max(side, length(q) - wid[0]) : side;
                        side = (i + 1 == count && along > len) ? max(side, length(q - ab) - wid[i + 1]) : side;
                        // A guard far from the segment, where mitre lines of a tight zig-zag can cross.
                        side = max(side, length(q - ab * 0.5) - (len * 0.5 + 3.0 * max(wid[i], wid[i + 1]) + 0.3));
                        d = (pastStart && !pastEnd) ? min(d, side) : d;
                        pastStart = pastEnd;
                    }
                }
                return d;
            }

            // A fat cartoon impact star of `points` points, outer radius `radius`, each point its own length.
            float SdImpact(float2 p, float radius, float points, float seed)
            {
                float sector = LIGHTNING_TAU / points;
                float angle = atan2(p.x, p.y);
                float slot = floor(angle / sector);
                float within = angle - sector * slot - 0.5 * sector;
                float tipRadius = radius * (0.8 + 0.2 * EFX_Hash21(float2(slot, seed)));
                float2 q = length(p) * float2(cos(within), abs(sin(within)));
                float2 tip = float2(tipRadius, 0.0);
                float2 valley = _ImpactValley * radius * float2(cos(0.5 * sector), sin(0.5 * sector));
                float2 edge = valley - tip;
                float2 w = q - tip;
                float dist = length(w - edge * saturate(dot(w, edge) / dot(edge, edge)));
                return (edge.x * w.y - edge.y * w.x) > 0.0 ? -dist : dist;
            }

            // One bolt, in its own frame (x across, y out from its root, which hides in the cloud). `side` is the
            // way it leans, `exitY` where it comes out of the cloud, `reach` where its point ends. Returns its
            // signed distance and, through `tip`, where its point is.
            float SdBolt(float2 q, float seed, float side, float exitY, float reach, out float2 tip)
            {
                float4 h = float4(EFX_Hash21(float2(seed, 2.1)), EFX_Hash21(float2(seed, 2.7)),
                                  EFX_Hash21(float2(seed, 3.3)), EFX_Hash21(float2(seed, 3.9)));
                float lean1 = _BoltLean * (0.8 + 0.4 * h.x);
                float jog1 = _BoltJog * (0.8 + 0.4 * h.y);
                float lean2 = _BoltLean * (0.85 + 0.4 * h.z);
                float jog2 = _BoltJog * (0.7 + 0.3 * h.w);
                float shown = max(reach - exitY, 0.1);
                float w = _BoltWidth;

                // The sign shape: a lean out of the cloud, a short jog back, and a long lean to the point. Some
                // strikes take two steps of it on the way out. The first stroke starts just under the crown (its
                // root is hidden anyway), so it leans like the last one and the two read as parallel strokes.
                float2 pts[6];
                float wid[6];
                bool twoStep = EFX_Hash21(float2(seed, 5.3)) < _TwoStepChance;
                float rise1 = exitY + shown * (twoStep ? 0.26 : 0.4) * (0.9 + 0.2 * h.y);
                float jogRise = shown * (twoStep ? 0.07 : 0.09);
                pts[0] = float2(-side * lean1 * 0.5, exitY - 0.5);
                pts[1] = float2(side * lean1 * 0.5, rise1);
                pts[2] = float2(pts[1].x - side * jog1, rise1 + jogRise);
                wid[0] = w;
                wid[1] = w * 0.94;
                wid[2] = w * 0.86;
                float d;
                [branch] if (twoStep)
                {
                    float rise3 = exitY + shown * 0.6;
                    pts[3] = float2(pts[2].x + side * lean2 * 0.8, rise3);
                    pts[4] = float2(pts[3].x - side * jog2 * 0.8, rise3 + jogRise);
                    pts[5] = float2(pts[4].x + side * lean1 * 0.8, reach);
                    wid[3] = w * 0.7;
                    wid[4] = w * 0.62;
                    wid[5] = 0.0;
                    d = SdZigZag(q, pts, wid, 5);
                    tip = pts[5];
                }
                else
                {
                    pts[3] = float2(pts[2].x + side * lean2, reach);
                    pts[4] = pts[3];
                    pts[5] = pts[3];
                    wid[3] = 0.0;
                    wid[4] = 0.0;
                    wid[5] = 0.0;
                    d = SdZigZag(q, pts, wid, 3);
                    tip = pts[3];

                    // Now and then a thin fork off the jog's far corner, splitting away from the last stroke (which
                    // leans the other way) with a little kink of its own: a short branch, clearly smaller than the
                    // bolt, so the two prongs open into a V.
                    [branch] if (EFX_Hash21(float2(seed, 4.4)) < _ForkChance)
                    {
                        float2 forkPts[6];
                        forkPts[0] = pts[2] + float2(side * w * 0.3, 0.0);
                        forkPts[1] = forkPts[0] + float2(-side * 0.5, 0.42) * _ForkLength;
                        forkPts[2] = forkPts[1] + float2(side * 0.14, 0.18) * _ForkLength;
                        forkPts[3] = forkPts[2] + float2(-side * 0.26, 0.42) * _ForkLength;
                        forkPts[4] = forkPts[3];
                        forkPts[5] = forkPts[3];
                        float forkWid[6] = { w * 0.4, w * 0.3, w * 0.24, 0.0, 0.0, 0.0 };
                        d = min(d, SdZigZag(q, forkPts, forkWid, 3));
                    }
                }
                return d;
            }

            // One strike channel at this pixel: its bolt and impact star, as signed distances folded into
            // `boltSd` and `impactSd`. (phi, r) is the pixel; the strike roots at `rootPhi`, `age` s ago. While
            // the arc is still coming alive (`grow` below 1) the whole bolt is drawn smaller, about its root.
            void AddStrike(float phi, float r, float inner, float rootPhi, float age, float seed, float usableAngle,
                           float exitY, float reachBase, float grow, inout float boltSd, inout float impactSd)
            {
                bool show = age >= 0.0 && age < _StrikeHold && !(age >= _BlinkAt && age < _BlinkAt + _BlinkLength);
                [branch] if (!show)
                {
                    return;
                }
                float2 sc;
                sincos(phi - rootPhi, sc.x, sc.y);
                float2 q = float2(r * sc.x, r * sc.y - inner - _BoltRoot);
                // A random tilt, leaning inward near the arc's ends so the bolt stays over the arc; the zig-zag
                // leans the same way there.
                float rootShare = rootPhi / max(usableAngle, 1e-3);
                float lean = clamp(EFX_Hash21(float2(seed, 1.9)) * 2.0 - 1.0 - rootShare * 0.8, -1.0, 1.0);
                float2 cs;
                sincos(lean * _BoltTilt * LIGHTNING_DEGREE, cs.x, cs.y);
                q = float2(q.x * cs.y - q.y * cs.x, q.x * cs.x + q.y * cs.y) / grow;
                [branch] if (abs(q.x) > 2.4 || q.y < exitY - 1.0)
                {
                    return;
                }
                float side = EFX_Hash21(float2(seed, 1.3)) < 0.5 ? -1.0 : 1.0;
                side = abs(rootShare) > 0.5 ? -sign(rootShare) : side;
                float reach = reachBase + lerp(_BoltReachMin, _BoltReachMax, EFX_Hash21(float2(seed, 4.1)));
                // After the blink it comes back as a second drawing: same root, lean and reach, new zig-zag.
                float drawing = age >= _BlinkAt ? seed + 0.37 : seed;
                float2 tip;
                boltSd = min(boltSd, SdBolt(q, drawing, side, exitY, reach, tip) * grow);
                float size = _ImpactSize * (age < _ImpactTime * 0.4 ? _ImpactPop : 0.7);
                [branch] if (age < _ImpactTime && size > 0.0)
                {
                    float2 cs2;
                    sincos(EFX_Hash21(float2(seed, 8.1)) * LIGHTNING_TAU, cs2.x, cs2.y);
                    float2 iq = q - tip;
                    iq = float2(iq.x * cs2.y - iq.y * cs2.x, iq.x * cs2.x + iq.y * cs2.y);
                    impactSd = min(impactSd, SdImpact(iq, size, round(_ImpactPoints), seed) * grow);
                }
            }

            // A seam's crackle: whether it's lit now (bursts of flicker every so often), and its seed.
            float CrackleLit(float id, float t, out float seed)
            {
                float every = _CrackleBurstEvery * (0.6 + 0.8 * EFX_Hash21(float2(id, 31.7)));
                float phase = frac(t / every + EFX_Hash21(float2(id, 47.3)));
                float flick = Fold(floor(t * _CrackleFlicker), 503.0);
                seed = flick + id * 17.0;
                return step(phase * every, _CrackleBurstLength) * step(EFX_Hash21(float2(id + 0.37, flick)), _CrackleOnChance);
            }

            // Where a strike channel's strike lands along the usable span: -1..1.
            float StrikeSpot(float seed)
            {
                return EFX_Hash21(float2(seed, 7.3)) * 2.0 - 1.0;
            }

            // How long after its beat starts a channel's strike lands.
            float StrikeDelay(float seed, float period)
            {
                return EFX_Hash21(float2(seed, 3.1)) * max(period - _StrikeHold, 0.0);
            }

            // ---------- The strike schedule (the same for the whole arc, so per vertex) ----------
            LightningVaryings LightningVertex(ArcAttributes input)
            {
                LightningVaryings o;
                o.arc = ArcVertex(input);

                float t = _Time.y;
                float midRadius = _ArcShape.y + ArcThickness() * 0.5;
                float usable = max(0.5 * ArcSweep() * midRadius - _BoltEndMargin, 0.0);
                float usableAngle = usable / midRadius;
                float period = 1.0 / _StrikeRate;
                float arcSeed = frac(_ArcShape.w * 0.159) * 97.0;

                // Two channels on the same beat, half a beat apart, so a bolt is out most of the time. Channel
                // A's beat is always one of the two A beats channel B's beat straddles, so those two are worked
                // out once and A takes its own from them.
                float tB = t - 0.5 * period;
                float tickB = floor(tB * _StrikeRate);
                float tickA = floor(t * _StrikeRate);
                float seedA0 = Fold(tickB, 509.0) + arcSeed;
                float seedA1 = Fold(tickB + 1.0, 509.0) + arcSeed;
                float delayA0 = StrikeDelay(seedA0, period);
                float delayA1 = StrikeDelay(seedA1, period);
                float spotA0 = StrikeSpot(seedA0) * usableAngle;
                float spotA1 = StrikeSpot(seedA1) * usableAngle;
                bool laterA = tickA > tickB;
                float seedA = laterA ? seedA1 : seedA0;
                float delayA = laterA ? delayA1 : delayA0;
                float ageA = t - tickA * period - delayA;
                float phiA = laterA ? spotA1 : spotA0;

                float seedB = Fold(tickB, 509.0) + arcSeed + 211.0;
                float delayB = StrikeDelay(seedB, period);
                float ageB = tB - tickB * period - delayB;
                float phiB = StrikeSpot(seedB) * usableAngle;

                // Channel B keeps clear of every strike that can be out with its own. A strike never outlasts
                // its beat, so those are the strikes of the two A beats B's beat straddles, and a grant's strike
                // (from the arc's middle, at _FlareTime). If B's spot is too close to one of them, it moves to a
                // random spot in the widest stretch clear of them all. (A spot that can't meet B's strike stands
                // in as a copy of one that can; a copy's own gap is empty, so it never wins.)
                float landB = (tickB + 0.5) * period + delayB;
                bool meetsA0 = abs(tickB * period + delayA0 - landB) < _StrikeHold;
                bool meetsA1 = abs((tickB + 1.0) * period + delayA1 - landB) < _StrikeHold;
                // Only a B strike landing after the grant dodges it; one already out stays where it is rather
                // than jumping aside mid-strike.
                bool meetsGrant = landB >= _FlareTime && landB - _FlareTime < _StrikeHold;
                float anySpot = meetsA0 ? spotA0 : (meetsA1 ? spotA1 : (meetsGrant ? 0.0 : 1e3));
                float3 spots = float3(meetsA0 ? spotA0 : anySpot, meetsA1 ? spotA1 : anySpot, meetsGrant ? 0.0 : anySpot);
                float separation = _StrikeSeparation / midRadius;
                float3 apart = abs(phiB - spots);
                [branch] if (min(apart.x, min(apart.y, apart.z)) < separation)
                {
                    float lo = min(spots.x, min(spots.y, spots.z));
                    float hi = max(spots.x, max(spots.y, spots.z));
                    float mid = max(min(spots.x, spots.y), min(max(spots.x, spots.y), spots.z));
                    float2 gap = float2(-usableAngle, lo - separation);
                    float2 gap1 = float2(lo + separation, mid - separation);
                    float2 gap2 = float2(mid + separation, hi - separation);
                    float2 gap3 = float2(hi + separation, usableAngle);
                    gap = (gap1.y - gap1.x) > (gap.y - gap.x) ? gap1 : gap;
                    gap = (gap2.y - gap2.x) > (gap.y - gap.x) ? gap2 : gap;
                    gap = (gap3.y - gap3.x) > (gap.y - gap.x) ? gap3 : gap;
                    phiB = gap.y > gap.x ? lerp(gap.x, gap.y, EFX_Hash21(float2(seedB, 7.9))) : 0.5 * (gap.x + gap.y);
                }

                // A just-granted arc strikes at once, from its middle, on channel A. Channel A's own strike of
                // the beat the grant came out in is dropped, rather than showing up mid-strike once it's gone.
                float sinceGrant = t - _FlareTime;
                bool grant = sinceGrant >= 0.0 && sinceGrant < _StrikeHold;
                bool dropped = sinceGrant >= _StrikeHold && abs(tickA * period + delayA - _FlareTime) < _StrikeHold;
                ageA = grant ? sinceGrant : (dropped ? 1e4 : ageA);
                phiA = grant ? 0.0 : phiA;
                seedA = grant ? seedA + 0.5 : seedA;
                float grantLit = (sinceGrant >= 0.0 && sinceGrant < _FlashTime * 2.0) ? 1.0 : 0.0;

                o.strikeA = float4(ageA, phiA, seedA, grantLit);
                o.strikeB = float4(ageB, phiB, seedB, usableAngle);
                return o;
            }

            half4 LightningFragment(LightningVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input.arc);
                float px = ArcPixel(input.arc);
                half energy = ArcEnergy();
                half takeover = ArcTakeover();

                // The shared tile, while it still shows.
                half4 tile = half4(0.0, 0.0, 0.0, 0.0);
                if (takeover < 0.999)
                {
                    tile = EFX_Over(ArcNeutral(input.arc, f), ArcHalo(f, input.arc.color.rgb));
                }
                [branch] if (energy < 0.001)
                {
                    return tile;
                }

                float t = _Time.y;
                float inner = _ArcShape.y;
                float thickness = ArcThickness();
                float r = length(input.arc.positionOS);
                float phi = (input.arc.uv.x - 0.5) * ArcSweep();   // radians from the arc's centre
                float y = r - inner;                               // world units out from the gear's inner edge
                float midRadius = inner + thickness * 0.5;

                gBoilTime = floor(t * _BoilRate) / _BoilRate;
                gBoilTick = Fold(floor(t * _BoilRate), 499.0);

                // The strikes, from the vertex stage.
                gAgeA = input.strikeA.x;
                gPhiA = input.strikeA.y;
                float seedA = input.strikeA.z;
                gGrant = input.strikeA.w;
                gAgeB = input.strikeB.x;
                gPhiB = input.strikeB.y;
                float seedB = input.strikeB.z;
                float usableAngle = input.strikeB.w;

                // ---------- The cloud ----------
                float crownY = thickness - _CrownDrop;
                float cloudTop = crownY + (_CrownSize + _CrownSizeRandom + _Swell + _BoilJitter + _ThrowPop) * energy
                                 + _BoilWobble + _CloudInkWidth + _ArcHaloWidth + 2.0 * px;
                half4 storm = half4(0.0, 0.0, 0.0, 0.0);
                half4 halo = half4(0.0, 0.0, 0.0, 0.0);
                [branch] if (y < cloudTop && y > -0.8)
                {
                    // Each row of puffs only reaches so far above and below its centre line (its biggest puff,
                    // boiled, swollen and popped as far as it goes), so a pixel out of a row's reach skips it.
                    // That changes nothing there: a puff only paints inside itself, and only moves the cloud's
                    // outline, ink and halo within the halo's width outside it. (An ellipse's distance below or
                    // above it is at least the gap times its width, when that's under 1.)
                    float outlineReach = _ArcHaloWidth + px;

                    // The underside: big gentle scallops along the inner edge.
                    float under = 1e4;
                    [branch] if (abs(y - _UnderRaise) < _UnderSize + _Swell + _BoilJitter + _BoilWobble + outlineReach)
                    {
                        float underRadius = inner + _UnderRaise;
                        float underId = floor(phi * underRadius / _UnderSpacing);
                        Puff underA = MakePuff(phi, r, y, underId - 1.0, _UnderSpacing, underRadius, _UnderRaise, _UnderSize, 0.0, 1.0, 1.0, 23.0, 1.0, 0.0);
                        Puff underB = MakePuff(phi, r, y, underId, _UnderSpacing, underRadius, _UnderRaise, _UnderSize, 0.0, 1.0, 1.0, 23.0, 1.0, 0.0);
                        Puff underC = MakePuff(phi, r, y, underId + 1.0, _UnderSpacing, underRadius, _UnderRaise, _UnderSize, 0.0, 1.0, 1.0, 23.0, 1.0, 0.0);
                        under = min(underA.sd, min(underB.sd, underC.sd));
                    }

                    // The body: the band from the scallops' centres up to the crown's centres, so only puffs make
                    // the outline (no flat stretches between them).
                    float lift = _UnderRaise * 0.5;
                    float2 bodyHalf = float2(f.halfSize.x, max(0.5 * (crownY - lift), 0.01));
                    float2 bodyP = f.p - float2(0.0, 0.5 * (crownY + lift) - 0.5 * thickness);
                    float body = EFX_SdRoundBox(bodyP, bodyHalf, _ArcCornerRadius);
                    // Each end rounds off in one big puff, bulging a little past the band's end.
                    float endAlong = 0.5 * ArcSweep() * midRadius - _EndPuffSize + _EndPuffBulge;
                    float endPhi = endAlong / midRadius;
                    float endCentreY = thickness * 0.5 + 0.08;
                    float2 endQ = float2((abs(phi) - endPhi) * r, y - endCentreY);
                    float endPuff = length(endQ) - _EndPuffSize;
                    body = min(max(body, (abs(phi) - endPhi) * r), endPuff);

                    // The shadow: a band along the underside's scalloped edge, thinning away up the sides.
                    float lower = min(under, endPuff);
                    float shadowDepth = _ShadowDepth * saturate((_UnderRaise - y) / 0.6);
                    half shadow = EFX_FillPx(lower, px) * (1.0 - EFX_FillPx(lower + shadowDepth, px)) * step(1e-3, shadowDepth);

                    // Paint. The cloud is crown above the billows' centres and billow below, so gaps between
                    // puffs never show through: the crown shows in the valleys between the billows.
                    half3 crownTone = gGrant > 0.5 ? _CloudTop.rgb : _CloudLight.rgb;
                    half3 rgb = lerp(crownTone, _CloudMid.rgb, EFX_FillPx(y - _LumpHeight, px));
                    // The end puffs: billow tone with a crown-tone top, inked along the top, so the tone split
                    // follows their round instead of cutting straight across.
                    Puff endP;
                    endP.sd = endPuff;
                    endP.cap = length(endQ + float2(0.0, 0.55 * _EndPuffSize)) - _EndPuffSize;
                    endP.capCut = -1e4;
                    endP.upper = saturate((endQ.y / _EndPuffSize - _InkArcStart) * 3.0) * step(0.0, abs(phi) - endPhi + 0.3 / midRadius);
                    endP.phi = endPhi;
                    endP.id = 0.0;
                    PaintPuff(rgb, endP, _CloudMid.rgb, crownTone, 1.0, px);

                    // The crown: puffs centred just inside the outer edge, bulging past it. They grow with the
                    // arc's energy, boil, swell and pops too, so dying down the crown shrinks right back into
                    // the tile. (Its reach counts two pops: both strike channels can pop the same puff at once.)
                    float crownSd = 1e4;
                    float crownReach = (_CrownSize + _CrownSizeRandom + _Swell + _BoilJitter + 2.0 * _ThrowPop + _BoilWobble) * energy
                                       + outlineReach / min(_CrownWidth, 1.0);
                    [branch] if (y > crownY - crownReach)
                    {
                        float crownRadius = inner + crownY;
                        float crownId = floor(phi * crownRadius / _CrownSpacing);
                        float crownSize = _CrownSize * energy;
                        float crownRandom = _CrownSizeRandom * energy;
                        Puff crownA = MakePuff(phi, r, y, crownId - 1.0, _CrownSpacing, crownRadius, crownY, crownSize, crownRandom, _CrownWidth, _CrownInkStart, 11.0, energy, energy);
                        Puff crownB = MakePuff(phi, r, y, crownId, _CrownSpacing, crownRadius, crownY, crownSize, crownRandom, _CrownWidth, _CrownInkStart, 11.0, energy, energy);
                        Puff crownC = MakePuff(phi, r, y, crownId + 1.0, _CrownSpacing, crownRadius, crownY, crownSize, crownRandom, _CrownWidth, _CrownInkStart, 11.0, energy, energy);
                        crownSd = min(crownA.sd, min(crownB.sd, crownC.sd));
                        half3 capA = lerp(_CloudTop.rgb, _FlashColor.rgb, CapFlash(crownA.phi, crownRadius));
                        half3 capB = lerp(_CloudTop.rgb, _FlashColor.rgb, CapFlash(crownB.phi, crownRadius));
                        half3 capC = lerp(_CloudTop.rgb, _FlashColor.rgb, CapFlash(crownC.phi, crownRadius));
                        PaintRow(rgb, crownA, crownB, crownC, crownTone, capA, capB, capC, 1.0, px);
                    }
                    float cloud = min(body, min(under, crownSd));

                    // The billows across the middle: wide and flat, inked only along their tops. They're inside
                    // the cloud, so only their insides count.
                    float lumpRadius = inner + _LumpHeight;
                    [branch] if (abs(y - _LumpHeight) < _LumpSize + _LumpSizeRandom + _Swell + _BoilJitter + _BoilWobble + px / min(_LumpWidth, 1.0))
                    {
                        float lumpId = floor(phi * lumpRadius / _LumpSpacing);
                        Puff lumpA = MakePuff(phi, r, y, lumpId - 1.0, _LumpSpacing, lumpRadius, _LumpHeight, _LumpSize, _LumpSizeRandom, _LumpWidth, _InkArcStart, 5.0, 1.0, 0.0);
                        Puff lumpB = MakePuff(phi, r, y, lumpId, _LumpSpacing, lumpRadius, _LumpHeight, _LumpSize, _LumpSizeRandom, _LumpWidth, _InkArcStart, 5.0, 1.0, 0.0);
                        Puff lumpC = MakePuff(phi, r, y, lumpId + 1.0, _LumpSpacing, lumpRadius, _LumpHeight, _LumpSize, _LumpSizeRandom, _LumpWidth, _InkArcStart, 5.0, 1.0, 0.0);
                        PaintRow(rgb, lumpA, lumpB, lumpC, _CloudMid.rgb, _CloudMid.rgb, _CloudMid.rgb, _CloudMid.rgb, 0.0, px);
                    }
                    rgb = lerp(rgb, _CloudDeep.rgb, shadow);

                    // The crackle on the nearest seam between two billows, while it's lit: a short slanted
                    // zig-zag across the seam, redrawn every flicker.
                    float seamId = floor(phi * lumpRadius / _LumpSpacing + 0.5) - 1.0;
                    float seamAlong = (seamId + 1.0) * _LumpSpacing;
                    float crackleSeed;
                    half crackleOn = CrackleLit(seamId, t, crackleSeed)
                                     * step(abs(seamAlong), 0.5 * ArcSweep() * lumpRadius - _CrackleEndMargin);
                    [branch] if (crackleOn > 0.5)
                    {
                        float2 centre = float2(seamAlong + (EFX_Hash21(float2(crackleSeed, 7.7)) - 0.5) * 0.3,
                                               _CrackleHeight + _CrackleHeightRandom * (EFX_Hash21(float2(crackleSeed, 8.8)) * 2.0 - 1.0));
                        float2 dq = float2(phi * r - centre.x * r / lumpRadius, y - centre.y);
                        float slant = (_CrackleTilt + _CrackleTiltRandom * (EFX_Hash21(float2(crackleSeed, 9.4)) * 2.0 - 1.0))
                                      * (EFX_Hash21(float2(crackleSeed, 9.9)) < 0.5 ? -1.0 : 1.0);
                        float2 cs;
                        sincos(slant * LIGHTNING_DEGREE, cs.x, cs.y);
                        float2 cq = float2(dq.x * cs.y + dq.y * cs.x, -dq.x * cs.x + dq.y * cs.y);
                        // Only pixels near the zig-zag build it. SdZigZag keeps each stroke inside a disc round its
                        // middle (half its length + 3 widths + 0.3), and the strokes span at most 0.37 of the
                        // length along and two swings across, so nothing it draws, ink included, lies outside
                        // this box — whatever the knobs (a tapered stroke's mitre can reach further than its
                        // width suggests).
                        float guard = 3.0 * _CrackleWidth + 0.3 + _CrackleInkWidth;
                        float2 crackleReach = float2(_CrackleSwing, 0.185 * _CrackleLength) + guard + px;
                        [branch] if (abs(cq.x) < 0.5 * _CrackleLength + crackleReach.x && abs(cq.y) < _CrackleSwing + crackleReach.y)
                        {
                            float flip = EFX_Hash21(float2(crackleSeed, 6.6)) < 0.5 ? -1.0 : 1.0;
                            float2 cpts[6];
                            float cwid[6];
                            [unroll] for (int k = 0; k < 6; k++)
                            {
                                float u = min(k, 4) / 4.0 - 0.5;
                                float jitterX = (EFX_Hash21(float2(crackleSeed, k + 1.0)) - 0.5) * 0.12;
                                float swing = (k == 0 || k >= 4) ? 0.0 : ((k & 1) ? flip : -flip) * _CrackleSwing * (0.6 + 0.4 * EFX_Hash21(float2(crackleSeed, k + 5.0)));
                                cpts[k] = float2((u + (k == 0 || k >= 4 ? 0.0 : jitterX)) * _CrackleLength, swing);
                                cwid[k] = k == 0 ? _CrackleWidth * 0.5 : (k >= 4 ? 0.0 : _CrackleWidth);
                            }
                            float crackle = SdZigZag(cq, cpts, cwid, 4);
                            half3 crackleRgb = lerp(_CrackleColor.rgb * _CrackleGlow, _CoreColor.rgb * _CoreGlow,
                                                    EFX_FillPx(crackle + _CrackleWidth * 0.55, px));
                            rgb = lerp(rgb, lerp(_InkColor.rgb, crackleRgb, EFX_FillPx(crackle, px)),
                                       EFX_FillPx(crackle - _CrackleInkWidth, px));
                        }
                    }

                    // Ink round the whole cloud.
                    rgb = lerp(rgb, _InkColor.rgb, 1.0 - EFX_FillPx(cloud + _CloudInkWidth, px));
                    half outsideTile = 1.0 - EFX_FillPx(f.sdf, px);
                    // While the arc comes alive the crown bursts out of the tile first; everything else, the end
                    // puffs too, follows the takeover. Dying down, the crown goes last, fading out with the bolts.
                    half burstOut = outsideTile * step(thickness * 0.5, y) * EFX_FillPx(abs(f.p.x) - f.halfSize.x, px)
                                    * saturate(energy * 4.0);
                    storm = half4(rgb, EFX_FillPx(cloud, px) * lerp(takeover, 1.0, burstOut));

                    // Aimed at while active: the halo follows the cloud's outline instead of the tile's.
                    half ring = EFX_FillPx(cloud - _ArcHaloWidth, px) * (1.0 - EFX_FillPx(cloud, px));
                    halo = half4(lerp(input.arc.color.rgb, 1.0, 0.5) * _ArcHaloGlow, ring * _Highlight * takeover);
                }

                // ---------- The bolts, from behind the cloud ----------
                float boltSd = 1e4;
                float impactSd = 1e4;
                [branch] if (storm.a < 0.999)
                {
                    float exitY = crownY + _CrownSize - _BoltRoot;   // where a bolt comes out of the crown
                    float reachBase = thickness - _BoltRoot;
                    float grow = lerp(0.35, 1.0, energy);
                    AddStrike(phi, r, inner, gPhiA, gAgeA, seedA, usableAngle, exitY, reachBase, grow, boltSd, impactSd);
                    AddStrike(phi, r, inner, gPhiB, gAgeB, seedB, usableAngle, exitY, reachBase, grow, boltSd, impactSd);
                }

                half3 boltRgb = lerp(_BoltColor.rgb * _BoltGlow, _CoreColor.rgb * _CoreGlow, EFX_FillPx(boltSd + _BoltCoreInset, px));
                boltRgb = lerp(_InkColor.rgb, boltRgb, EFX_FillPx(boltSd, px));
                half4 bolts = half4(boltRgb, EFX_FillPx(boltSd - _BoltInkWidth, px) * saturate(energy * 4.0));

                half3 impactRgb = lerp(_ImpactColor.rgb * _ImpactGlow, _CoreColor.rgb * _CoreGlow,
                                       EFX_FillPx(impactSd + _ImpactSize * 0.3, px));
                impactRgb = lerp(_InkColor.rgb, impactRgb, EFX_FillPx(impactSd, px));
                half4 impact = half4(impactRgb, EFX_FillPx(impactSd - _ImpactInkWidth, px) * saturate(energy * 4.0));

                // The tile gives way to the cloud as it takes over.
                tile.a *= 1.0 - takeover;
                // Bolts sit behind the tile while it still shows, so they come out of it, not over it.
                half4 result = EFX_Over(bolts, halo);
                result = EFX_Over(tile, result);
                result = EFX_Over(storm, result);
                return EFX_Over(impact, result);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
