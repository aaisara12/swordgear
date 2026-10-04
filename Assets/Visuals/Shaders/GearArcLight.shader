Shader "Swordgear/Gear Arc Light"
{
    // Light's gear section. Idle or aimed at, it's the shared cartoon tile in Light's pearl colour. Active,
    // the tile turns to cartoon opal — Opalite's pastel sheen cut into flat bands with white seams, flowing
    // across it — while pastel music notes float up out of it past the gear, wobbling as they rise, and
    // sparkles pop around them: Light is a harp.
    //
    // The sheen runs on world position, as Opalite's does on the notes: as the gear trails the player the
    // bands slide across the stone, which reads as light catching it.
    Properties
    {
        [Header(Opal)]
        _MilkColor ("Milk Colour", Color) = (0.95, 0.93, 0.98, 1)
        _SheenScale ("Sheen Scale (bands per unit)", Range(0.05, 4)) = 0.3
        _SheenSpeed ("Sheen Speed", Range(0, 2)) = 0.5
        _SheenAngle ("Sheen Angle (degrees)", Range(0, 360)) = 35
        _Swirl ("Swirl", Range(0, 1.5)) = 0.45
        _Bands ("Colour Bands Per Cycle", Range(3, 8)) = 5
        _InkColor ("Ink", Color) = (0.32, 0.22, 0.45, 1)
        _Emission ("Emission (HDR)", Range(1, 3)) = 1.25

        [Header(Notes)]
        _NoteReach ("Reach Past The Gear (world units)", Range(0, 3.5)) = 2.6
        _NoteSize ("Size (world units)", Range(0.3, 1.2)) = 0.95
        _NoteSpacing ("Spacing (world units)", Range(1.5, 5)) = 2.1
        _NoteRate ("Notes Per Second, Per Slot", Range(0.1, 2)) = 0.55
        _SparkleDensity ("Sparkle Density", Range(0, 1)) = 0.35

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
            #pragma fragment LightFragment

            #define GEAR_ARC_MATERIAL_PROPERTIES \
                half4 _MilkColor; half _SheenScale; half _SheenSpeed; half _SheenAngle; half _Swirl; half _Bands; \
                half4 _InkColor; half _Emission; \
                half _NoteReach; half _NoteSize; half _NoteSpacing; half _NoteRate; half _SparkleDensity;
            #include "GearArcCommon.hlsl"

            // Pastel rainbow, as in Opalite.shader: a high floor and a small swing keep it opalescent, not neon.
            half3 Pastel(float t, half swing)
            {
                return 0.78 + swing * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            float2 Rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            // Signed distance to a cartoon eighth note, head at the origin, about one unit tall.
            float SdNote(float2 p)
            {
                float2 h = Rotate(p, 0.35) / float2(0.3, 0.21);
                float head = (length(h) - 1.0) * 0.21;
                float stem = EFX_SdRoundBox(p - float2(0.24, 0.52), float2(0.06, 0.52), 0.04);
                float flag = EFX_SdRoundBox(Rotate(p - float2(0.42, 0.88), 0.6), float2(0.2, 0.08), 0.06);
                return min(head, min(stem, flag));
            }

            half4 LightFragment(ArcVaryings input) : SV_Target
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

                // Opal: Opalite's drifting, swirling sheen, cut into flat pastel bands with white seams.
                float2 p = input.positionWS;
                float angle = radians(_SheenAngle) + t * 0.05;
                float swirl = sin(p.x * 1.7 + t * 0.6) * cos(p.y * 1.3 - t * 0.4) * _Swirl;
                float s = (dot(p, float2(cos(angle), sin(angle))) * _SheenScale + t * _SheenSpeed + swirl) * _Bands;
                half3 rgb = lerp(_MilkColor.rgb, Pastel(floor(s) / _Bands, 0.3), 0.8) * _Emission;
                float seam = min(frac(s), 1.0 - frac(s));
                rgb = lerp(rgb, 1.6, 1.0 - EFX_Step(0.06, seam));
                rgb = lerp(rgb, _InkColor.rgb, max(EFX_Step(-ArcInkWidth, f.sdf), ArcChargeEdge(x)));
                half4 opal = half4(rgb, EFX_Fill(f.sdf) * ArcTakeoverAt(x));

                // Notes: one per slot on its own clock, popping out of the opal, wobbling up past the gear and
                // shrinking away, each its own pastel.
                float slot = floor(x / _NoteSpacing);
                float seed = EFX_Hash21(float2(slot, 5.7));
                float life = frac(t * _NoteRate * (0.8 + 0.4 * seed) + seed);
                float centreX = (slot + 0.5) * _NoteSpacing;
                float2 noteAt = float2(centreX + sin(life * 9.0 + seed * 6.0) * 0.25,
                                       thickness * 0.55 + life * (thickness * 0.45 + _NoteReach * energy));
                float size = _NoteSize * smoothstep(0.0, 0.12, life) * (1.0 - smoothstep(0.75, 1.0, life))
                             * ArcEndFade(centreX, 1.2) * ArcTakeover();
                // The arc's frame runs counter-clockwise, which mirrors it against the screen; flip x back so
                // the notes don't read backwards.
                float2 fromNote = (float2(x, y) - noteAt) * float2(-1.0, 1.0);
                float2 local = Rotate(fromNote, sin(t * 4.0 + seed * 9.0) * 0.25) / max(size, 1e-3) + float2(0.1, 0.45);
                float note = SdNote(local) * size;
                half4 notes = half4(_InkColor.rgb, EFX_FillPx(note - ArcInkWidth * 0.55, px));
                notes = EFX_Over(half4(Pastel(seed, 0.4) * _Emission * 1.2, EFX_FillPx(note, px)), notes);
                notes.a *= step(0.01, size);

                // Sparkles popping round the notes.
                float2 sparkleCell = floor(float2(x, y) / 1.3);
                float sparkleSeed = EFX_Hash21(sparkleCell + 7.4);
                float2 sparkleCentre = (sparkleCell + 0.5 + (EFX_Hash22(sparkleCell + 1.1) - 0.5) * 0.6) * 1.3;
                float pop = pow(saturate(sin(t * (2.0 + sparkleSeed * 3.0) + sparkleSeed * 40.0)), 3.0);
                float sparkleSize = 0.45 * pop * step(1.0 - _SparkleDensity, sparkleSeed) * ArcTakeover() * ArcEndFade(sparkleCentre.x, 1.0);
                float2 fromSparkle = float2(x, y) - sparkleCentre;
                float sparkle = EFX_SdStar4(fromSparkle, max(sparkleSize, 1e-3));
                half4 sparkles = half4(Pastel(sparkleSeed * 3.0, 0.3) * _Emission * 1.8,
                                       EFX_FillPx(sparkle, px) * step(length(fromSparkle), sparkleSize) * step(thickness * 0.6, y));

                return EFX_Over(sparkles, EFX_Over(notes, EFX_Over(opal, tile)));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
