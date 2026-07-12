#nullable enable

using System;
using System.Collections.Generic;

/// <summary>
/// Runtime-only combat definition produced by <see cref="EncounterBuilder"/>.
/// Not a ScriptableObject — one instance per combat step.
/// </summary>
[Serializable]
public class CombatEncounter
{
    public EncounterTheme Theme;
    public string ThemeDisplayName = string.Empty;
    public List<ComposedWave> Waves = new();
    public float ThreatBudget;
    public float ThreatSpent;
    public SpawnModifiers DifficultyModifiers = SpawnModifiers.Identity;

    public int WaveCount => Waves?.Count ?? 0;
}
