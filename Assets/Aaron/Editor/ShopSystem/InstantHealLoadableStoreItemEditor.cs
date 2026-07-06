#if UNITY_EDITOR
#nullable enable

using Shop;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InstantHealLoadableStoreItem))]
public class InstantHealLoadableStoreItemEditor : Editor
{
    private SerializedProperty? _idProp;
    private SerializedProperty? _healPercentProp;

    private void OnEnable()
    {
        _idProp = serializedObject.FindProperty("id");
        _healPercentProp = serializedObject.FindProperty("healPercentOfMaxHp");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_healPercentProp != null && _idProp != null)
        {
            _idProp.stringValue = InstantHealSerializer.Serialize(_healPercentProp.floatValue);
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", "id");
        if (_idProp != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_idProp);
            EditorGUI.EndDisabledGroup();
        }

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
