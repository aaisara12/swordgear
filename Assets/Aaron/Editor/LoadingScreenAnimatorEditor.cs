#nullable enable
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadingScreenAnimator))]
public class LoadingScreenAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default fields
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var animator = (LoadingScreenAnimator)target;

        // Buttons only active in Play Mode (coroutines require runtime)
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        if (GUILayout.Button("Fade In"))
        {
            animator.FadeInLoadingScreen();
            EditorUtility.SetDirty(animator);
        }

        if (GUILayout.Button("Fade Out"))
        {
            animator.FadeOutLoadingScreen();
            EditorUtility.SetDirty(animator);
        }
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to run the animations.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}