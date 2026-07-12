#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates/refreshes WaveComposerSettings and wires it to CoreSystems RunManager.
/// Menu: Henry → Generate Wave Composer Settings
/// </summary>
public static class WaveComposerSettingsCreator
{
    private const string SettingsFolder = "Assets/Aaron/ScriptableObjects";
    private const string SettingsPath = SettingsFolder + "/WaveComposerSettings.asset";
    private const string CoreSystemsPrefabPath = "Assets/Aaron/Prefabs/CoreSystems.prefab";

    [MenuItem("Henry/Generate Wave Composer Settings")]
    public static void GenerateFromMenu()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Aaron", "ScriptableObjects");
        }

        WaveComposerSettings settings = AssetDatabase.LoadAssetAtPath<WaveComposerSettings>(SettingsPath);
        bool created = false;
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            created = true;
        }

        EditorUtility.SetDirty(settings);
        WireToCoreSystems(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"WaveComposerSettingsCreator: {(created ? "created" : "updated")} {SettingsPath} and wired to CoreSystems.");
    }

    private static void WireToCoreSystems(WaveComposerSettings settings)
    {
        GameObject? root = PrefabUtility.LoadPrefabContents(CoreSystemsPrefabPath);
        if (root == null)
        {
            Debug.LogError($"WaveComposerSettingsCreator: could not load {CoreSystemsPrefabPath}");
            return;
        }

        try
        {
            RunManager? runManager = root.GetComponentInChildren<RunManager>(true);
            if (runManager == null)
            {
                Debug.LogError("WaveComposerSettingsCreator: RunManager not found on CoreSystems.");
                return;
            }

            SerializedObject so = new SerializedObject(runManager);
            SerializedProperty prop = so.FindProperty("waveComposerSettings");
            if (prop == null)
            {
                Debug.LogError("WaveComposerSettingsCreator: RunManager.waveComposerSettings field not found (script compile?).");
                return;
            }

            prop.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CoreSystemsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
