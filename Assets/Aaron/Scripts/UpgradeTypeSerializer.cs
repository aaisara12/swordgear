#nullable enable

using System;

/// <summary>
/// Converts between UpgradeType enum values and their string representations for serialization.
/// Provides single-value and collection helpers.
/// </summary>
public static class UpgradeTypeSerializer
{
    private const string kElementUpgradeItemIdPrefix = "elem-upgrade-";
    
    /// <summary>
    /// Serialize a single UpgradeType to its string form (the enum name).
    /// </summary>
    public static string Serialize(UpgradeType upgrade)
    {
        return $"{kElementUpgradeItemIdPrefix}{upgrade}";
    }

    /// <summary>
    /// Try to parse a string into an UpgradeType. Case-insensitive.
    /// Returns false if parsing fails.
    /// </summary>
    public static bool TryDeserialize(string? value, out UpgradeType upgrade)
    {
        upgrade = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        int indexOfStartOfUpgradeString = value.IndexOf(kElementUpgradeItemIdPrefix, StringComparison.Ordinal);

        if (indexOfStartOfUpgradeString == -1)
        {
            return false;
        }
        
        // aisara => Unfortunately, Unity runtime uses .NET4.6 so we can't use ReadOnlySpans overload in TryParse
        string upgradeString = value.Substring(indexOfStartOfUpgradeString + kElementUpgradeItemIdPrefix.Length);

        return Enum.TryParse(upgradeString, ignoreCase: true, result: out upgrade);
    }

    /// <summary>
    /// Parse a string into an UpgradeType or throw ArgumentException on failure.
    /// </summary>
    public static UpgradeType Deserialize(string? value)
    {
        if (TryDeserialize(value, out var upgrade))
            return upgrade;

        throw new ArgumentException($"Unknown UpgradeType value '{value}'", nameof(value));
    }
}
        


