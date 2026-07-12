#nullable enable

using System;

/// <summary>
/// Builds a runtime <see cref="CombatEncounter"/> from run context + catalog + composer settings.
/// </summary>
public static class EncounterBuilder
{
    public static CombatEncounter Build(
        in EncounterContext context,
        EnemyCatalog catalog,
        WaveComposerSettings settings)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return WaveComposer.Compose(context, catalog, settings);
    }
}
