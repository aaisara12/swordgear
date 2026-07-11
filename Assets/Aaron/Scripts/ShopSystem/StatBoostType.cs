#nullable enable

using System;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Type of stat boost. The numeric value (percent, flat amount, etc.) is defined on the ScriptableObject.
    /// </summary>
    public enum StatBoostKind
    {
        MoveSpeed,           // value = percent (e.g. 5 = +5%, -3 = -3%)
        DamageMultiplier,    // value = percent added to base damage (e.g. 10 = +10%)
        MaxHp,               // value = percent (e.g. 10 = +10%)
        RangedDamage,        // value = percent (e.g. 5 = +5%)
        ProjectileSpeed,     // value = percent (e.g. 10 = +10%)
        UltimateCharge,      // value = percent (e.g. 10 = 10% faster)
        Lifesteal,           // value = percent of damage (e.g. 2 = 2%)
        Regen,               // value = percent max HP per second (e.g. 0.5)
        MeleeRange,          // value = percent larger melee hitbox/reach (e.g. 10 = +10%)
        AttackSpeed,         // value = percent faster melee attacks (e.g. 10 = +10%)
    }

    /// <summary>
    /// One stat change: kind and value. Value can be negative (e.g. trade-offs).
    /// </summary>
    [Serializable]
    public struct StatBoostEntry
    {
        public StatBoostKind kind;
        public float value;
    }
}
