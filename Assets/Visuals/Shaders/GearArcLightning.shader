Shader "Swordgear/Gear Arc Lightning"
{
    // Lightning's gear section. Idle or aimed at, it's the shared cartoon tile in Lightning's colour. Active,
    // the tile turns to a storm — indigo cloud puffs rolling along it — and cartoon bolts never stop: one
    // crawls zig-zagging through the cloud while fresh ones strike out past the gear many times a second, each
    // a fat yellow zig-zag with a white-hot core in dark ink, sparks popping, the storm strobing as they land.
    Properties
    {
        [Header(Bolts)]
        _BoltReach ("Bolt Reach Past The Gear (world units)", Range(0, 3.5)) = 3.2
        _BoltSpacing ("Bolt Spacing (world units)", Range(0.8, 4)) = 1.7
        _BoltWidth ("Bolt Half-Width (world units)", Range(0.05, 0.4)) = 0.26
        _StrikeRate ("Strikes Per Second", Range(1, 30)) = 9
        _BoltColor ("Bolt", Color) = (1.0, 0.86, 0.15, 1)
        _CoreColor ("Bolt Core", Color) = (1.0, 1.0, 0.85, 1)
        _InkColor ("Ink", Color) = (0.07, 0.04, 0.2, 1)
        _Emission ("Bolt Emission (HDR)", Range(1, 4)) = 1.8

        [Header(Storm)]
        _StormColor ("Storm", Color) = (0.14, 0.12, 0.38, 1)
        _CloudColor ("Cloud", Color) = (0.28, 0.26, 0.6, 1)
        _CloudSpeed ("Cloud Drift (world units / s)", Range(0, 4)) = 0.8
        _SparkDensity ("Spark Density", Range(0, 1)) = 0.3

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
            #pragma fragment LightningFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half _BoltReach; half _BoltSpacing; half _BoltWidth; half _StrikeRate; \
                half4 _BoltColor; half4 _CoreColor; half4 _InkColor; half _Emission; \
                half4 _StormColor; half4 _CloudColor; half _CloudSpeed; half _SparkDensity;
            #include "GearArcCommon.hlsl"

            half4 LightningFragment(ArcVaryings input) : SV_Target
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

                // Every bolt is redrawn on each tick: lightning never holds a shape. Now and then a tick
                // strobes the whole storm.
                float tick = floor(t * _StrikeRate);
                half strobe = step(0.75, EFX_Hash21(float2(tick, 9.1)));

                // The storm: indigo, with a row of cloud puffs along its inner half rolling along the arc.
                float puff = frac((x - t * _CloudSpeed) / 0.95) * 2.0 - 1.0;
                float cloudTop = thickness * 0.48 + 0.4 * sqrt(saturate(1.0 - puff * puff));
                half3 rgb = lerp(_CloudColor.rgb, _StormColor.rgb, EFX_Step(0.0, y - cloudTop));
                rgb *= 1.0 + strobe * 0.9;

                // A bolt crawling through the cloud along the arc, re-kinked every tick.
                float kink = x / 0.7;
                float k = floor(kink);
                float crawlY = thickness * lerp(0.22 + 0.56 * EFX_Hash21(float2(k, tick)),
                                                0.22 + 0.56 * EFX_Hash21(float2(k + 1.0, tick)), frac(kink));
                float crawl = EFX_FieldToDistance(abs(y - crawlY), x) - _BoltWidth * 0.7;
                half crawlOn = step(0.3, EFX_Hash21(float2(tick, 4.4)));
                rgb = lerp(rgb, _InkColor.rgb, EFX_Fill(crawl - ArcInkWidth) * crawlOn);
                rgb = lerp(rgb, _BoltColor.rgb * _Emission, EFX_Fill(crawl) * crawlOn);
                rgb = lerp(rgb, _CoreColor.rgb * _Emission * 1.3, EFX_Fill(crawl + _BoltWidth * 0.4) * crawlOn);
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-ArcInkWidth, f.sdf));
                half4 storm = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Strikes: in each slot along the arc, a fresh bolt most ticks, rooted in the cloud, kinking
                // every 0.55 units on its way out and thinning to its tip.
                float id = floor(x / _BoltSpacing);
                half strikes = step(0.4, EFX_Hash21(float2(id, tick + 0.5)));
                float centre = (id + 0.5) * _BoltSpacing;
                float reach = _BoltReach * energy * (0.55 + 0.45 * EFX_Hash21(float2(id, tick + 3.7))) * ArcEndFade(centre, 1.0);
                float rootY = thickness * 0.75;
                float along = (y - rootY) / 0.55;
                float j = floor(along);
                float swing = _BoltSpacing * 0.17;
                float x0 = centre + (EFX_Hash21(float2(id * 7.1 + j, tick)) - 0.5) * 2.0 * swing * step(0.5, j);
                float x1 = centre + (EFX_Hash21(float2(id * 7.1 + j + 1.0, tick)) - 0.5) * 2.0 * swing;
                float slope = (x1 - x0) / 0.55;
                float fromAxis = abs(x - lerp(x0, x1, frac(along))) * rsqrt(1.0 + slope * slope);
                float halfWidth = _BoltWidth * (1.0 - 0.7 * saturate((y - rootY) / max(reach, 1e-3)));
                float bolt = max(fromAxis - halfWidth, max(rootY - y, y - rootY - (thickness * 0.25 + reach)));

                half boost = _Emission * (1.0 + strobe * 0.5);
                half inside = lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf));
                half4 bolts = half4(_InkColor.rgb, EFX_FillPx(bolt - ArcInkWidth, px));
                bolts = EFX_Over(half4(_BoltColor.rgb * boost, EFX_FillPx(bolt, px)), bolts);
                bolts = EFX_Over(half4(_CoreColor.rgb * boost * 1.4, EFX_FillPx(fromAxis - halfWidth * 0.4, px) * EFX_FillPx(bolt, px)), bolts);
                bolts.a *= strikes * inside;

                // Sparks popping round the strikes, re-scattered every tick.
                float2 sparkCell = floor(float2(x, y) / 1.1);
                float seed = EFX_Hash21(sparkCell + tick * 0.37);
                float2 sparkCentre = (sparkCell + 0.5 + (EFX_Hash22(sparkCell + tick) - 0.5) * 0.5) * 1.1;
                float sparkSize = 0.34 * step(1.0 - _SparkDensity, seed) * ArcTakeover() * ArcEndFade(sparkCentre.x, 1.0);
                float2 fromSpark = float2(x, y) - sparkCentre;
                float spark = EFX_SdStar4(fromSpark, max(sparkSize, 1e-3));
                half4 sparks = half4(_CoreColor.rgb * _Emission * 1.5,
                                     EFX_FillPx(spark, px) * step(length(fromSpark), sparkSize) * step(thickness * 0.9, y));

                return EFX_Over(sparks, EFX_Over(bolts, EFX_Over(storm, tile)));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
