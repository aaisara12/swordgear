#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode menu to spawn one of each new archetype near the player for Commit 18 playtesting.
/// Menu: Henry/Playtest/Spawn New Enemy Archetypes (Play Mode)
/// </summary>
public static class EnemyArchetypePlaytestMenu
{
    private const string PrefabFolder = "Assets/Visuals/Prefabs/Enemies";

    [MenuItem("Henry/Playtest/Spawn New Enemy Archetypes (Play Mode)", true)]
    private static bool ValidateSpawn()
    {
        return Application.isPlaying;
    }

    [MenuItem("Henry/Playtest/Spawn New Enemy Archetypes (Play Mode)")]
    public static void SpawnShowcase()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("EnemyArchetypePlaytestMenu: enter Play Mode first.");
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            Debug.LogError("EnemyArchetypePlaytestMenu: player not found.");
            return;
        }

        Vector3 origin = GameManager.Instance.player.transform.position;
        string[] prefabNames =
        {
            "Turret_Physical",
            "Shotgun_Physical",
            "BeamSniper_Physical",
            "Turret_Fire",
            "Shotgun_Fire",
            "BeamSniper_Fire",
        };

        float spacing = 2.5f;
        for (int i = 0; i < prefabNames.Length; i++)
        {
            string path = $"{PrefabFolder}/{prefabNames[i]}.prefab";
            GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"EnemyArchetypePlaytestMenu: missing {path}. Run Henry → Generate New Enemy Archetype Prefabs.");
                continue;
            }

            float x = (i - (prefabNames.Length - 1) * 0.5f) * spacing;
            Vector3 position = origin + new Vector3(x, 3f, 0f);
            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            ApplyPlaytestModifiers(instance, prefab);
        }

        Debug.Log("EnemyArchetypePlaytestMenu: spawned turret, shotgun, and beam sniper showcase (physical + fire).");
    }

    private static void ApplyPlaytestModifiers(GameObject instance, GameObject prefab)
    {
        EnemyController? controller = instance.GetComponent<EnemyController>();
        if (controller == null)
        {
            return;
        }

        EnemyCatalog? catalog = RunManager.Instance?.EnemyCatalog;
        if (catalog == null)
        {
            catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/Aaron/ScriptableObjects/EnemyCatalog.asset");
        }

        if (catalog == null)
        {
            return;
        }

        if (EncounterContext.TryFromCurrent(RunManager.Instance?.Run, out EncounterContext context))
        {
            controller.ApplySpawnModifiers(catalog.ResolveSpawnModifiers(context, prefab));
            return;
        }

        if (catalog.TryGetByPrefab(prefab, out EnemyArchetype? archetype)
            && archetype != null
            && archetype.applyElementKnobsAtSpawn)
        {
            controller.ApplySpawnModifiers(SpawnModifiers.FromElement(catalog.GetElementKnobs(archetype.element)));
        }
    }
}
#endif
