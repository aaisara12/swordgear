#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Shop;

/// <summary>
/// Converts between stat boost entries and item ID for PlayerBlob inventory.
/// ID format: "stat-MoveSpeed-5" (single) or "stat-MoveSpeed-5,RangedDamage--10" (multiple). Values can be negative.
/// </summary>
public static class StatBoostSerializer
{
    private const string kStatBoostItemIdPrefix = "stat-";

    public static string Serialize(IReadOnlyList<StatBoostEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return kStatBoostItemIdPrefix + "MoveSpeed-0";

        var parts = new List<string>(entries.Count);
        foreach (var e in entries)
            parts.Add($"{e.kind}-{e.value.ToString(CultureInfo.InvariantCulture)}");
        return kStatBoostItemIdPrefix + string.Join(",", parts);
    }

    /// <summary>Legacy: serialize a single kind+value.</summary>
    public static string Serialize(StatBoostKind kind, float value)
    {
        return Serialize(new[] { new StatBoostEntry { kind = kind, value = value } });
    }

    /// <summary>Parse item id into a list of stat entries. Returns true if id is a valid stat-boost id.</summary>
    public static bool TryDeserializeEntries(string? id, out List<StatBoostEntry> entries)
    {
        entries = new List<StatBoostEntry>();
        if (string.IsNullOrWhiteSpace(id))
            return false;

        int prefixIdx = id.IndexOf(kStatBoostItemIdPrefix, StringComparison.Ordinal);
        if (prefixIdx == -1)
            return false;

        string suffix = id.Substring(prefixIdx + kStatBoostItemIdPrefix.Length);
        if (string.IsNullOrEmpty(suffix))
            return false;

        string[] segments = suffix.Split(',');
        foreach (string seg in segments)
        {
            if (string.IsNullOrWhiteSpace(seg))
                continue;
            int dashIdx = seg.IndexOf('-');
            if (dashIdx <= 0 || dashIdx == seg.Length - 1)
                continue;
            string kindStr = seg.Substring(0, dashIdx);
            string valueStr = seg.Substring(dashIdx + 1);
            if (!Enum.TryParse(kindStr, ignoreCase: true, result: out StatBoostKind kind))
                continue;
            if (!float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                continue;
            entries.Add(new StatBoostEntry { kind = kind, value = value });
        }

        return entries.Count > 0;
    }

    /// <summary>Legacy: parse single stat from id. Uses first entry if multiple.</summary>
    public static bool TryDeserialize(string? id, out StatBoostKind kind, out float value)
    {
        kind = default;
        value = 0f;
        if (!TryDeserializeEntries(id, out var entries) || entries.Count == 0)
            return false;
        kind = entries[0].kind;
        value = entries[0].value;
        return true;
    }
}
