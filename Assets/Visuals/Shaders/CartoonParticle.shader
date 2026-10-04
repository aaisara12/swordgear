Shader "Swordgear/Cartoon Particle"
{
    // The polish pass's cartoon particles. Each material picks a shape (_SHAPE_*), drawn in the particle's own
    // UV square as a flat two-tone cartoon — body, a shaded crescent, a highlight, an ink outline — with no
    // textures, so every element's bursts share one look and stay crisp at any size. The particle's colour is
    // the body; shade and ink are derived from it, so colour-over-lifetime recolours the whole cartoon.
    //
    // Renderers using it need custom vertex streams UV, AgePercent and StableRandom.x, which pack into
    // TEXCOORD0.xyzw: age thins rings, the random seeds each bolt's zig-zag. Shapes that point along their
    // flight (_ALIGN_VELOCITY: shards, bolts, comets) also need Velocity, in TEXCOORD1: the quad stays facing
    // the camera and the shape turns inside it. (Unity's own velocity alignment turns the quad edge-on to a
    // top-down camera.)
    Properties
    {
        [KeywordEnum(Blob, Star, Shard, Bolt, Ring, Puff, Comet)] _Shape ("Shape", Float) = 0
        _Emission ("Emission (HDR)", Range(0.5, 6)) = 1.5
        _InkTone ("Ink (the colour this dark)", Range(0, 1)) = 0.25
        _InkWidth ("Ink Width (of the half-size)", Range(0, 0.3)) = 0.12
        _ShadeTone ("Shade (the colour this dark)", Range(0, 1)) = 0.72
        _HighlightDot ("Highlight Dot", Range(0, 1)) = 0.6
        _InkMaxPixels ("Ink Width Cap (pixels)", Range(0, 12)) = 4
        [Toggle(_ALIGN_VELOCITY)] _AlignVelocity ("Point Along Velocity", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex ParticleVertex
            #pragma fragment ParticleFragment
            #pragma shader_feature_local _SHAPE_BLOB _SHAPE_STAR _SHAPE_SHARD _SHAPE_BOLT _SHAPE_RING _SHAPE_PUFF _SHAPE_COMET
            #pragma shader_feature_local _ALIGN_VELOCITY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ElementFX.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Emission;
                half _InkTone;
                half _InkWidth;
                half _ShadeTone;
                half _HighlightDot;
                half _InkMaxPixels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 uvAgeSeed : TEXCOORD0;   // uv, AgePercent, StableRandom.x
                float3 velocity : TEXCOORD1;    // only streamed for _ALIGN_VELOCITY
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uvAgeSeed : TEXCOORD0;
                float2 heading : TEXCOORD1;     // unit direction of flight, on screen
            };

            Varyings ParticleVertex(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.color = input.color;
                o.uvAgeSeed = input.uvAgeSeed;
            #if defined(_ALIGN_VELOCITY)
                float2 v = input.velocity.xy;
                o.heading = dot(v, v) > 1e-6 ? normalize(v) : float2(0.0, 1.0);
            #else
                o.heading = float2(0.0, 1.0);
            #endif
                return o;
            }

            // Uneven capsule: a circle of radius r1 at the origin joined smoothly to one of r2 at (0, h).
            float SdUnevenCapsule(float2 p, float r1, float r2, float h)
            {
                p.x = abs(p.x);
                float b = (r1 - r2) / h;
                float a = sqrt(1.0 - b * b);
                float k = dot(p, float2(-b, a));
                if (k < 0.0) return length(p) - r1;
                if (k > a * h) return length(p - float2(0.0, h)) - r2;
                return dot(p, float2(a, b)) - r1;
            }

            // The shape, as a signed distance in the particle's square (-1..1 each way).
            float Shape(float2 q, float age, float seed)
            {
            #if defined(_SHAPE_STAR)
                return EFX_SdStar4(q, 0.95);
            #elif defined(_SHAPE_SHARD)
                // A long diamond, point first along +y.
                return (abs(q.x) * 2.6 + abs(q.y) - 0.95) * rsqrt(2.6 * 2.6 + 1.0);
            #elif defined(_SHAPE_BOLT)
                // A zig-zag along y, kinking every 0.38, each particle its own.
                float kink = (q.y + 0.95) / 0.38;
                float k = floor(kink);
                float x0 = (EFX_Hash21(float2(k, seed * 97.0)) - 0.5) * 0.7 * step(0.5, k);
                float x1 = (EFX_Hash21(float2(k + 1.0, seed * 97.0)) - 0.5) * 0.7;
                float slope = (x1 - x0) / 0.38;
                float across = abs(q.x - lerp(x0, x1, frac(kink))) * rsqrt(1.0 + slope * slope);
                return max(across - 0.2 * (1.0 - 0.5 * abs(q.y)), abs(q.y) - 0.95);
            #elif defined(_SHAPE_RING)
                // A ring that thins as the particle ages.
                float thickness = lerp(0.08, 0.012, age);
                return abs(length(q) - (0.94 - thickness)) - thickness;
            #elif defined(_SHAPE_PUFF)
                // A cartoon cloud: four overlapping bumps.
                float d = length(q - float2(0.0, 0.18)) - 0.52;
                d = min(d, length(q - float2(-0.45, -0.12)) - 0.4);
                d = min(d, length(q - float2(0.45, -0.12)) - 0.4);
                return min(d, length(q - float2(0.0, -0.32)) - 0.42);
            #elif defined(_SHAPE_COMET)
                // A round head leading along +y, a tail tapering away behind it.
                return SdUnevenCapsule(float2(q.x, 0.5 - q.y), 0.42, 0.06, 1.38);
            #else
                return length(q) - 0.8;
            #endif
            }

            half4 ParticleFragment(Varyings input) : SV_Target
            {
                float2 q = (input.uvAgeSeed.xy - 0.5) * 2.0;
            #if defined(_ALIGN_VELOCITY)
                // Turn the shape so its +y points along the flight. It has to fit the square's inscribed circle.
                float2 heading = normalize(input.heading);
                q = float2(dot(q, float2(heading.y, -heading.x)), dot(q, heading));
            #endif
                float age = input.uvAgeSeed.z;
                float seed = input.uvAgeSeed.w;

                float sdf = Shape(q, age, seed);
                half3 body = input.color.rgb;

                // Shade: the crescent of the shape not covered by itself nudged up-left, as if lit from there.
                // Shards shade one face instead, so they read as cut crystal.
            #if defined(_SHAPE_SHARD)
                half shade = EFX_Step(0.0, q.x);
            #elif defined(_SHAPE_RING) || defined(_SHAPE_BOLT)
                half shade = 0.0;
            #else
                half shade = EFX_Step(0.0, Shape(q + float2(0.2, -0.2), age, seed));
            #endif
                half3 rgb = lerp(body, body * _ShadeTone, shade) * _Emission;

            #if defined(_SHAPE_BLOB) || defined(_SHAPE_PUFF) || defined(_SHAPE_COMET)
                half glint = EFX_Fill(length(q - float2(-0.34, 0.36)) - 0.14) * _HighlightDot;
                rgb = lerp(rgb, max(rgb, 1.0) * 1.3, glint);
            #elif defined(_SHAPE_BOLT)
                rgb = lerp(rgb, max(rgb, 1.0) * 1.4, EFX_Fill(sdf + 0.08));   // a white-hot core
            #endif

                // Ink in the particle's own units, capped in pixels so a big ring isn't drawn in a fat marker.
                half ink = min(_InkWidth, _InkMaxPixels * fwidth(q.x));
                rgb = lerp(rgb, body * _InkTone, EFX_Step(-ink, sdf) * step(1e-4, ink));
                return half4(rgb, EFX_Fill(sdf) * input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
