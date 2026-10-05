Shader "Swordgear/Cartoon Particle"
{
    // The polish pass's cartoon particles. Each material picks a shape (_Shape), drawn in the particle's own UV
    // square as a flat two-tone cartoon — body, a shade, a highlight, an ink outline — with no textures, so
    // every element's bursts share one look and stay crisp at any size. The particle's colour is the body;
    // shade and ink are derived from it, so colour-over-lifetime recolours the whole cartoon.
    //
    // The shape is a plain enum rather than keywords (there are more shapes than a KeywordEnum allows); every
    // particle of a material takes the same branch, so the branch costs next to nothing. Its Inspector dropdown
    // comes from the C# enum CartoonParticleShape (an inline [Enum] list stops working past seven entries), so
    // a new shape is added in both places with the same number.
    //
    // Renderers using it need custom vertex streams UV, AgePercent and StableRandom.x, which pack into
    // TEXCOORD0.xyzw: age thins rings, the random seeds each bolt's zig-zag and each rock's outline. Shapes
    // that point along their flight (_ALIGN_VELOCITY: shards, bolts, comets, dashes) also need Velocity, in
    // TEXCOORD1: the quad stays facing the camera and the shape turns inside it. (Unity's own velocity
    // alignment turns the quad edge-on to a top-down camera.)
    Properties
    {
        [Enum(CartoonParticleShape)] _Shape ("Shape", Float) = 0
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
            #pragma shader_feature_local _ALIGN_VELOCITY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ElementFX.hlsl"

            #define SHAPE_BLOB 0
            #define SHAPE_STAR 1
            #define SHAPE_SHARD 2
            #define SHAPE_BOLT 3
            #define SHAPE_RING 4
            #define SHAPE_PUFF 5
            #define SHAPE_COMET 6
            #define SHAPE_SWIRL 7
            #define SHAPE_ROCK 8
            #define SHAPE_NOTE 9
            #define SHAPE_LEAF 10
            #define SHAPE_DASH 11
            #define SHAPE_SNOWFLAKE 12

            CBUFFER_START(UnityPerMaterial)
                float _Shape;
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

            float2 Rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
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
            float Shape(int shape, float2 q, float age, float seed)
            {
                [branch] if (shape == SHAPE_STAR)
                {
                    return EFX_SdStar4(q, 0.95);
                }
                else if (shape == SHAPE_SHARD)
                {
                    // A long diamond, point first along +y.
                    return (abs(q.x) * 2.6 + abs(q.y) - 0.95) * rsqrt(2.6 * 2.6 + 1.0);
                }
                else if (shape == SHAPE_BOLT)
                {
                    // A zig-zag along y, kinking every 0.38, each particle its own.
                    float kink = (q.y + 0.95) / 0.38;
                    float k = floor(kink);
                    float x0 = (EFX_Hash21(float2(k, seed * 97.0)) - 0.5) * 0.7 * step(0.5, k);
                    float x1 = (EFX_Hash21(float2(k + 1.0, seed * 97.0)) - 0.5) * 0.7;
                    float slope = (x1 - x0) / 0.38;
                    float across = abs(q.x - lerp(x0, x1, frac(kink))) * rsqrt(1.0 + slope * slope);
                    return max(across - 0.2 * (1.0 - 0.5 * abs(q.y)), abs(q.y) - 0.95);
                }
                else if (shape == SHAPE_RING)
                {
                    // A ring that thins as the particle ages.
                    float thickness = lerp(0.08, 0.012, age);
                    return abs(length(q) - (0.94 - thickness)) - thickness;
                }
                else if (shape == SHAPE_PUFF)
                {
                    // A cartoon cloud: four overlapping bumps.
                    float d = length(q - float2(0.0, 0.18)) - 0.52;
                    d = min(d, length(q - float2(-0.45, -0.12)) - 0.4);
                    d = min(d, length(q - float2(0.45, -0.12)) - 0.4);
                    return min(d, length(q - float2(0.0, -0.32)) - 0.42);
                }
                else if (shape == SHAPE_COMET)
                {
                    // A round head leading along +y, a tail tapering away behind it.
                    return SdUnevenCapsule(float2(q.x, 0.5 - q.y), 0.42, 0.06, 1.38);
                }
                else if (shape == SHAPE_SWIRL)
                {
                    // A gust curl: about a turn and a quarter of a spiral stroke, thickening toward its tail.
                    const float pitch = 0.105;
                    float theta = atan2(q.y, q.x);
                    float phi = theta + 6.2831853 * round((length(q) / pitch - theta) / 6.2831853);
                    phi = clamp(phi, 1.2, 8.6);
                    float2 onSpiral = pitch * phi * float2(cos(phi), sin(phi));
                    return length(q - onSpiral) - (0.08 + 0.08 * (phi - 1.2) / 7.4);
                }
                else if (shape == SHAPE_ROCK)
                {
                    // A chunky rock: a rounded block with two corners knocked off, each its own proportions.
                    float d = EFX_SdRoundBox(q, float2(0.78, 0.55 + 0.2 * seed), 0.22);
                    d = max(d, dot(q, float2(0.7071, 0.7071)) - 0.72);
                    return max(d, dot(q, float2(-0.857, -0.514)) - (0.66 + 0.1 * seed));
                }
                else if (shape == SHAPE_NOTE)
                {
                    // A cartoon eighth note, centred in the square.
                    float2 p = q * 1.15 + float2(0.12, 0.45);
                    float2 h = Rotate(p, 0.35) / float2(0.3, 0.21);
                    float head = (length(h) - 1.0) * 0.21;
                    float stem = EFX_SdRoundBox(p - float2(0.24, 0.52), float2(0.06, 0.52), 0.04);
                    float flag = EFX_SdRoundBox(Rotate(p - float2(0.42, 0.88), 0.6), float2(0.2, 0.08), 0.06);
                    return min(head, min(stem, flag)) / 1.15;
                }
                else if (shape == SHAPE_LEAF)
                {
                    // A leaf: the lens between two circles, tips along y.
                    return max(length(q - float2(0.6, 0.0)), length(q + float2(0.6, 0.0))) - 0.95;
                }
                else if (shape == SHAPE_DASH)
                {
                    // A speed line: a capsule along y.
                    return length(float2(q.x, q.y - clamp(q.y, -0.78, 0.78))) - 0.19;
                }
                else if (shape == SHAPE_SNOWFLAKE)
                {
                    // Six arms, each with a pair of branches, round a small hub. Fold the plane into one arm's
                    // sixth (pointing up), mirrored, and draw that arm.
                    const float sector = 6.2831853 / 6.0;
                    float angle = atan2(q.x, q.y);
                    angle -= sector * round(angle / sector);
                    float r = length(q);
                    float2 f = r * float2(abs(sin(angle)), cos(angle));
                    float arm = length(float2(f.x, f.y - clamp(f.y, 0.0, 0.84))) - 0.09;
                    float2 root = f - float2(0.0, 0.48);
                    float2 outward = float2(0.7071, 0.7071);
                    float branch = length(root - outward * clamp(dot(root, outward), 0.0, 0.3)) - 0.075;
                    return min(min(arm, branch), r - 0.2);
                }

                return length(q) - 0.8;
            }

            half4 ParticleFragment(Varyings input) : SV_Target
            {
                int shape = (int)round(_Shape);
                float2 q = (input.uvAgeSeed.xy - 0.5) * 2.0;
            #if defined(_ALIGN_VELOCITY)
                // Turn the shape so its +y points along the flight. It has to fit the square's inscribed circle.
                float2 heading = normalize(input.heading);
                q = float2(dot(q, float2(heading.y, -heading.x)), dot(q, heading));
            #endif
                float age = input.uvAgeSeed.z;
                float seed = input.uvAgeSeed.w;

                float sdf = Shape(shape, q, age, seed);
                half3 body = input.color.rgb;

                // Edges are anti-aliased over one pixel of the particle's square, not over the field's own
                // gradient: a swirl's field jumps where it changes turns, and that would fringe.
                float px = max(fwidth(q.x), fwidth(q.y));

                // Shade: most shapes shade the crescent not covered by themselves nudged up-left, as if lit from
                // there; shards and leaves shade one face; strokes (rings, bolts, swirls, dashes) stay flat.
                bool stroke = shape == SHAPE_RING || shape == SHAPE_BOLT || shape == SHAPE_SWIRL || shape == SHAPE_DASH
                              || shape == SHAPE_SNOWFLAKE;
                bool faceted = shape == SHAPE_SHARD || shape == SHAPE_LEAF;
                half crescent = 1.0 - EFX_FillPx(Shape(shape, q + float2(0.2, -0.2), age, seed), px);
                half shade = stroke ? 0.0 : (faceted ? EFX_Step(0.0, q.x) : crescent);
                half3 rgb = lerp(body, body * _ShadeTone, shade) * _Emission;

                bool glossy = shape == SHAPE_BLOB || shape == SHAPE_PUFF || shape == SHAPE_COMET || shape == SHAPE_ROCK;
                half glint = EFX_Fill(length(q - float2(-0.34, 0.36)) - 0.14) * _HighlightDot * (glossy ? 1.0 : 0.0);
                rgb = lerp(rgb, max(rgb, 1.0) * 1.3, glint);
                half core = shape == SHAPE_BOLT ? EFX_FillPx(sdf + 0.08, px) : 0.0;   // a bolt's white-hot core
                rgb = lerp(rgb, max(rgb, 1.0) * 1.4, core);

                // Ink in the particle's own units, capped in pixels so a big ring isn't drawn in a fat marker.
                half ink = min(_InkWidth, _InkMaxPixels * px);
                half outline = (1.0 - EFX_FillPx(sdf + ink, px)) * step(1e-4, ink);
                half midrib = shape == SHAPE_LEAF ? (1.0 - EFX_Step(ink * 0.6, abs(q.x))) * step(abs(q.y), 0.6) : 0.0;
                rgb = lerp(rgb, body * _InkTone, max(outline, midrib));
                return half4(rgb, EFX_FillPx(sdf, px) * input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
