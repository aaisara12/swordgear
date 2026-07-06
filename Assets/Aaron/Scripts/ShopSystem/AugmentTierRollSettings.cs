#nullable enable

using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(
        fileName = "AugmentTierRollSettings",
        menuName = "Scriptable Objects/Augment Tier Roll Settings")]
    public class AugmentTierRollSettings : ScriptableObject
    {
        [SerializeField] private AugmentTierRollWeights weights = AugmentTierRollWeights.Default;

        public AugmentTierRollWeights Weights => weights;

        public void SetWeights(AugmentTierRollWeights newWeights)
        {
            weights = newWeights;
        }
    }
}
