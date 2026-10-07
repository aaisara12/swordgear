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

    [Tooltip("How many times the ultimate can be overcharged past its base version. Each extra full set of the " +
             "requirements above grants one level; sets owned beyond this cap are ignored. Players aren't " +
             "expected to reach it — it exists so the ceiling is easy to retune.")]
    [SerializeField] private int _maxOverchargeLevel = 10;

    [SerializeField] private UltimateEffect? _effect;

    public IReadOnlyList<ElementRequirement> Requirements => _requirements;
    public float ChargeRequired => Mathf.Max(0.01f, _chargeRequired);
    public int MaxOverchargeLevel => Mathf.Max(0, _maxOverchargeLevel);
    public UltimateEffect? Effect => _effect;

    /// <summary>
    /// How many complete sets of the element requirements the player's augments cover. 0 means locked — the
    /// ultimate neither accumulates charge nor activates, 1 is the base ultimate, and every set past the first
    /// is one level of overcharge. Uncapped; <see cref="GetOverchargeLevel"/> applies the cap.
    /// An ultimate that declares no requirements is always unlocked with exactly one set.
    /// </summary>
    public int GetCompletedSets(IReadOnlyDictionary<Element, int> augmentCounts)
    {
        int sets = int.MaxValue;

        foreach (ElementRequirement requirement in _requirements)
        {
            if (requirement.count <= 0)
                continue;

            int owned = augmentCounts.TryGetValue(requirement.element, out int count) ? count : 0;
            sets = Mathf.Min(sets, owned / requirement.count);
        }

        return sets == int.MaxValue ? 1 : sets;
    }

    /// <summary>
    /// The overcharge level handed to the effect: 0 for the base ultimate, +1 per extra full set of requirements,
    /// capped at <see cref="MaxOverchargeLevel"/>. Returns 0 while locked, so check the set count — not this — to
    /// decide whether the ultimate is usable.
    /// </summary>
    public int GetOverchargeLevel(IReadOnlyDictionary<Element, int> augmentCounts) =>
        Mathf.Clamp(GetCompletedSets(augmentCounts) - 1, 0, MaxOverchargeLevel);

    /// <summary>
    /// How far (0-1) one element requirement has come toward completing the set above
    /// <paramref name="completedSets"/>. Drives the per-element readout: the unlock set while the ultimate is
    /// locked, the next overcharge level once it isn't.
    /// </summary>
    public float GetRequirementFill(ElementRequirement requirement, int completedSets, IReadOnlyDictionary<Element, int> augmentCounts)
    {
        if (requirement.count <= 0)
            return 1f;

        int owned = augmentCounts.TryGetValue(requirement.element, out int count) ? count : 0;
        int alreadySpentOnLowerSets = completedSets * requirement.count;

        return Mathf.Clamp01((owned - alreadySpentOnLowerSets) / (float)requirement.count);
    }
}
