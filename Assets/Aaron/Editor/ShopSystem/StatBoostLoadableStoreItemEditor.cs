#nullable enable

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Shop;

[CustomEditor(typeof(StatBoostLoadableStoreItem))]
public class StatBoostLoadableStoreItemEditor : Editor
{
    private SerializedProperty? _scriptProp;
    private SerializedProperty? _idProp;
    private SerializedProperty? _displayNameProp;
    private SerializedProperty? _descriptionProp;
    private SerializedProperty? _costProp;
    private SerializedProperty? _iconProp;
    private SerializedProperty? _qualityTierProp;
    private SerializedProperty? _statBoostsProp;

    private void OnEnable()
    {
        _scriptProp = serializedObject.FindProperty("m_Script");
        _idProp = serializedObject.FindProperty("id");
        _displayNameProp = serializedObject.FindProperty("displayName");
        _descriptionProp = serializedObject.FindProperty("description");
        _costProp = serializedObject.FindProperty("cost");
        _iconProp = serializedObject.FindProperty("icon");
        _qualityTierProp = serializedObject.FindProperty("qualityTier");
        _statBoostsProp = serializedObject.FindProperty("statBoosts");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_statBoostsProp != null && _idProp != null)
        {
            var entries = new List<StatBoostEntry>();
            for (int i = 0; i < _statBoostsProp.arraySize; i++)
            {
                var elem = _statBoostsProp.GetArrayElementAtIndex(i);
                var kindProp = elem.FindPropertyRelative("kind");
                var valueProp = elem.FindPropertyRelative("value");
                if (kindProp != null && valueProp != null)
                    entries.Add(new StatBoostEntry { kind = (StatBoostKind)kindProp.enumValueIndex, value = valueProp.floatValue });
            }
            _idProp.stringValue = StatBoostSerializer.Serialize(entries);
        }

        if (_scriptProp != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_scriptProp);
            EditorGUI.EndDisabledGroup();
        }

        if (_idProp != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_idProp);
            EditorGUI.EndDisabledGroup();
        }

        if (_statBoostsProp != null)
            EditorGUILayout.PropertyField(_statBoostsProp, new GUIContent("Stat Boosts"), true);
        if (_displayNameProp != null)
            EditorGUILayout.PropertyField(_displayNameProp);
        if (_descriptionProp != null)
            EditorGUILayout.PropertyField(_descriptionProp);
        if (_costProp != null)
            EditorGUILayout.PropertyField(_costProp);
        if (_iconProp != null)
            EditorGUILayout.PropertyField(_iconProp);
        if (_qualityTierProp != null)
            EditorGUILayout.PropertyField(_qualityTierProp);

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
