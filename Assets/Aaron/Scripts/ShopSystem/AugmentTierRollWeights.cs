#nullable enable

using System;
using UnityEngine;

namespace Shop
{
    [Serializable]
    public struct AugmentTierRollWeights
    {
        public float Bronze;
        public float Silver;
        public float Gold;
        public float Diamond;

        public AugmentTierRollWeights(float bronze, float silver, float gold, float diamond)
        {
            Bronze = bronze;
            Silver = silver;
            Gold = gold;
            Diamond = diamond;
        }

        public static AugmentTierRollWeights Default => new(50f, 20f, 20f, 10f);

        public float Total => Bronze + Silver + Gold + Diamond;

        public AugmentQualityTier RollTier()
        {
            float total = Total;
            if (total <= 0f)
            {
                return AugmentQualityTier.Low;
            }

            float roll = UnityEngine.Random.Range(0f, total);
            if (roll < Bronze)
            {
                return AugmentQualityTier.Low;
            }

            roll -= Bronze;
            if (roll < Silver)
            {
                return AugmentQualityTier.Medium;
            }

            roll -= Silver;
            if (roll < Gold)
            {
                return AugmentQualityTier.High;
            }

            return AugmentQualityTier.Elite;
        }

        public static AugmentQualityTier ApplyComboFloor(AugmentQualityTier rolled, AugmentQualityTier comboFloor) =>
            rolled >= comboFloor ? rolled : comboFloor;

        public void SetTierWeight(AugmentQualityTier tier, float weight)
        {
            switch (tier)
            {
                case AugmentQualityTier.Medium:
                    Silver = weight;
                    break;
                case AugmentQualityTier.High:
                    Gold = weight;
                    break;
                case AugmentQualityTier.Elite:
                    Diamond = weight;
                    break;
                default:
                    Bronze = weight;
                    break;
            }
        }

        public float GetTierWeight(AugmentQualityTier tier) => tier switch
        {
            AugmentQualityTier.Medium => Silver,
            AugmentQualityTier.High => Gold,
            AugmentQualityTier.Elite => Diamond,
            _ => Bronze,
        };
    }
}
