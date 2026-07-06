Shader "Swordgear/UI/AugmentTierCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 0.5)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _GlowStrength ("Edge Glow", Range(0, 3)) = 0.65
        _SweepStrength ("Sweep Strength", Range(0, 2)) = 0
        _SweepSpeed ("Sweep Speed", Range(0, 2)) = 0.25
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.35
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 2.2
        _SparkleStrength ("Sparkle Strength", Range(0, 1.5)) = 0
        _SparkleSpeed ("Sparkle Speed", Range(0, 8)) = 2.5
        _SparkleDensity ("Sparkle Density", Range(0, 1)) = 0.5
        _MetallicNoise ("Metallic Noise", Range(0, 1)) = 0.25
        _TimeOffset ("Time Offset", Float) = 0
        _RimInner ("Rim Inner Edge", Range(0.15, 0.75)) = 0.34
        _RimPower ("Rim Power", Range(0.3, 4)) = 0.65

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
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
            fixed4 _HighlightColor;
            fixed4 _ShadowColor;
            float _GlowStrength;
            float _SweepStrength;
            float _SweepSpeed;
            float _PulseStrength;
            float _PulseSpeed;
            float _SparkleStrength;
            float _SparkleSpeed;
            float _SparkleDensity;
            float _MetallicNoise;
            float _TimeOffset;
            float _RimInner;
            float _RimPower;

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float SparklePoint(float2 p, float scale)
            {
                float r = length(p);
                float core = exp(-r * r * scale * scale);

                float angle = atan2(p.y, p.x);
                float rays = pow(abs(sin(angle * 2.0 + 0.785398)), 24.0);
                float rayMask = exp(-r * 14.0);
                float softRays = rays * rayMask;

                float diag = pow(abs(sin(angle * 4.0)), 32.0) * exp(-r * 20.0) * 0.35;
                return core + softRays + diag;
            }

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
                float t = _Time.y + _TimeOffset;
                float pulse = lerp(1.0, sin(t * _PulseSpeed) * 0.5 + 0.5, _PulseStrength);

                float depth = smoothstep(0.02, 0.98, uv.y);
                half3 color = lerp(_ShadowColor.rgb, _Color.rgb, depth * 0.78 + 0.22);

                float topGlow = pow(saturate(1.0 - uv.y), 1.8) * _HighlightColor.a * 1.35;
                color = lerp(color, _HighlightColor.rgb, saturate(topGlow));

                float sweep = 0.0;
                if (_SweepStrength > 0.001)
                {
                    float sweepPos = frac(t * _SweepSpeed) * 1.45 - 0.22;
                    float sweepLine = abs(uv.x + uv.y * 0.55 - sweepPos);
                    sweep = smoothstep(0.05, 0.0, sweepLine);
                    sweep *= smoothstep(0.0, 0.06, uv.y) * smoothstep(1.0, 0.06, uv.y);
                    color += _HighlightColor.rgb * sweep * _SweepStrength;
                }

                float2 centered = abs(uv - 0.5) * 2.0;
                float edge = max(centered.x, centered.y);
                float rim = smoothstep(_RimInner, 1.0, edge);
                rim = pow(rim, _RimPower);
                color += _HighlightColor.rgb * rim * _GlowStrength * 1.6 * pulse;

                if (_MetallicNoise > 0.001)
                {
                    float2 noiseUv = uv * float2(96.0, 72.0);
                    float noise = Hash21(floor(noiseUv)) * 0.5 + Hash21(floor(noiseUv * 1.7 + 3.1)) * 0.5;
                    color += (_HighlightColor.rgb - color) * (noise - 0.5) * _MetallicNoise * 0.35;
                }

                if (_SparkleStrength > 0.001)
                {
                    float sparkleT = t * _SparkleSpeed;
                    float density = lerp(0.88, 0.68, _SparkleDensity);
                    float2 grid = uv * lerp(16.0, 28.0, _SparkleDensity);
                    float2 cell = floor(grid);
                    float2 fracCell = frac(grid) - 0.5;
                    float n = Hash21(cell);
                    float spawn = step(density, n);
                    float twinkle = sin(sparkleT + n * 6.28318) * 0.5 + 0.5;
                    twinkle = pow(twinkle, 3.0);
                    float sparkle = SparklePoint(fracCell, 55.0) * spawn * twinkle;
                    color += half3(1.0, 1.0, 1.0) * sparkle * _SparkleStrength * pulse;
                }

                half4 result = half4(color, tex.a * _Color.a);
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                return result;
            }
            ENDCG
        }
    }
}
