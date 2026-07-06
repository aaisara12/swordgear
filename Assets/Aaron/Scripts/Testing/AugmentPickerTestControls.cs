#nullable enable

using UnityEngine;

/// <summary>
/// Runtime controls for <see cref="AugmentPickerTestHarness"/> in the augment picker test scene.
/// </summary>
public class AugmentPickerTestControls : MonoBehaviour
{
    [SerializeField] private AugmentPickerTestHarness? harness;

    private void Awake()
    {
        if (harness == null)
        {
            harness = FindFirstObjectByType<AugmentPickerTestHarness>();
        }
    }

    public void RefreshAugments()
    {
        harness?.RefreshAugmentPicker();
    }

    public void SetMinimumTierBronze() => SetMinimumTier(0);
    public void SetMinimumTierSilver() => SetMinimumTier(1);
    public void SetMinimumTierGold() => SetMinimumTier(2);
    public void SetMinimumTierDiamond() => SetMinimumTier(3);

    private void SetMinimumTier(int tierIndex)
    {
        harness?.SetDebugMinimumTier(tierIndex);
    }
}
