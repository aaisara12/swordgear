Shader "Swordgear/CatchRingBurst"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorCore ("Color Core", Color) = (1, 1, 1, 1)
        _ColorMid ("Color Mid", Color) = (0.5, 0.95, 1, 1)
        _ColorOuter ("Color Outer", Color) = (0.1, 0.35, 0.85, 1)
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _MaxRadius ("Max Radius", Range(0, 1)) = 0.95
        _RingWidth ("Ring Width", Range(0.001, 0.3)) = 0.08
        _Softness ("Softness", Range(0, 0.2)) = 0.02
        _EchoOffset ("Echo Offset", Range(0, 0.5)) = 0.12
        _EchoIntensity ("Echo Intensity", Range(0, 1)) = 0.35
        _FadeStart ("Fade Start", Range(0, 1)) = 0.55
        _Intensity ("Intensity", Range(0, 5)) = 1.75

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

        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex RingVertex
            #pragma fragment RingFragment
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorCore;
                half4 _ColorMid;
                half4 _ColorOuter;
                half4 _TintColor;
                half _Progress;
                half _MaxRadius;
                half _RingWidth;
                half _Softness;
                half _EchoOffset;
                half _EchoIntensity;
                half _FadeStart;
                half _Intensity;
            CBUFFER_END

            half RingBand(half dist, half width, half softness)
            {
                half inner = smoothstep(0, softness, dist);
                half outer = 1.0 - smoothstep(width, width + softness, dist);
                return inner * outer;
            }

            Varyings RingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 RingFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half2 centered = input.uv - 0.5;
                half dist = length(centered) * 2.0;

                half radius = _Progress * _MaxRadius;
                half primaryDist = abs(dist - radius);
                half primaryBand = RingBand(primaryDist, _RingWidth, _Softness);

                half echoRadius = max(radius - _EchoOffset, 0.0);
                half echoDist = abs(dist - echoRadius);
                half echoBand = RingBand(echoDist, _RingWidth * 0.85, _Softness) * _EchoIntensity;

                half fade = 1.0 - smoothstep(_FadeStart, 1.0, _Progress);
                half alpha = (primaryBand + echoBand) * fade * _Intensity * tex.a * input.color.a;

                half3 palette = lerp(_ColorCore.rgb, _ColorMid.rgb, saturate(_Progress * 1.5));
                palette = lerp(palette, _ColorOuter.rgb, saturate((_Progress - 0.35) * 1.5));
                half3 rgb = palette * _TintColor.rgb * alpha;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
