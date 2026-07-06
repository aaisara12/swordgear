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

    public void SetComboFloorBronze() => harness?.SetComboFloorTier(0);
    public void SetComboFloorSilver() => harness?.SetComboFloorTier(1);
    public void SetComboFloorGold() => harness?.SetComboFloorTier(2);
    public void SetComboFloorDiamond() => harness?.SetComboFloorTier(3);
}
