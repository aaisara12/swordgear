Shader "Swordgear/Element Vignette"
{
    // A faint tint at the screen's edges in the imbued element's colour: soft and still, so it tells the player
    // which element they're in without pulling their eye. The element's own particles along the edges (one
    // prefab per element, Assets/Visuals/Prefabs/ElementFX/Border/) carry its identity. On a switch the tint
    // deepens briefly and settles back.
    //
    // Drawn on a full-screen UI image in the arena's Screen Space - Camera canvas, which ElementVignette only
    // enables while an element is imbued. It's driven through globals: _ElementVignetteColor,
    // _ElementVignetteHold (1 while imbued) and _ElementVignetteFlareTime (when it last flared, on the clock
    // URP feeds _Time.y); the flare plays out here, so nothing tweens it.
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [Header(Held edge)]
        _HoldDepth ("Depth (screen heights)", Range(0, 0.15)) = 0.045
        _HoldAlpha ("Opacity At The Edge", Range(0, 1)) = 0.22
        _Falloff ("Falloff (1 = linear, higher = hugs the edge)", Range(0.5, 4)) = 1.8
        [Header(Switch flare)]
        _FlareDepth ("Extra Depth (screen heights)", Range(0, 0.3)) = 0.05
        _FlareAlpha ("Extra Opacity", Range(0, 1)) = 0.3
        _FlareDecay ("Decay (per second)", Range(1, 12)) = 6
        _Glow ("Glow (HDR)", Range(0, 2)) = 0.2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "False" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VignetteVertex
            #pragma fragment VignetteFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _HoldDepth;
                half _HoldAlpha;
                half _Falloff;
                float _FlareDepth;
                half _FlareAlpha;
                float _FlareDecay;
                half _Glow;
            CBUFFER_END

            // Set by ElementVignette.
            half4 _ElementVignetteColor;
            half _ElementVignetteHold;
            float _ElementVignetteFlareTime;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings VignetteVertex(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 VignetteFragment(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;

                // Distance to the nearest screen edge, in screen heights. The corners round off slightly, so
                // they don't read as hard diagonal seams.
                float2 uv = input.uv;
                float2 toEdge = float2(min(uv.x, 1.0 - uv.x) * aspect, min(uv.y, 1.0 - uv.y));
                const float corner = 0.04;
                float2 c = max(corner - toEdge, 0.0);
                float d = min(toEdge.x, toEdge.y) - (length(c) - max(c.x, c.y));

                float since = _Time.y - _ElementVignetteFlareTime;
                half flare = since < 0.0 ? 0.0 : exp(-since * _FlareDecay);

                float depth = _HoldDepth + flare * _FlareDepth;
                half fade = pow(saturate(1.0 - d / max(depth, 1e-4)), _Falloff);
                half alpha = (_HoldAlpha + flare * _FlareAlpha) * fade * _ElementVignetteHold;
                half3 rgb = _ElementVignetteColor.rgb * (1.0 + flare * _Glow);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
