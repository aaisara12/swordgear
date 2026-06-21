#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UltimateAbility", menuName = "Game/Ultimate Ability")]
public class UltimateAbilitySO : ScriptableObject
{
    [Serializable]
    public struct ElementCharge
    {
        public Element element;
        public int count;
    }

    [SerializeField] private List<ElementCharge> _requirements = new();
    [SerializeField] private UltimateEffect? _effect;

    public IReadOnlyList<ElementCharge> Requirements => _requirements;
    public UltimateEffect? Effect => _effect;

    public bool IsSatisfiedBy(Dictionary<Element, int> charges)
    {
        foreach (var req in _requirements)
        {
            if (!charges.TryGetValue(req.element, out int count) || count < req.count)
                return false;
        }
        return true;
    }
}
