#nullable enable

using System.Collections.Generic;
using System.Text;

/// <summary> Wording for streak bonuses, shared by the HUD panel and the overhead callouts. </summary>
public static class StreakText
{
    public static string Label(StreakStat stat) => stat switch
    {
        StreakStat.Damage => "DMG",
        StreakStat.AttackSpeed => "ATK SPD",
        StreakStat.MoveSpeed => "MOVE",
        _ => stat.ToString().ToUpperInvariant(),
    };

    /// <summary> "+40% DMG" for two stacks of +20% damage; several bonuses are joined by <paramref name="separator"/>. </summary>
    public static string Describe(IReadOnlyList<StreakBonus> bonuses, int stacks, string separator = "  ")
    {
        var text = new StringBuilder();
        foreach (StreakBonus bonus in bonuses)
        {
            if (text.Length > 0)
            {
                text.Append(separator);
            }

            text.Append('+').Append(UnityEngine.Mathf.RoundToInt(bonus.percentPerStack * stacks)).Append("% ").Append(Label(bonus.stat));
        }

        return text.ToString();
    }
}
