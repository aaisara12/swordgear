using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneAttribute))]
public class StartSceneAttributeDrawer : PropertyDrawer
{
    // Cache display options (with a <None> entry at index 0)
    private string[] displayOptions = null;

    private void EnsureSceneCache()
    {
        if (displayOptions != null) return;

        var buildScenes = UnityEditor.EditorBuildSettings.scenes;
        var names = new List<string> { "<None>" };

        foreach (var scene in buildScenes)
        {
            var path = scene.path ?? string.Empty;
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            names.Add(string.IsNullOrEmpty(name) ? path : name);
        }

        displayOptions = names.ToArray();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureSceneCache();

        if (displayOptions == null || displayOptions.Length == 1)
        {
            EditorGUI.LabelField(position, label.text, "No scenes in Build Settings");
            return;
        }

        var current = property.stringValue ?? string.Empty;
        int currentIndex = 0; // default to <None>
        for (int i = 1; i < displayOptions.Length; i++)
        {
            if (displayOptions[i] == current)
            {
                currentIndex = i;
                break;
            }
        }

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
        if (newIndex != currentIndex)
        {
            property.stringValue = (newIndex <= 0) ? string.Empty : displayOptions[newIndex];
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
