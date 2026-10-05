Shader "Swordgear/Gear Arc Lightning"
{
    // Lightning's gear section. Idle or aimed at, it's the shared cartoon tile in Lightning's colour. Active,
    // the tile turns to a storm — indigo cloud puffs rolling along it — and cartoon bolts never stop: one
    // crawls zig-zagging through the cloud while fresh ones strike out past the gear many times a second, each
    // a fat yellow zig-zag with a white-hot core in dark ink, sparks popping, the storm strobing as they land.
    //
    // The loose pieces (the sparks) are particles, not this shader: ArcBitsLightning.prefab in
    // Assets/Visuals/Prefabs/ElementFX/ArcBits/, placed on the arc by GearArcArt.
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

        [Header(Strike Shape)]
        _StrikeGapChance ("Slot Left Dark On A Tick (0-1 chance)", Range(0, 1)) = 0.4
        _BoltReachMin ("Shortest Strike (share of Bolt Reach)", Range(0, 1)) = 0.55
        _BoltReachRandom ("Random Extra Strike Length (share of Bolt Reach)", Range(0, 1)) = 0.45
        _BoltEndFade ("Strikes Die Down Toward The Arc's Ends Over (world units)", Range(0.05, 4)) = 1.0
        _BoltRoot ("Strike Root Height (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.75
        _BoltKinkSpacing ("Strike Kink Spacing (world units)", Range(0.1, 2)) = 0.55
        _BoltSwing ("Strike Zig-Zag Swing (share of Bolt Spacing)", Range(0, 0.5)) = 0.17
        _BoltTaper ("Strike Thinning Toward Its Tip (0 none - 1 to a point)", Range(0, 1)) = 0.7
        _CoreShare ("Strike Core Width (share of the bolt's width)", Range(0, 1)) = 0.4
        _CoreGlow ("Strike Core Brightness (x Bolt Emission)", Range(0, 3)) = 1.4

        [Header(Crawling Bolt)]
        _CrawlGapChance ("Missing On A Tick (0-1 chance)", Range(0, 1)) = 0.3
        _CrawlKinkSpacing ("Crawl Kink Spacing (world units)", Range(0.1, 3)) = 0.7
        _CrawlLow ("Crawl Lowest Point (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.22
        _CrawlSpan ("Crawl Height Range (share of the band)", Range(0, 1)) = 0.56
        _CrawlWidth ("Crawl Half-Width (x Bolt Half-Width)", Range(0.1, 2)) = 0.7
        _CrawlCoreInset ("Crawl Core Inset From Its Edge (x Bolt Half-Width)", Range(0, 2)) = 0.4
        _CrawlCoreGlow ("Crawl Core Brightness (x Bolt Emission)", Range(0, 3)) = 1.3

        [Header(Storm)]
        _StormColor ("Storm", Color) = (0.14, 0.12, 0.38, 1)
        _CloudColor ("Cloud", Color) = (0.28, 0.26, 0.6, 1)
        _CloudSpeed ("Cloud Drift (world units / s)", Range(0, 4)) = 0.8
        _CloudBase ("Cloud Base Height (share of the band, 0 inner - 1 outer)", Range(0, 1)) = 0.48
        _CloudPuffHeight ("Cloud Puff Height (world units)", Range(0, 2)) = 0.4
        _CloudPuffSpacing ("Cloud Puff Spacing (world units)", Range(0.2, 4)) = 0.95

        [Header(Strobe)]
        _StrobeSkipChance ("Tick Without A Strobe (0-1 chance)", Range(0, 1)) = 0.75
        _StrobeStormFlash ("Storm Brightening On A Strobe (x extra)", Range(0, 3)) = 0.9
        _StrobeBoltFlash ("Bolt Brightening On A Strobe (x extra)", Range(0, 3)) = 0.5

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
                half _StrikeGapChance; half _BoltReachMin; half _BoltReachRandom; float _BoltEndFade; \
                half _BoltRoot; float _BoltKinkSpacing; half _BoltSwing; half _BoltTaper; half _CoreShare; half _CoreGlow; \
                half _CrawlGapChance; float _CrawlKinkSpacing; half _CrawlLow; half _CrawlSpan; \
                half _CrawlWidth; half _CrawlCoreInset; half _CrawlCoreGlow; \
                half4 _StormColor; half4 _CloudColor; half _CloudSpeed; \
                half _CloudBase; float _CloudPuffHeight; float _CloudPuffSpacing; \
                half _StrobeSkipChance; half _StrobeStormFlash; half _StrobeBoltFlash;
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
                half strobe = step(_StrobeSkipChance, EFX_Hash21(float2(tick, 9.1)));

                // The storm: indigo, with a row of cloud puffs along its inner half rolling along the arc.
                float puff = frac((x - t * _CloudSpeed) / _CloudPuffSpacing) * 2.0 - 1.0;
                float cloudTop = thickness * _CloudBase + _CloudPuffHeight * sqrt(saturate(1.0 - puff * puff));
                half3 rgb = lerp(_CloudColor.rgb, _StormColor.rgb, EFX_Step(0.0, y - cloudTop));
                rgb *= 1.0 + strobe * _StrobeStormFlash;

                // A bolt crawling through the cloud along the arc, re-kinked every tick.
                float kink = x / _CrawlKinkSpacing;
                float k = floor(kink);
                float crawlY = thickness * lerp(_CrawlLow + _CrawlSpan * EFX_Hash21(float2(k, tick)),
                                                _CrawlLow + _CrawlSpan * EFX_Hash21(float2(k + 1.0, tick)), frac(kink));
                float crawl = EFX_FieldToDistance(abs(y - crawlY), x) - _BoltWidth * _CrawlWidth;
                half crawlOn = step(_CrawlGapChance, EFX_Hash21(float2(tick, 4.4)));
                rgb = lerp(rgb, _InkColor.rgb, EFX_Fill(crawl - _ArcInkWidth) * crawlOn);
                rgb = lerp(rgb, _BoltColor.rgb * _Emission, EFX_Fill(crawl) * crawlOn);
                rgb = lerp(rgb, _CoreColor.rgb * _Emission * _CrawlCoreGlow, EFX_Fill(crawl + _BoltWidth * _CrawlCoreInset) * crawlOn);
                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_ArcInkWidth, f.sdf));
                half4 storm = half4(rgb, EFX_Fill(f.sdf) * ArcTakeover());

                // Strikes: in each slot along the arc, a fresh bolt most ticks, rooted in the cloud, kinking
                // every 0.55 units on its way out and thinning to its tip.
                float id = floor(x / _BoltSpacing);
                half strikes = step(_StrikeGapChance, EFX_Hash21(float2(id, tick + 0.5)));
                float centre = (id + 0.5) * _BoltSpacing;
                float reach = _BoltReach * energy * (_BoltReachMin + _BoltReachRandom * EFX_Hash21(float2(id, tick + 3.7))) * ArcEndFade(centre, _BoltEndFade);
                float rootY = thickness * _BoltRoot;
                float along = (y - rootY) / _BoltKinkSpacing;
                float j = floor(along);
                float swing = _BoltSpacing * _BoltSwing;
                float x0 = centre + (EFX_Hash21(float2(id * 7.1 + j, tick)) - 0.5) * 2.0 * swing * step(0.5, j);
                float x1 = centre + (EFX_Hash21(float2(id * 7.1 + j + 1.0, tick)) - 0.5) * 2.0 * swing;
                float slope = (x1 - x0) / _BoltKinkSpacing;
                float fromAxis = abs(x - lerp(x0, x1, frac(along))) * rsqrt(1.0 + slope * slope);
                float halfWidth = _BoltWidth * (1.0 - _BoltTaper * saturate((y - rootY) / max(reach, 1e-3)));
                float bolt = max(fromAxis - halfWidth, max(rootY - y, y - rootY - (thickness * (1.0 - _BoltRoot) + reach)));

                half boost = _Emission * (1.0 + strobe * _StrobeBoltFlash);
                half inside = lerp(ArcTakeover(), 1.0, EFX_Step(0.0, f.sdf));
                half4 bolts = half4(_InkColor.rgb, EFX_FillPx(bolt - _ArcInkWidth, px));
                bolts = EFX_Over(half4(_BoltColor.rgb * boost, EFX_FillPx(bolt, px)), bolts);
                bolts = EFX_Over(half4(_CoreColor.rgb * boost * _CoreGlow, EFX_FillPx(fromAxis - halfWidth * _CoreShare, px) * EFX_FillPx(bolt, px)), bolts);
                bolts.a *= strikes * inside;

                return EFX_Over(bolts, EFX_Over(storm, tile));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
