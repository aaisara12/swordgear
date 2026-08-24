#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UltimateAbility", menuName = "Game/Ultimate Ability")]
public class UltimateAbilitySO : ScriptableObject
{
    [Serializable]
    public struct ElementRequirement
    {
        public Element element;
        public int count;
    }

    [Tooltip("Augments of each element the player must own for this ultimate to unlock. Owning whole extra sets " +
             "overcharges it to a higher level.")]
    [SerializeField] private List<ElementRequirement> _requirements = new();

    [Tooltip("Damage the player must deal to fill the meter once the ultimate is unlocked.")]
    [SerializeField] private float _chargeRequired = 400f;

    [SerializeField] private UltimateEffect? _effect;

    public IReadOnlyList<ElementRequirement> Requirements => _requirements;
    public float ChargeRequired => Mathf.Max(0.01f, _chargeRequired);
    public UltimateEffect? Effect => _effect;

    /// <summary>
    /// How many complete sets of the element requirements the player's augments cover. 0 means locked — the
    /// ultimate neither accumulates charge nor activates. Each additional full set raises the level by one, with
    /// no cap. An ultimate that declares no requirements is always unlocked at level 1.
    /// </summary>
    public int GetLevel(IReadOnlyDictionary<Element, int> augmentCounts)
    {
        int level = int.MaxValue;

        foreach (ElementRequirement requirement in _requirements)
        {
            if (requirement.count <= 0)
                continue;

            int owned = augmentCounts.TryGetValue(requirement.element, out int count) ? count : 0;
            level = Mathf.Min(level, owned / requirement.count);
        }

        return level == int.MaxValue ? 1 : level;
    }

    /// <summary>
    /// How far (0-1) one element requirement has come toward completing the set above <paramref name="level"/>.
    /// Drives the per-element readout while the ultimate is still locked.
    /// </summary>
    public float GetRequirementFill(ElementRequirement requirement, int level, IReadOnlyDictionary<Element, int> augmentCounts)
    {
        if (requirement.count <= 0)
            return 1f;

        int owned = augmentCounts.TryGetValue(requirement.element, out int count) ? count : 0;
        int alreadySpentOnLowerLevels = level * requirement.count;

        return Mathf.Clamp01((owned - alreadySpentOnLowerLevels) / (float)requirement.count);
    }
}
