using System.Collections.Generic;

public class LevelBlueprint
{
    public ArenaLayoutTemplate Layout;

    /// <summary>Primary wave source (Commit 21). When set, <see cref="LevelLoader"/> spawns from composed specs.</summary>
    public CombatEncounter Encounter;

    /// <summary>Legacy ScriptableObject wave list. Used only when <see cref="Encounter"/> is null.</summary>
    public List<EnemyWaveConfig> Waves;

    public bool IsShopLevel;

    public int WaveCount
    {
        get
        {
            if (Encounter != null)
            {
                return Encounter.WaveCount;
            }

            return Waves != null ? Waves.Count : 0;
        }
    }
}
