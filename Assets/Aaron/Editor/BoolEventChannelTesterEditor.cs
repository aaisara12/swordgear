#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// TODO: aisara => Make a base class that we can reuse for other EventChannelTesters - just have each implement Trigger()
[CustomEditor(typeof(BoolEventChannelTester))]
public class BoolEventChannelTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoolEventChannelTester tester = (BoolEventChannelTester)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Trigger"))
        {
            tester.Trigger();
            EditorUtility.SetDirty(tester);
        }
    }
}
#endif
