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
    // TEXCOORD0.xyzw: age thins rings, the random gives each bolt its jog, lean and girth (and mirrors half of
    // them), each swirl its length, each shard its girth, point and break, each dash its bow and each rock its
    // outline. Shapes that point along their flight (_ALIGN_VELOCITY: shards, bolts, comets, dashes) also need
    // Velocity, in TEXCOORD1: the quad stays facing the camera and the shape turns inside it. (Unity's own
    // velocity alignment turns the quad edge-on to a top-down camera.) Turned shards and bolts still light the
    // side facing the screen's upper left, like every other shape.
    //
    // Shards, bolts, swirls and dashes draw their own two tones and white-hot parts (see "Shapes drawn with
    // their own inner detail"). Their shaded side never brightens past a set ceiling however high the emission,
    // so pale colours keep a visible second tone. Their ink thins with the stroke, so a swirl's hook and a
    // dash's tail stay their colour rather than turning to ink. A dash's tail also dissolves behind it. The
    // other shapes take the generic shade crescent and the material's ink width unchanged.
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

        // Set per renderer by ArcBits on the gear's arc pieces only, so they sit back with the rest of the gear
        // (GearPresence.hlsl). 0 everywhere else.
        [HideInInspector] _PieceFadeOut ("Gear Fade Out", Range(0, 1)) = 0
        [HideInInspector] _PieceDesaturate ("Gear Desaturate", Range(0, 1)) = 0
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
            #include "GearPresence.hlsl"

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
                half _PieceFadeOut;
                half _PieceDesaturate;
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

            // The ink line's width in the particle's own units, capped in pixels so a big ring isn't drawn in a
            // fat marker.
            float InkWidth(float px)
            {
                return min(_InkWidth, _InkMaxPixels * px);
            }

            // ---------- Shapes drawn with their own inner detail ----------
            // Shards, bolts, swirls and dashes paint their own shade and white-hot parts rather than taking the
            // generic crescent. Each returns the signed distance (negative inside, in the particle's square) and
            // fills a ShapeDetail. Masks are already anti-aliased over `px`, one pixel in the square's units. They
            // take no derivatives, so they can sit inside a branch. `litSide` is +1 when the left of the flight
            // faces the light (the screen's upper left), -1 when the right does.
            struct ShapeDetail
            {
                half shade;         // 0 lit .. 1 shaded
                half hot;           // 0 .. 1 white-hot
                float inkLimit;     // the ink line is no wider than this here, so a thin stroke keeps its colour
                half alpha;         // 1, or less where the shape dissolves (a dash's tail)
            };

            // A cartoon ice shard flying point first along +y: a long crystal with a sharp point and a square,
            // slanted broken end, the face toward the light lit and the other shaded either side of the ridge,
            // with a straight white glint down the lit face. Each shard its own girth, point and break.
            float ShardShape(float2 q, float seed, float px, half litSide, inout ShapeDetail d)
            {
                float halfWidth = 0.27 + 0.08 * seed;                   // each shard its own girth...
                float shoulder = 0.12 + 0.18 * frac(seed * 7.31);       // ...where its point starts...
                float breakTilt = (frac(seed * 3.7) - 0.5) * 1.0;       // ...and how slanted its broken end is
                const float nose = 0.95;            // the point
                const float breakAt = -0.62;        // the broken end, across the middle
                const float glintWidth = 0.045;     // the glint's half-width
                float2 p = float2(abs(q.x), q.y);
                float2 frontNormal = normalize(float2(nose - shoulder, halfWidth));
                float2 breakNormal = normalize(float2(breakTilt, -1.0));
                float sdf = max(p.x - halfWidth, dot(p - float2(0.0, nose), frontNormal));
                sdf = max(sdf, dot(q - float2(0.0, breakAt), breakNormal));
                d.shade = EFX_FillPx(-q.x * litSide, px);
                // A straight glint down the middle of the lit face, its ends cut square to the point's edge and
                // to the break, like a facet catching the light.
                float glint = abs(q.x + litSide * halfWidth * 0.5) - glintWidth;
                glint = max(glint, dot(p - float2(0.0, nose - 0.3), frontNormal));
                glint = max(glint, dot(q - float2(0.0, breakAt + 0.22), breakNormal));
                d.hot = EFX_FillPx(glint, px);
                return sdf;
            }

            // Signed distance to a polygon edge, accumulated (iq's polygon): call once per edge, `a` the edge's
            // vertex and `b` the one before it; the polygon's distance is then parity * sqrt(nearest).
            void PolygonEdge(float2 p, float2 a, float2 b, inout float nearest, inout float parity)
            {
                float2 e = b - a;
                float2 w = p - a;
                float2 c = w - e * saturate(dot(w, e) / dot(e, e));
                nearest = min(nearest, dot(c, c));
                bool3 crossing = bool3(p.y >= a.y, p.y < b.y, e.x * w.y > e.y * w.x);
                if (all(crossing) || all(!crossing)) parity = -parity;
            }

            // A cartoon lightning bolt, point first along +y: a slanted slab, a jog, then a blade tapering to the
            // point. The side toward the light is lit and the other shaded, with a white-hot core down the seam
            // between that stops short of the point. Each bolt jogs at its own height, swings its point its own
            // way and has its own girth; half are mirrored.
            float BoltShape(float2 q, float seed, float px, half litSide, inout ShapeDetail d)
            {
                const float fit = 0.94;                                 // the glyph below is drawn a little big
                const float coreWidth = 0.042;                          // the white-hot core's half-width...
                const float coreStops = 0.7;                            // ...and how far up the blade it reaches
                float2 p = q / fit;
                bool mirrored = seed > 0.5;
                p.x = mirrored ? -p.x : p.x;
                float jog = (frac(seed * 13.7) - 0.5) * 0.4;            // the zig's height, up or down the bolt
                float lean = (frac(seed * 29.1) - 0.5) * 0.3;           // how far the point swings
                float girth = 1.0 + (frac(seed * 41.3) - 0.5) * 0.3;    // the back slab's width, +-15%
                // The outline, round from the back edge's left corner.
                float2 backLeft = float2(0.14 - 0.23 * girth, -0.92);
                float2 backRight = float2(0.14 + 0.23 * girth, -0.92);
                float2 notchRight = float2(0.106, -0.10 + jog);
                float2 jutRight = float2(0.405, -0.10 + jog);
                float2 tip = float2(-0.176 + lean, 0.95);
                float2 notchLeft = float2(-0.088, 0.12 + jog);
                float2 jutLeft = float2(-0.387, 0.12 + jog);
                float nearest = dot(p - backLeft, p - backLeft);
                float inside = 1.0;
                PolygonEdge(p, backLeft, jutLeft, nearest, inside);
                PolygonEdge(p, backRight, backLeft, nearest, inside);
                PolygonEdge(p, notchRight, backRight, nearest, inside);
                PolygonEdge(p, jutRight, notchRight, nearest, inside);
                PolygonEdge(p, tip, jutRight, nearest, inside);
                PolygonEdge(p, notchLeft, tip, nearest, inside);
                PolygonEdge(p, jutLeft, notchLeft, nearest, inside);
                float sdf = inside * sqrt(nearest) * fit;

                // The seam: up the middle of the slab, across the jog and up the middle of the blade to the point.
                float slabLeft = lerp(backLeft.x, jutLeft.x, (notchRight.y - backLeft.y) / (jutLeft.y - backLeft.y));
                float bladeRight = lerp(jutRight.x, tip.x, (notchLeft.y - jutRight.y) / (tip.y - jutRight.y));
                float2 seam0 = float2(0.14, -0.92);
                float2 seam1 = float2(0.5 * (slabLeft + notchRight.x), notchRight.y);
                float2 seam2 = float2(0.5 * (notchLeft.x + bladeRight), notchLeft.y);
                float2 from = p.y < seam1.y ? seam0 : (p.y < seam2.y ? seam1 : seam2);
                float2 to = p.y < seam1.y ? seam1 : (p.y < seam2.y ? seam2 : tip);
                float slope = (to.x - from.x) / (to.y - from.y);
                float across = (p.x - (from.x + (p.y - from.y) * slope)) * rsqrt(1.0 + slope * slope) * fit;
                across = mirrored ? -across : across;                   // now + is the right of the flight
                d.shade = EFX_FillPx(-across * litSide, px);
                float core = coreWidth * saturate((coreStops - p.y) / 0.45);
                d.hot = EFX_FillPx(abs(across) - core, px) * EFX_FillPx(sdf + InkWidth(px) + 0.04, px);
                return sdf;
            }

            // A cartoon gust curl: one brush stroke about a turn and a third long that sweeps wide on the outside
            // and tightens into a hook, fat at its outer end (with a little swell, as if the brush pressed down)
            // and tapering steadily to a point at the centre, its centre-facing side shaded. Each curl is its
            // own length.
            float SwirlShape(float2 q, float seed, float px, inout ShapeDetail d)
            {
                const float tau = 6.2831853;
                const float innerEnd = 2.0;         // the stroke runs from this angle (radians, its point)...
                float outerEnd = 10.1 + 0.9 * seed; // ...out to this one, its fat end
                const float outerRadius = 0.78;     // how far out the fat end sits
                const float tighten = 1.35;         // 1 = evenly spaced turns; more = a tighter hook inside
                const float fattest = 0.145;        // half-width at the fat end...
                const float press = 0.25;           // ...swelling by this share over the last fifth
                const float shadeFrom = 0.3;        // the inner shade stops this share of the half-width short of the middle
                float r = length(q);
                float theta = atan2(q.y, q.x);
                float turn = round((outerEnd * pow(max(r / outerRadius, 0.0), 1.0 / tighten) - theta) / tau);
                float innerRadius = outerRadius * pow(max(innerEnd / outerEnd, 0.0), tighten);
                float2 innerTip = innerRadius * float2(cos(innerEnd), sin(innerEnd));
                float2 outerTip = outerRadius * float2(cos(outerEnd), sin(outerEnd));
                float sdf = 1e4;
                float side = 1.0;
                float width = 0.0;
                [unroll] for (int i = -1; i <= 1; i++)
                {
                    float phi = theta + tau * (turn + i);
                    float onStroke = clamp(phi, innerEnd, outerEnd);
                    float along = (onStroke - innerEnd) / (outerEnd - innerEnd);
                    float halfWidth = fattest * pow(max(along, 0.0), 1.1) * (1.0 + press * smoothstep(0.8, 1.0, along));
                    float radius = outerRadius * pow(max(onStroke / outerEnd, 0.0), tighten);
                    float offset = r - radius;
                    // The spiral crosses each radius at a slant, so the gap along the radius overstates the distance.
                    float climb = tighten * radius / onStroke;
                    float gap = phi < innerEnd ? length(q - innerTip)
                              : (phi > outerEnd ? length(q - outerTip) : abs(offset) * radius * rsqrt(radius * radius + climb * climb));
                    float dist = gap - halfWidth;
                    if (dist < sdf) { sdf = dist; side = offset + shadeFrom * halfWidth; width = halfWidth; }
                }
                d.shade = EFX_FillPx(side, px);
                d.inkLimit = 0.32 * width;
                return sdf;
            }

            // Signed distance to an ellipse of semi-axes r about the origin (iq's approximation: exact enough
            // near the edge, which is all the anti-aliasing and ink need).
            float SdEllipse(float2 p, float2 r)
            {
                float k0 = length(p / r);
                float k1 = length(p / (r * r));
                return k0 * (k0 - 1.0) / max(k1, 1e-5);
            }

            // A speed streak flying along +y, in one flat colour: a blunt-pointed head that draws out into a long
            // taper, the tail dissolving to nothing behind. Each streak bows a little one way or the other.
            float DashShape(float2 q, float seed, float px, inout ShapeDetail d)
            {
                const float tailEnd = -0.96;        // the tail's point (+y is the flight)...
                const float fattestAt = 0.52;       // ...the head's widest point...
                const float noseEnd = 0.96;         // ...and the nose
                const float fattest = 0.13;         // the head's half-width
                const float fullness = 0.8;         // the taper's curve: 1 = straight sides, less = fuller for longer
                const float fadeFrom = -0.5;        // the tail is solid down to here, then dissolves to its point
                float bow = (seed - 0.5) * 0.2;     // how far the middle bends off the straight
                const float reach = 0.96;           // the bow is measured over -reach..reach
                // The centre line: a gentle parabola through the ends.
                float centre = bow * (1.0 - q.y * q.y / (reach * reach));
                float centreSlope = -2.0 * bow * q.y / (reach * reach);
                // The head: an oval from its widest point to the nose, turned to follow the bow.
                float headCentreX = bow * (1.0 - fattestAt * fattestAt / (reach * reach));
                float2 headAxis = normalize(float2(-2.0 * bow * fattestAt / (reach * reach), 1.0));
                float2 h = q - float2(headCentreX, fattestAt);
                float head = SdEllipse(float2(dot(h, float2(headAxis.y, -headAxis.x)), dot(h, headAxis)),
                                       float2(fattest, noseEnd - fattestAt));
                // The tail: sides tapering from the head's width to a point.
                float s = saturate((q.y - tailEnd) / (fattestAt - tailEnd));    // 0 the tail's point .. 1 the head
                float halfWidth = fattest * pow(s, fullness);
                float widthSlope = fattest * fullness * pow(max(s, 0.05), fullness - 1.0) / (fattestAt - tailEnd);
                float across = q.x - centre;
                float slope = sign(across) * centreSlope - widthSlope;
                float tailSd = (abs(across) - halfWidth) * rsqrt(1.0 + slope * slope);
                tailSd = max(tailSd, max(q.y - fattestAt, tailEnd - q.y));
                d.inkLimit = 0.3 * halfWidth;
                d.alpha = smoothstep(tailEnd, fadeFrom, q.y);
                return min(head, tailSd);
            }

            float DetailedShape(int shape, float2 q, float seed, float px, half litSide, out ShapeDetail d)
            {
                d.shade = 0.0;
                d.hot = 0.0;
                d.inkLimit = 1e4;
                d.alpha = 1.0;
                float sdf;
                [branch] if (shape == SHAPE_SHARD) sdf = ShardShape(q, seed, px, litSide, d);
                else if (shape == SHAPE_BOLT) sdf = BoltShape(q, seed, px, litSide, d);
                else if (shape == SHAPE_SWIRL) sdf = SwirlShape(q, seed, px, d);
                else sdf = DashShape(q, seed, px, d);
                return sdf;
            }

            // The other shapes, as a signed distance in the particle's square (-1..1 each way).
            float Shape(int shape, float2 q, float age, float seed)
            {
                [branch] if (shape == SHAPE_STAR)
                {
                    return EFX_SdStar4(q, 0.95);
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
                half litSide = 1.0;     // +1: the left of the flight faces the light (always so when not turned)
            #if defined(_ALIGN_VELOCITY)
                // Turn the shape so its +y points along the flight. It has to fit the square's inscribed circle.
                float2 heading = normalize(input.heading);
                q = float2(dot(q, float2(heading.y, -heading.x)), dot(q, heading));
                // Everything is lit from the screen's upper left, so a shard or bolt lights whichever side faces
                // there rather than always its left.
                litSide = dot(float2(-heading.y, heading.x), float2(-1.0, 1.0)) >= 0.0 ? 1.0 : -1.0;
            #endif
                float age = input.uvAgeSeed.z;
                float seed = input.uvAgeSeed.w;

                half3 body = input.color.rgb;

                // Edges are anti-aliased over one pixel of the particle's square, not over the field's own
                // gradient: a swirl's field jumps where it changes turns, and that would fringe.
                float px = max(fwidth(q.x), fwidth(q.y));
                half faceShade = EFX_Step(0.0, q.x);

                // Shade: shards, bolts, swirls and dashes paint their own (and their white-hot parts). Of the
                // rest, most shade the crescent not covered by themselves nudged up-left, as if lit from there;
                // leaves shade one face; rings and snowflakes stay flat.
                float sdf;
                half shade;
                half core;
                ShapeDetail detail;
                detail.shade = 0.0;
                detail.hot = 0.0;
                detail.inkLimit = 1e4;
                detail.alpha = 1.0;
                bool detailed = shape == SHAPE_SHARD || shape == SHAPE_BOLT || shape == SHAPE_SWIRL || shape == SHAPE_DASH;
                [branch] if (detailed)
                {
                    sdf = DetailedShape(shape, q, seed, px, litSide, detail);
                    shade = detail.shade;
                    core = detail.hot;
                }
                else
                {
                    sdf = Shape(shape, q, age, seed);
                    bool stroke = shape == SHAPE_RING || shape == SHAPE_SNOWFLAKE;
                    half crescent = 1.0 - EFX_FillPx(Shape(shape, q + float2(0.2, -0.2), age, seed), px);
                    shade = stroke ? 0.0 : (shape == SHAPE_LEAF ? faceShade : crescent);
                    core = 0.0;
                }
                half3 shadeColour = body * _ShadeTone;
                // A detailed shape's two tones are its whole look, so emission mustn't lift a pale colour's shaded
                // side past white, where it would clip to the same white as the lit side: its brightest channel
                // stays at or under DETAIL_SHADE_CEILING.
                const half DETAIL_SHADE_CEILING = 0.72;
                half shadeBrightest = max(max(shadeColour.r, shadeColour.g), max(shadeColour.b, 1e-3)) * _Emission;
                shadeColour *= detailed ? min(1.0, DETAIL_SHADE_CEILING / shadeBrightest) : 1.0;
                half3 rgb = lerp(body, shadeColour, shade) * _Emission;

                bool glossy = shape == SHAPE_BLOB || shape == SHAPE_PUFF || shape == SHAPE_COMET || shape == SHAPE_ROCK;
                half glint = EFX_Fill(length(q - float2(-0.34, 0.36)) - 0.14) * _HighlightDot * (glossy ? 1.0 : 0.0);
                rgb = lerp(rgb, max(rgb, 1.0) * 1.3, glint);
                // White-hot: a bolt's core, a shard's glint - kept to a gentle boost so bloom doesn't swallow them.
                rgb = lerp(rgb, max(rgb, 1.0) * 1.15, core);

                // A detailed shape's ink thins with its own stroke (a swirl's hook, a dash's tail) and fades out
                // once thinner than a pixel, so thin ends stay their colour instead of turning to ink.
                half ink = min(InkWidth(px), detail.inkLimit);
                half outline = (1.0 - EFX_FillPx(sdf + ink, px)) * step(1e-4, ink) * saturate(detail.inkLimit / px);
                half midrib = shape == SHAPE_LEAF ? (1.0 - EFX_Step(ink * 0.6, abs(q.x))) * step(abs(q.y), 0.6) : 0.0;
                rgb = lerp(rgb, body * _InkTone, max(outline, midrib));
                return GearRecede(half4(rgb, EFX_FillPx(sdf, px) * input.color.a * detail.alpha), _PieceFadeOut, _PieceDesaturate);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
