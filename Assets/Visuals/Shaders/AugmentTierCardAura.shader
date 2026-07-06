Shader "Swordgear/UI/AugmentTierCardAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Aura Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 6)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 1.6
        _TimeOffset ("Time Offset", Float) = 0
        _RingWidth ("Ring Width", Range(0.01, 0.3)) = 0.12
        _Softness ("Softness", Range(0.01, 0.3)) = 0.08

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
            Name "Aura"
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
            float _TimeOffset;
            float _RingWidth;
            float _Softness;

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
                float pulse = sin((_Time.y + _TimeOffset) * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(0.72, 1.0, pulse);

                float2 centered = abs(uv - 0.5) * 2.0;
                float edge = max(centered.x, centered.y);

                float inner = 1.0 - _RingWidth;
                float outerRing = smoothstep(inner - _Softness, inner, edge)
                    * (1.0 - smoothstep(1.0, 1.0 + _Softness * 2.5, edge));
                float outerBloom = smoothstep(0.72, 0.95, edge) * (1.0 - smoothstep(0.95, 1.08, edge));
                float cornerBoost = pow(saturate((edge - 0.82) / 0.18), 1.4);

                float aura = max(outerRing, outerBloom * 0.65) + cornerBoost * 0.45;
                aura *= tex.a;

                half3 glow = _Color.rgb * aura * _Intensity * pulse;
                glow *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);

                return half4(glow, aura * _Color.a);
            }
            ENDCG
        }
    }
}
