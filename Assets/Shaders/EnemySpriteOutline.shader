Shader "SwordGear/EnemySpriteOutline"
{
    // Renders a dilated silhouette of a sprite in a solid colour. Sits on a child renderer BEHIND the
    // enemy sprite, so only the expanded rim shows and the base sprite keeps its lit material and
    // per-element tint. Requires a FullRect sprite with transparent padding — a Tight mesh crops the
    // dilation at the silhouette edge. URP 2D renderer.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0.04, 0.03, 0.07, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 24
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            // 16 taps around a circle — enough that the dilated rim reads round rather than polygonal.
            static const float2 kTaps[16] =
            {
                float2( 1.000,  0.000), float2( 0.924,  0.383), float2( 0.707,  0.707), float2( 0.383,  0.924),
                float2( 0.000,  1.000), float2(-0.383,  0.924), float2(-0.707,  0.707), float2(-0.924,  0.383),
                float2(-1.000,  0.000), float2(-0.924, -0.383), float2(-0.707, -0.707), float2(-0.383, -0.924),
                float2( 0.000, -1.000), float2( 0.383, -0.924), float2( 0.707, -0.707), float2( 0.924, -0.383)
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;

                half dilated = 0;
                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    float2 uv = IN.uv + kTaps[i] * offset;
                    dilated = max(dilated, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a);
                }

                // Only alpha comes from the renderer, so element tint never colours the outline.
                return half4(_OutlineColor.rgb, dilated * _OutlineColor.a * IN.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
