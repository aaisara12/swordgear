#nullable enable

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for GameInitializer.
/// Shows elementManager/playerBlobLoader/sceneTransitioner normally and
/// renders startSceneName as a dropdown populated from Editor Build Settings.
/// </summary>
[CustomEditor(typeof(GameInitializer))]
public class GameInitializerEditor : Editor
{
    private SerializedProperty? elementManagerProp;
    private SerializedProperty? playerBlobLoaderProp;
    private SerializedProperty? sceneTransitionerProp;
    private SerializedProperty? startSceneNameProp;

    private void OnEnable()
    {
        elementManagerProp = serializedObject.FindProperty("elementManager");
        playerBlobLoaderProp = serializedObject.FindProperty("playerBlobLoader");
        sceneTransitionerProp = serializedObject.FindProperty("sceneTransitioner");
        startSceneNameProp = serializedObject.FindProperty("startSceneName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (elementManagerProp != null) EditorGUILayout.PropertyField(elementManagerProp);
        if (playerBlobLoaderProp != null) EditorGUILayout.PropertyField(playerBlobLoaderProp);
        if (sceneTransitionerProp != null) EditorGUILayout.PropertyField(sceneTransitionerProp);

        // Custom UI for startSceneName: dropdown from build settings
        if (startSceneNameProp == null)
        {
            EditorGUILayout.HelpBox("startSceneName property not found.", MessageType.Error);
        }
        else
        {
            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes == null || buildScenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes found in Build Settings. Add scenes to enable the Start Scene dropdown.", MessageType.Warning);
                EditorGUILayout.PropertyField(startSceneNameProp, new GUIContent("Start Scene Name"));
            }
            else
            {
                // Build display names list (first entry is "<None>")
                string[] names = new string[buildScenes.Length + 1];
                names[0] = "<None>";
                for (int i = 0; i < buildScenes.Length; i++)
                {
                    var path = buildScenes[i].path;
                    names[i + 1] = Path.GetFileNameWithoutExtension(path);
                }

                // Determine current selection index by comparing stored name
                string currentName = startSceneNameProp.stringValue ?? string.Empty;
                int currentIndex = 0;
                if (!string.IsNullOrEmpty(currentName))
                {
                    for (int i = 0; i < buildScenes.Length; i++)
                    {
                        if (names[i + 1] == currentName)
                        {
                            currentIndex = i + 1;
                            break;
                        }
                    }
                }

                int newIndex = EditorGUILayout.Popup("Start Scene", currentIndex, names);
                if (newIndex != currentIndex)
                {
                    if (newIndex == 0) startSceneNameProp.stringValue = string.Empty;
                    else startSceneNameProp.stringValue = names[newIndex];
                }

                // Also show the path of the selected scene (optional)
                if (newIndex > 0)
                {
                    EditorGUILayout.LabelField("Scene Path", buildScenes[newIndex - 1].path);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

