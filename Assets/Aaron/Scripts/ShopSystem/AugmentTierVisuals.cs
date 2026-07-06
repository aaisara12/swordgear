#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public readonly struct AugmentTierCardStyle
    {
        public AugmentTierCardStyle(
            Color baseColor,
            Color highlightColor,
            Color shadowColor,
            Color borderColor,
            Color auraColor,
            Color flareColor,
            float glowStrength,
            float sweepStrength,
            float pulseStrength,
            float sparkleStrength,
            float sparkleDensity,
            float metallicNoise,
            float auraIntensity,
            float flareIntensity)
        {
            BaseColor = baseColor;
            HighlightColor = highlightColor;
            ShadowColor = shadowColor;
            BorderColor = borderColor;
            AuraColor = auraColor;
            FlareColor = flareColor;
            GlowStrength = glowStrength;
            SweepStrength = sweepStrength;
            PulseStrength = pulseStrength;
            SparkleStrength = sparkleStrength;
            SparkleDensity = sparkleDensity;
            MetallicNoise = metallicNoise;
            AuraIntensity = auraIntensity;
            FlareIntensity = flareIntensity;
        }

        public Color BaseColor { get; }
        public Color HighlightColor { get; }
        public Color ShadowColor { get; }
        public Color BorderColor { get; }
        public Color AuraColor { get; }
        public Color FlareColor { get; }
        public float GlowStrength { get; }
        public float SweepStrength { get; }
        public float PulseStrength { get; }
        public float SparkleStrength { get; }
        public float SparkleDensity { get; }
        public float MetallicNoise { get; }
        public float AuraIntensity { get; }
        public float FlareIntensity { get; }
    }

    public static class AugmentTierVisuals
    {
        private const float DiamondSweepSpeed = 0.48f;
        private const float FlareHotspotY = 0.16f;
        private const float FlareIntensity = 1.2f;
        private const float RimGlowStrength = 1.5f;

        private static readonly Color BronzeBase = new(0.22f, 0.11f, 0.03f, 1f);
        private static readonly Color BronzeHighlight = new(0.72f, 0.42f, 0.12f, 0.9f);
        private static readonly Color BronzeShadow = new(0.06f, 0.03f, 0.01f, 1f);
        private static readonly Color BronzeBorder = new(0.82f, 0.48f, 0.12f, 0.9f);
        private static readonly Color BronzeAura = new(1.0f, 0.42f, 0.06f, 1f);

        private static readonly Color SilverBase = new(0.26f, 0.28f, 0.34f, 1f);
        private static readonly Color SilverHighlight = new(0.60f, 0.64f, 0.74f, 0.80f);
        private static readonly Color SilverShadow = new(0.11f, 0.12f, 0.15f, 1f);
        private static readonly Color SilverBorder = new(0.70f, 0.74f, 0.80f, 0.9f);
        private static readonly Color SilverAura = new(0.65f, 0.82f, 1.1f, 1f);
        private static readonly Color SilverFlare = new(0.46f, 0.52f, 0.66f, 1f);
        private const float SilverRimGlowStrength = 1.1f;
        private const float SilverFlareIntensity = 1.0f;

        private static readonly Color GoldBase = new(0.36f, 0.22f, 0.01f, 1f);
        private static readonly Color GoldHighlight = new(0.85f, 0.70f, 0.14f, 0.95f);
        private static readonly Color GoldShadow = new(0.13f, 0.07f, 0.0f, 1f);
        private static readonly Color GoldBorder = new(0.85f, 0.64f, 0.05f, 0.95f);
        private static readonly Color GoldAura = new(1.1f, 0.68f, 0.04f, 1f);

        private static readonly Color DiamondBase = new(0.06f, 0.14f, 0.38f, 1f);
        private static readonly Color DiamondHighlight = new(0.45f, 0.78f, 1.2f, 1f);
        private static readonly Color DiamondShadow = new(0.02f, 0.06f, 0.18f, 1f);
        private static readonly Color DiamondBorder = new(0.55f, 0.88f, 1.3f, 1f);
        private static readonly Color DiamondAura = new(0.30f, 0.65f, 1.5f, 1f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int SweepStrengthId = Shader.PropertyToID("_SweepStrength");
        private static readonly int SweepSpeedId = Shader.PropertyToID("_SweepSpeed");
        private static readonly int PulseStrengthId = Shader.PropertyToID("_PulseStrength");
        private static readonly int SparkleStrengthId = Shader.PropertyToID("_SparkleStrength");
        private static readonly int SparkleDensityId = Shader.PropertyToID("_SparkleDensity");
        private static readonly int MetallicNoiseId = Shader.PropertyToID("_MetallicNoise");
        private static readonly int TimeOffsetId = Shader.PropertyToID("_TimeOffset");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int HotspotYId = Shader.PropertyToID("_HotspotY");
        private static readonly int RimInnerId = Shader.PropertyToID("_RimInner");
        private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");

        public static AugmentQualityTier ResolveTier(IStoreItem item) =>
            item is LoadableStoreItem loadable ? loadable.QualityTier : AugmentQualityTier.Low;

        public static Color GetCardBackgroundColor(AugmentQualityTier tier) =>
            GetCardStyle(tier).BaseColor;

        public static AugmentTierCardStyle GetCardStyle(AugmentQualityTier tier) =>
            tier switch
            {
                AugmentQualityTier.Medium => new AugmentTierCardStyle(
                    SilverBase, SilverHighlight, SilverShadow, SilverBorder, SilverAura, SilverFlare,
                    SilverRimGlowStrength, 0f, 0.38f, 0f, 0f, 0.32f, 0f, SilverFlareIntensity),
                AugmentQualityTier.High => new AugmentTierCardStyle(
                    GoldBase, GoldHighlight, GoldShadow, GoldBorder, GoldAura, GoldHighlight,
                    RimGlowStrength, 0f, 0.48f, 0.22f, 0.38f, 0.38f, 0f, FlareIntensity),
                AugmentQualityTier.Elite => new AugmentTierCardStyle(
                    DiamondBase, DiamondHighlight, DiamondShadow, DiamondBorder, DiamondAura, DiamondHighlight,
                    RimGlowStrength, 1.25f, 0.58f, 0.9f, 0.62f, 0.42f, 0f, FlareIntensity),
                _ => new AugmentTierCardStyle(
                    BronzeBase, BronzeHighlight, BronzeShadow, BronzeBorder, BronzeAura, BronzeHighlight,
                    RimGlowStrength, 0f, 0.34f, 0f, 0f, 0.28f, 0f, FlareIntensity),
            };

        public static void ApplyCardStyle(
            Image cardBackground,
            Image? cardBorder,
            Image? cardAura,
            Image? cardInnerFlare,
            AugmentQualityTier tier,
            Material cardMaterialInstance,
            Material? auraMaterialInstance,
            Material? flareMaterialInstance,
            float timeOffset)
        {
            AugmentTierCardStyle style = GetCardStyle(tier);

            float glowStrength = style.GlowStrength;
            float sweepStrength = style.SweepStrength;
            float pulseStrength = style.PulseStrength;
            float sparkleStrength = style.SparkleStrength;
            float metallicNoise = style.MetallicNoise;
            float flareIntensity = style.FlareIntensity;
            float flareHotspotY = FlareHotspotY;
            float sweepSpeed = sweepStrength > 0.01f ? DiamondSweepSpeed : 0f;

            cardMaterialInstance.SetColor(ColorId, style.BaseColor);
            cardMaterialInstance.SetColor(HighlightColorId, style.HighlightColor);
            cardMaterialInstance.SetColor(ShadowColorId, style.ShadowColor);
            cardMaterialInstance.SetFloat(GlowStrengthId, glowStrength);
            cardMaterialInstance.SetFloat(SweepStrengthId, sweepStrength);
            cardMaterialInstance.SetFloat(SweepSpeedId, sweepSpeed);
            cardMaterialInstance.SetFloat(PulseStrengthId, pulseStrength);
            cardMaterialInstance.SetFloat(SparkleStrengthId, sparkleStrength);
            cardMaterialInstance.SetFloat(SparkleDensityId, style.SparkleDensity);
            cardMaterialInstance.SetFloat(MetallicNoiseId, metallicNoise);
            cardMaterialInstance.SetFloat(TimeOffsetId, timeOffset);
            cardMaterialInstance.SetFloat(RimInnerId, Mathf.Lerp(0.52f, 0.26f, Mathf.Clamp01(glowStrength / 2f)));
            cardMaterialInstance.SetFloat(RimPowerId, Mathf.Lerp(1.1f, 0.45f, Mathf.Clamp01(glowStrength / 2f)));

            cardBackground.material = cardMaterialInstance;
            cardBackground.color = Color.white;

            if (cardBorder != null)
            {
                cardBorder.color = style.BorderColor;
            }

            if (cardAura != null)
            {
                cardAura.enabled = false;
            }

            if (cardInnerFlare != null && flareMaterialInstance != null)
            {
                flareMaterialInstance.SetColor(ColorId, style.FlareColor);
                flareMaterialInstance.SetFloat(IntensityId, flareIntensity);
                flareMaterialInstance.SetFloat(HotspotYId, flareHotspotY);
                flareMaterialInstance.SetFloat(TimeOffsetId, timeOffset);
                cardInnerFlare.material = flareMaterialInstance;
                cardInnerFlare.color = Color.white;
                cardInnerFlare.enabled = flareIntensity > 0.01f;
            }
            else if (cardInnerFlare != null)
            {
                cardInnerFlare.enabled = false;
            }
        }
    }
}
