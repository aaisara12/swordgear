#nullable enable
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BoolEventChannelSO))]
[CanEditMultipleObjects]
public class BoolEventChannelSOEditor : Editor
{
    private bool raiseValue;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug / Test", EditorStyles.boldLabel);

        raiseValue = EditorGUILayout.Toggle("Raise Value", raiseValue);

        if (GUILayout.Button("RaiseDataChanged"))
        {
            foreach (var obj in targets)
            {
                if (obj is BoolEventChannelSO channel)
                {
                    // Call the event on each selected asset
                    channel.RaiseDataChanged(raiseValue);

                    // Mark dirty so changes (if any) are saved in the editor
                    EditorUtility.SetDirty(channel);
                }
            }
        }
    }
}