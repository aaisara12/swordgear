Shader "Swordgear/Gear Arc"
{
    // The plain gear arc: the element's colour with glowing edges, swelling and brightening (into HDR, so
    // it blooms) when aimed at or active. The fallback for any element without a shader of its own, and
    // the template the element arc shaders follow.
    Properties
    {
        [Header(State response)]
        _Swell ("Swell When Aimed (world units)", Range(0, 1)) = 0.35
        _HighlightBoost ("Brightness When Aimed", Range(0, 4)) = 1.2
        _ActiveBoost ("Brightness When Active", Range(0, 4)) = 0.8
        _RimWidth ("Rim Width (0..0.5 of the band)", Range(0.01, 0.5)) = 0.12
        _RimGlow ("Rim Glow", Range(0, 4)) = 1.2

        [HideInInspector] _Highlight ("Highlight", Range(0, 1)) = 0
        [HideInInspector] _Active ("Active", Range(0, 1)) = 0
        [HideInInspector] _Fill ("Fill", Range(0, 1)) = 1
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
            #pragma fragment ArcFragment

            #include "GearArcCommon.hlsl"

            half4 ArcFragment(ArcVaryings input) : SV_Target
            {
                half3 rgb = input.color.rgb * (1.0 + ArcRim(input.uv.y)) * ArcStateGlow();
                return half4(rgb, input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
