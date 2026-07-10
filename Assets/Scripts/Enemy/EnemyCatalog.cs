#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative roster of the 20 enemy archetypes plus per-element combat knobs.
/// WaveComposer (Commit 21) spends <see cref="EnemyArchetype.baseThreatCost"/> from this catalog.
/// </summary>
[CreateAssetMenu(fileName = "EnemyCatalog", menuName = "Scriptable Objects/EnemyCatalog")]
public class EnemyCatalog : ScriptableObject
{
    [SerializeField] private List<EnemyArchetype> archetypes = new();
    [SerializeField] private List<ElementStatKnobs> elementKnobs = new();

    public IReadOnlyList<EnemyArchetype> Archetypes => archetypes;
    public IReadOnlyList<ElementStatKnobs> ElementKnobs => elementKnobs;

    public bool TryGetById(string id, out EnemyArchetype? archetype)
    {
        archetype = null;
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (int i = 0; i < archetypes.Count; i++)
        {
            EnemyArchetype entry = archetypes[i];
            if (entry != null && entry.id == id)
            {
                archetype = entry;
                return true;
            }
        }

        return false;
    }

    public bool TryGetByPrefab(GameObject? prefab, out EnemyArchetype? archetype)
    {
        archetype = null;
        if (prefab == null)
        {
            return false;
        }

        for (int i = 0; i < archetypes.Count; i++)
        {
            EnemyArchetype entry = archetypes[i];
            if (entry != null && entry.prefab == prefab)
            {
                archetype = entry;
                return true;
            }
        }

        return false;
    }

    public ElementStatKnobs GetElementKnobs(Element element)
    {
        for (int i = 0; i < elementKnobs.Count; i++)
        {
            if (elementKnobs[i].element == element)
            {
                return elementKnobs[i];
            }
        }

        return ElementStatKnobs.DefaultFor(element);
    }

    /// <summary>
    /// Difficulty curve × optional elemental knobs for the spawned prefab.
    /// Legacy melee/ranged keep baked elemental stats (<see cref="EnemyArchetype.applyElementKnobsAtSpawn"/> = false).
    /// </summary>
    public SpawnModifiers ResolveSpawnModifiers(in EncounterContext context, GameObject? prefab)
    {
        SpawnModifiers difficulty = DifficultyCurve.Evaluate(context);
        if (!TryGetByPrefab(prefab, out EnemyArchetype? archetype) || archetype == null)
        {
            return difficulty;
        }

        if (!archetype.applyElementKnobsAtSpawn)
        {
            return difficulty;
        }

        ElementStatKnobs knobs = GetElementKnobs(archetype.element);
        return SpawnModifiers.Combine(difficulty, SpawnModifiers.FromElement(knobs));
    }

#if UNITY_EDITOR
    public void EditorSetArchetypes(List<EnemyArchetype> entries)
    {
        archetypes = entries ?? new List<EnemyArchetype>();
    }

    public void EditorSetElementKnobs(List<ElementStatKnobs> knobs)
    {
        elementKnobs = knobs ?? new List<ElementStatKnobs>();
    }
#endif
}
