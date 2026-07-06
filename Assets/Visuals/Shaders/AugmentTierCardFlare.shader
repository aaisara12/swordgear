Shader "Swordgear/UI/AugmentTierCardFlare"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Flare Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1.2
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 2.8
        _MotionSpeed ("Motion Speed", Range(0, 4)) = 1.35
        _TimeOffset ("Time Offset", Float) = 0
        _HotspotY ("Hotspot Y", Range(0, 1)) = 0.16
        _Spread ("Spread", Range(0.2, 3)) = 1.35

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Flare"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Intensity;
            float _PulseSpeed;
            float _MotionSpeed;
            float _TimeOffset;
            float _HotspotY;
            float _Spread;

            v2f vert(appdata_t v)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = v.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                output.color = v.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half4 tex = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float2 uv = input.texcoord;
                float t = _TimeOffset;
                float pulse = sin(t * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(0.78, 1.05, pulse);

                float driftX = sin(t * _MotionSpeed) * 0.1 + sin(t * _MotionSpeed * 1.7 + 1.2) * 0.045;
                float driftY = sin(t * _MotionSpeed * 0.9 + 0.4) * 0.015;
                float2 hotspot = float2(0.5 + driftX, _HotspotY + driftY);

                // Wide horizontal ellipse — no upward rise gradient, no band mask
                float2 d = (uv - hotspot) * float2(0.42 / _Spread, 1.35);
                float dist = length(d);

                float2 hotspot2 = float2(0.5 - driftX * 0.55, _HotspotY + driftY * 0.65);
                float dist2 = length((uv - hotspot2) * float2(0.48 / _Spread, 1.45));

                float core = exp(-dist * dist * 3.2);
                float core2 = exp(-dist2 * dist2 * 4.0) * 0.3;
                float wash = exp(-dist * 1.25) * 0.45;

                float shimmer = sin(uv.x * 16.0 + t * 3.2) * 0.5 + 0.5;
                shimmer *= sin(uv.x * 20.0 - t * 2.1) * 0.5 + 0.5;
                shimmer = lerp(0.86, 1.04, shimmer);

                float flare = (core * 0.48 + core2 + wash) * shimmer;
                flare *= tex.a;

                half3 glow = _Color.rgb * flare * _Intensity * pulse;
                glow *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);

                return half4(glow, flare * _Color.a);
            }
            ENDCG
        }
    }
}
