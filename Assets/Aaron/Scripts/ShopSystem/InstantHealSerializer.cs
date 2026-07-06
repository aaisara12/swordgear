#nullable enable

using System;
using System.Globalization;

/// <summary>
/// One-shot heal augment IDs. Format: heal-10 (restore 10% of max HP on pick).
/// </summary>
public static class InstantHealSerializer
{
    private const string Prefix = "heal-";

    public static string Serialize(float percentOfMaxHp) =>
        Prefix + percentOfMaxHp.ToString(CultureInfo.InvariantCulture);

    public static bool TryDeserialize(string? id, out float percentOfMaxHp)
    {
        percentOfMaxHp = 0f;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        int prefixIdx = id.IndexOf(Prefix, StringComparison.Ordinal);
        if (prefixIdx < 0)
        {
            return false;
        }

        string suffix = id.Substring(prefixIdx + Prefix.Length);
        return float.TryParse(suffix, NumberStyles.Float, CultureInfo.InvariantCulture, out percentOfMaxHp);
    }
}
