Shader "SwordGear/EnemySpriteOutline"
{
    // Renders a dilated silhouette of a sprite as a lit 2D surface. Sits on a child renderer BEHIND the
    // enemy sprite, so only the expanded rim shows and the body keeps its own material. Goes through the
    // URP 2D shape-light path so the rim sits under the same lighting as everything else, and blends
    // toward the renderer colour so it picks up the enemy's element tint.
    // Requires a FullRect sprite with transparent padding — a Tight mesh crops the dilation.
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        _OutlineColor("Outline Color", Color) = (0.58, 0.57, 0.52, 1)
        _OutlineWidth("Outline Width (texels)", Float) = 17
        _TintAmount("Tint Toward Enemy Color", Range(0, 1)) = 0.35

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _TintAmount;
            CBUFFER_END

            // 16 taps around a circle — enough that the dilated rim reads round rather than polygonal.
            static const float2 kTaps[16] =
            {
                float2( 1.000,  0.000), float2( 0.924,  0.383), float2( 0.707,  0.707), float2( 0.383,  0.924),
                float2( 0.000,  1.000), float2(-0.383,  0.924), float2(-0.707,  0.707), float2(-0.924,  0.383),
                float2(-1.000,  0.000), float2(-0.924, -0.383), float2(-0.707, -0.707), float2(-0.383, -0.924),
                float2( 0.000, -1.000), float2( 0.383, -0.924), float2( 0.707, -0.707), float2( 0.924, -0.383)
            };

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;

                half dilated = 0;
                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    dilated = max(dilated, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + kTaps[i] * offset).a);
                }

                half3 rim = lerp(_OutlineColor.rgb, input.color.rgb, _TintAmount);
                half alpha = dilated * _OutlineColor.a * input.color.a;

                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(rim, alpha, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

#if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
#endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
