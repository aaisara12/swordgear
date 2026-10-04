Shader "Swordgear/Opalite"
{
    // Light's element colour. Opal is a milky white whose colour lives in a moving sheen and in small
    // "play-of-colour" flashes, so neither can be a flat tint: both are computed here per pixel.
    //
    // The sheen is driven by WORLD position + time rather than UVs, for two reasons: the gear-ring arc is a
    // mesh with no UVs, and a world-space sheen moving across a note as it flies reads as light catching a
    // stone rather than a texture scrolling over a sprite.
    //
    // Texture alpha and vertex/sprite colour are still multiplied in, so element tints, the gear ring's
    // highlight logic and any alpha fades keep working. Blend is exposed so one shader serves alpha-blended
    // notes and additive trails/glows.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Body)]
        _MilkColor ("Milk Colour", Color) = (0.95, 0.93, 0.98, 1)

        [Header(Sheen)]
        _SheenStrength ("Sheen Strength", Range(0, 1)) = 0.55
        _SheenScale ("Sheen Scale (bands per unit)", Range(0.05, 4)) = 0.45
        _SheenSpeed ("Sheen Speed", Range(0, 2)) = 0.18
        _SheenAngle ("Sheen Angle (degrees)", Range(0, 360)) = 35
        _Swirl ("Swirl", Range(0, 1.5)) = 0.45

        [Header(Play of colour)]
        _FleckScale ("Fleck Scale", Range(0.5, 20)) = 5.5
        _FleckThreshold ("Fleck Threshold", Range(0, 1)) = 0.78
        _FleckIntensity ("Fleck Intensity", Range(0, 3)) = 1.1
        _FleckTwinkle ("Fleck Twinkle Speed", Range(0, 3)) = 0.7

        [Header(Output)]
        _Intensity ("Intensity", Range(0, 3)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5   // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10  // OneMinusSrcAlpha

        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OpalVertex
            #pragma fragment OpalFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 opalPos : TEXCOORD3;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _MilkColor;
                half _SheenStrength;
                half _SheenScale;
                half _SheenSpeed;
                half _SheenAngle;
                half _Swirl;
                half _FleckScale;
                half _FleckThreshold;
                half _FleckIntensity;
                half _FleckTwinkle;
                half _Intensity;
            CBUFFER_END

            // Pastel rainbow: a cosine palette with a high floor and a small swing, which is what keeps the
            // sheen opalescent rather than neon. Phase offsets spread R, G and B a third of a turn apart.
            half3 Pastel(float t, half swing)
            {
                return 0.78 + swing * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings OpalVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.opalPos = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            half4 OpalFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 p = input.opalPos;
                float t = _Time.y;

                // Sheen: bands along a slowly drifting direction, bent by a gentle swirl so they never read as
                // straight stripes.
                float angle = radians(_SheenAngle) + t * 0.05;
                float2 dir = float2(cos(angle), sin(angle));
                float swirl = sin(p.x * 1.7 + t * 0.6) * cos(p.y * 1.3 - t * 0.4) * _Swirl;
                float s = dot(p, dir) * _SheenScale + t * _SheenSpeed + swirl;

                half3 sheen = Pastel(s, 0.22);
                half sheenMix = _SheenStrength * (0.55 + 0.45 * sin(s * 6.2831853 * 0.5 + 1.3));
                half3 body = lerp(_MilkColor.rgb, sheen, saturate(sheenMix));

                // Play of colour: sparse, more saturated flashes that drift and twinkle, the signature of opal.
                float n = ValueNoise(p * _FleckScale + float2(t * 0.11, -t * 0.07));
                half twinkle = 0.6 + 0.4 * sin(t * _FleckTwinkle * 6.2831853 + n * 12.0);
                half fleck = smoothstep(_FleckThreshold, 1.0, n) * twinkle;
                half3 fleckColour = Pastel(s * 1.7 + n * 2.0 + 0.25, 0.45);
                body += fleckColour * fleck * _FleckIntensity;

                half alpha = tex.a * input.color.a;
                half3 rgb = body * tex.rgb * input.color.rgb * _Intensity;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
