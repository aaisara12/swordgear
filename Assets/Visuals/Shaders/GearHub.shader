Shader "Swordgear/Gear Hub"
{
    // The cog behind the gear's arcs: a cartoon steel band just wider than the arcs, one chunky tooth in each
    // gap between them, rivets in the gaps, all in ink. While an element is imbued the tooth caps and the
    // band's inner rim glow in its colour. On every grant the cog clicks round one notch with a springy
    // overshoot and its teeth flash — the arcs themselves never move, so flick directions stay learnable.
    //
    // Drawn on a quad centred on the gear. GearManager sets the shape (_HubShape: the arcs' inner and outer
    // radius, the tooth count, the first arc's angle), the tint, and the click (_Notch, _ClickTime); the click
    // itself animates here from _ClickTime, so no script tweens it.
    Properties
    {
        [Header(Steel)]
        _SteelColor ("Steel", Color) = (0.36, 0.4, 0.52, 1)
        _BevelColor ("Bevel", Color) = (0.56, 0.61, 0.74, 1)
        _ShadeColor ("Shade", Color) = (0.23, 0.26, 0.35, 1)
        _InkColor ("Ink", Color) = (0.05, 0.06, 0.1, 1)
        _InkWidth ("Ink Width (world units)", Range(0.05, 0.4)) = 0.16

        [Header(Shape)]
        _BandMargin ("Band Past The Arcs (world units)", Range(0, 1.5)) = 0.55
        _ToothLength ("Tooth Length (world units)", Range(0.2, 2.5)) = 1.05
        _ToothRoot ("Tooth Width At Root (world units)", Range(0.3, 3)) = 2.1
        _ToothTip ("Tooth Width At Tip (world units)", Range(0.2, 3)) = 1.4
        _RivetRadius ("Rivet Radius (world units)", Range(0, 0.5)) = 0.2

        [Header(Glow and click)]
        _GlowIntensity ("Element Glow (HDR)", Range(1, 6)) = 3
        _ClickStiffness ("Click Stiffness", Range(5, 60)) = 30
        _ClickDamping ("Click Damping", Range(5, 40)) = 16

        [HideInInspector] _HubShape ("Hub Shape", Vector) = (9.5, 12.5, 7, 0)
        [HideInInspector] _Tint ("Tint", Color) = (1, 1, 1, 0)
        [HideInInspector] _Notch ("Notch", Float) = 0
        [HideInInspector] _ClickTime ("Click Time", Float) = -100
        [HideInInspector] _HubScale ("Gear Scale", Float) = 1
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
            #pragma vertex HubVertex
            #pragma fragment HubFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ElementFX.hlsl"
            #include "GearPresence.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SteelColor;
                half4 _BevelColor;
                half4 _ShadeColor;
                half4 _InkColor;
                float _InkWidth;
                float _BandMargin;
                float _ToothLength;
                float _ToothRoot;
                float _ToothTip;
                float _RivetRadius;
                half _GlowIntensity;
                float _ClickStiffness;
                float _ClickDamping;
                float4 _HubShape;
                half4 _Tint;
                float _Notch;
                float _ClickTime;
                float _HubScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 fromCentre : TEXCOORD0;   // from the gear's centre, in the gear's own units
            };

            Varyings HubVertex(Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                // In the gear's own units (world units at full size), so the cog shrinks with the arcs while the
                // gear sits back (GearManager sets _HubScale to the gear's scale).
                o.fromCentre = (positionWS.xy - TransformObjectToWorld(float3(0.0, 0.0, 0.0)).xy) / max(_HubScale, 1e-3);
                return o;
            }

            half4 HubFragment(Varyings input) : SV_Target
            {
                float2 p = input.fromCentre;
                float r = length(p);
                float teeth = max(_HubShape.z, 1.0);
                float pitch = 6.2831853 / teeth;   // radians between teeth
                float inner = _HubShape.x - _BandMargin;
                float outer = _HubShape.y + _BandMargin;
                float middle = (inner + outer) * 0.5;

                // The click: a spring from the last notch to this one, overshooting a touch and settling. The
                // stamp is on the same clock as _Time.y (Time.time); one in the future counts as settled.
                float sinceClick = _Time.y - _ClickTime;
                sinceClick = sinceClick < 0.0 ? 100.0 : sinceClick;
                float spring = 1.0 - exp(-sinceClick * _ClickDamping) * cos(sinceClick * _ClickStiffness);
                half flash = exp(-sinceClick * 7.0);

                // Teeth sit in the gaps between arcs: half a step round from each arc's centre.
                float turn = _HubShape.w + (_Notch - 1.0 + spring + 0.5) * pitch;
                float rel = atan2(p.y, p.x) - turn;
                rel -= pitch * round(rel / pitch);   // angle from the nearest gap, -pitch/2..pitch/2
                float across = rel * r;            // world units across that gap

                // The cog: the band, plus a tapering tooth out of it at every gap.
                float band = max(inner - r, r - outer);
                float up = r - outer;
                float halfWidth = lerp(_ToothRoot, _ToothTip, saturate(up / _ToothLength)) * 0.5;
                float tooth = max(abs(across) - halfWidth, max(-up - 0.3, up - _ToothLength));
                float cog = min(band, EFX_FieldToDistance(tooth, r));
                float rivet = length(float2(across, r - middle)) - _RivetRadius;

                // Flat steel: a lit bevel toward the outside, shade toward the inside.
                half3 rgb = _SteelColor.rgb;
                rgb = lerp(rgb, _ShadeColor.rgb, 1.0 - EFX_Step(inner + 0.45, r));
                rgb = lerp(rgb, _BevelColor.rgb, EFX_Step(outer - 0.4, r));

                // Element glow: the tooth caps and the band's inner rim, flashing on a click.
                half imbued = _Tint.a;
                half3 glow = _Tint.rgb * _GlowIntensity;
                half cap = EFX_Step(_ToothLength - 0.38, up) * EFX_Fill(tooth);
                half3 capColor = lerp(_BevelColor.rgb * 1.15, glow, imbued);
                rgb = lerp(rgb, capColor, cap);
                half rim = (1.0 - EFX_Step(inner + 0.2, r)) * imbued;
                rgb = lerp(rgb, glow, rim);
                rgb += lerp(half3(1.0, 1.0, 1.0), _Tint.rgb, imbued) * flash * 1.5 * EFX_Fill(tooth);

                // Rivets in the gaps, with a glint.
                half3 rivetColor = lerp(_ShadeColor.rgb, _BevelColor.rgb * 1.2,
                                        EFX_Step(0.0, -dot(float2(across, r - middle), float2(0.6, -0.8))));
                rgb = lerp(rgb, _InkColor.rgb, EFX_Fill(rivet - 0.06));
                rgb = lerp(rgb, rivetColor, EFX_Fill(rivet));

                rgb = lerp(rgb, _InkColor.rgb, EFX_Step(-_InkWidth, cog));
                // Sits back with the rest of the gear when the player isn't picking an element.
                return GearRecede(half4(rgb, EFX_Fill(cog)));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
