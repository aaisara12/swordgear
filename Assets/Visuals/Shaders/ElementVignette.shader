Shader "Swordgear/Element Vignette"
{
    // The screen's edges in the imbued element: a cartoon border, inked along its inner edge, whose edge takes
    // the element's shape — flame tongues licking in, icicle teeth, a zig-zag crackling, waves rolling,
    // stepped rock, drippy blobs, pastel scallops. On a switch it flares deep into the screen for half a second,
    // then settles to a thin edge that holds while the imbue lasts.
    //
    // Drawn on a full-screen UI image in a Screen Space - Camera canvas (so it sits under the HUD and blooms
    // with the scene). ElementVignette drives it through globals: _ElementVignetteColor, _ElementVignetteStyle
    // (the Element as a number), _ElementVignetteHold (0..1, the held edge) and _ElementVignetteFlareTime (when
    // it last flared, in _Time.y); the flare plays out here, so nothing tweens it.
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _HoldDepth ("Held Edge Depth (screen heights)", Range(0, 0.1)) = 0.03
        _FlareDepth ("Flare Depth (screen heights)", Range(0, 0.4)) = 0.13
        _FlareDecay ("Flare Decay (per second)", Range(1, 12)) = 4.5
        _HoldAlpha ("Held Edge Opacity", Range(0, 1)) = 0.6
        _Glow ("Flare Glow (HDR)", Range(0, 4)) = 0.8
        _InkWidth ("Ink Width (screen heights)", Range(0, 0.02)) = 0.005
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
            #include "ElementFX.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _HoldDepth;
                float _FlareDepth;
                float _FlareDecay;
                half _HoldAlpha;
                half _Glow;
                float _InkWidth;
            CBUFFER_END

            // Set by ElementVignette.
            half4 _ElementVignetteColor;
            float _ElementVignetteStyle;
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

            // Pastel rainbow, as in Opalite.shader.
            half3 Pastel(float t)
            {
                return 0.78 + 0.25 * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            // How far the border reaches at s (along the edge, screen heights), 0..1, in the element's shape.
            float Motif(int style, float s, float t)
            {
                [branch] if (style == 1)   // Fire: tongues flickering to random heights
                {
                    float u = s / 0.11 + sin(t * 3.0 + s * 9.0) * 0.15;
                    float id = floor(u);
                    float tick = floor(t * 7.0 + EFX_Hash21(float2(id, 1.0)) * 5.0);
                    float height = lerp(0.35, 1.0, EFX_Hash21(float2(id, tick)));
                    return pow(saturate(1.0 - abs(frac(u) * 2.0 - 1.0)), 0.6) * height;
                }
                else if (style == 2)   // Ice: jagged teeth, still
                {
                    float u = s / 0.09;
                    return (1.0 - abs(frac(u) * 2.0 - 1.0)) * lerp(0.4, 1.0, EFX_Hash21(float2(floor(u), 2.0)));
                }
                else if (style == 3)   // Lightning: a zig-zag re-kinked many times a second
                {
                    float u = s / 0.07;
                    float k = floor(u);
                    float tick = floor(t * 12.0);
                    return lerp(EFX_Hash21(float2(k, tick)), EFX_Hash21(float2(k + 1.0, tick)), frac(u));
                }
                else if (style == 4)   // Wind: waves rolling along
                {
                    return 0.5 + 0.5 * sin(s * 38.0 - t * 9.0 + sin(s * 11.0 + t * 2.0));
                }
                else if (style == 5)   // Earth: stepped blocks
                {
                    return lerp(0.3, 1.0, EFX_Hash21(float2(floor(s / 0.08), 5.0)));
                }
                else if (style == 6)   // Dark: drippy blobs, wobbling
                {
                    float u = s / 0.1;
                    float id = floor(u);
                    float x = frac(u) * 2.0 - 1.0;
                    return sqrt(saturate(1.0 - x * x)) * (0.55 + 0.45 * sin(t * 2.0 + id * 2.3));
                }
                else if (style == 7)   // Light: even scallops
                {
                    float x = frac(s / 0.09) * 2.0 - 1.0;
                    return sqrt(saturate(1.0 - x * x));
                }
                return 0.0;
            }

            half4 VignetteFragment(Varyings input) : SV_Target
            {
                int style = (int)round(_ElementVignetteStyle);
                float t = _Time.y;
                float aspect = _ScreenParams.x / _ScreenParams.y;

                // Distance to the nearest screen edge and position along it, in screen heights.
                float2 uv = input.uv;
                float dx = min(uv.x, 1.0 - uv.x) * aspect;
                float dy = min(uv.y, 1.0 - uv.y);
                float d = min(dx, dy);
                float s = dx < dy ? uv.y : uv.x * aspect;

                float since = t - _ElementVignetteFlareTime;
                half flare = since < 0.0 ? 0.0 : exp(-since * _FlareDecay);
                float depth = _ElementVignetteHold * _HoldDepth + flare * _FlareDepth;
                float edge = depth * (0.5 + 0.5 * Motif(style, s, t));

                float field = d - edge;   // negative inside the border
                float px = length(float2(ddx(d), ddy(d)));
                half cover = EFX_FillPx(field, px) * step(1e-4, depth) * step(0.5, style);

                // Cartoon: a darker outer band, the element's colour, an ink line along the inner edge.
                half3 body = _ElementVignetteColor.rgb;
                if (style == 7)
                {
                    body = Pastel(floor(s / 0.09) * 0.21);   // Light's scallops each their own pastel
                }
                half3 rgb = lerp(body, body * 0.7, EFX_FillPx(d - edge * 0.45, px));
                rgb *= 1.0 + flare * _Glow;
                half ink = 1.0 - EFX_FillPx(field + _InkWidth, px);
                rgb = lerp(rgb, body * 0.22, ink);

                half alpha = lerp(_HoldAlpha, 0.92, saturate(flare * 2.0));
                return half4(rgb, cover * alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
