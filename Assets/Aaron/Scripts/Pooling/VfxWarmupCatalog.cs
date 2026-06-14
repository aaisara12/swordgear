#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

public enum WarmupTier
{
    Inert,
    Behavioral
}

[Serializable]
public class VfxWarmupEntry
{
    public GameObject? prefab;
    public WarmupTier tier = WarmupTier.Inert;
    [Min(0)] public int prewarmCount = 2;
}

[CreateAssetMenu(fileName = "VfxWarmupCatalog", menuName = "Swordgear/VFX Warmup Catalog")]
public class VfxWarmupCatalog : ScriptableObject
{
    // aisara => Re-record CombatVfxShaderVariants.shadervariants after changing effect materials/keywords (Graphics Settings > Save to asset).
    [SerializeField] private ShaderVariantCollection? shaderVariants;
    [SerializeField] private List<VfxWarmupEntry> entries = new();

    public ShaderVariantCollection? ShaderVariants => GetShaderVariantsSafe();

    public IReadOnlyList<VfxWarmupEntry> Entries => entries;

    ShaderVariantCollection? GetShaderVariantsSafe()
    {
        try
        {
            return shaderVariants;
        }
        catch (MissingReferenceException)
        {
            return null;
        }
    }
}
