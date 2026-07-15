Shader "Swordgear/DashIndicatorFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.72, 0.72, 0.74, 0.55)
        _FlowColor ("Flow Color", Color) = (0.95, 0.95, 0.97, 1.0)
        _AccentColor ("Accent Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FlowSpeed ("Flow Speed", Range(0.1, 8)) = 2.4
        _BandCount ("Band Count", Range(1, 12)) = 4
        _BandWidth ("Band Width", Range(0.02, 0.4)) = 0.14
        _BandSoftness ("Band Softness", Range(0.005, 0.2)) = 0.05
        _TrailStrength ("Trail Strength", Range(0, 1)) = 0.55
        _EdgeFeather ("Edge Feather", Range(0, 0.5)) = 0.18
        _BaseOpacity ("Base Opacity", Range(0, 1)) = 0.55
        _Intensity ("Intensity", Range(0, 4)) = 1.55

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
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex FlowVertex
            #pragma fragment FlowFragment
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
                half4 _BaseColor;
                half4 _FlowColor;
                half4 _AccentColor;
                half _FlowSpeed;
                half _BandCount;
                half _BandWidth;
                half _BandSoftness;
                half _TrailStrength;
                half _EdgeFeather;
                half _BaseOpacity;
                half _Intensity;
            CBUFFER_END

            half SoftBand(half t, half width, half softness)
            {
                // Rising edge stays sharp; falling edge trails soft for a sliding look.
                half leading = smoothstep(0.0, softness, t);
                half trailing = 1.0 - smoothstep(width, width + softness + _TrailStrength * width, t);
                return leading * trailing;
            }

            Varyings FlowVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 FlowFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half2 uv = input.uv;

                // Length axis: player (near) at uv.y = 0 → tip at uv.y = 1.
                // Use uv.y only so bands stay perpendicular to the indicator borders.
                half along = uv.y;

                half sideMask = 1.0;
                if (_EdgeFeather > 0.0001)
                {
                    half edge = min(uv.x, 1.0 - uv.x);
                    sideMask = smoothstep(0.0, _EdgeFeather, edge);
                }

                half tipFade = 1.0 - smoothstep(0.82, 1.0, uv.y);
                half rootBoost = lerp(0.75, 1.0, smoothstep(0.0, 0.18, uv.y));

                half bands = max(_BandCount, 1.0);
                half phase = frac(along * bands - _Time.y * _FlowSpeed);
                // Remap so the bright front travels toward the tip.
                half travel = 1.0 - phase;
                half band = SoftBand(travel, _BandWidth, _BandSoftness);

                // Secondary thinner accent band for shimmer.
                half phase2 = frac(along * (bands * 0.5) - _Time.y * (_FlowSpeed * 1.35) + 0.37);
                half accent = SoftBand(1.0 - phase2, _BandWidth * 0.45, _BandSoftness * 0.7) * 0.55;

                half flowMask = saturate(band + accent);
                half baseAlpha = _BaseOpacity * _BaseColor.a;
                half flowAlpha = flowMask * _FlowColor.a;
                half accentAlpha = accent * _AccentColor.a;

                half3 baseRgb = _BaseColor.rgb;
                half3 flowRgb = lerp(_FlowColor.rgb, _AccentColor.rgb, accent);
                half3 rgb = lerp(baseRgb, flowRgb, saturate(flowMask));

                half alpha = (baseAlpha + flowAlpha * (0.65 + accentAlpha * 0.35)) * sideMask * tipFade * rootBoost;
                alpha *= tex.a * input.color.a * _Intensity;
                rgb *= input.color.rgb;

                return half4(rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
