Shader "Swordgear/Gear Arc"
{
    // The plain gear arc: the shared cartoon tile in the element's colour (GearArcCommon's ArcNeutral),
    // swelling, brightening into HDR and taking a glowing outline when aimed at or active. The fallback for
    // any element without a shader of its own, and the template the element arc shaders follow.
    Properties
    {
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
            #pragma fragment ArcFragment

            #include "GearArcCommon.hlsl"

            half4 ArcFragment(ArcVaryings input) : SV_Target
            {
                ArcFrame f = ArcGetFrame(input);
                return EFX_Over(ArcNeutral(input, f), ArcHalo(f, input.color.rgb));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
