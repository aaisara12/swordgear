#nullable enable

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// Custom editor for Shop.ElementUpgradeLoadableStoreItem
[CustomEditor(typeof(Shop.ElementUpgradeLoadableStoreItem))]
public class ElementUpgradeLoadableStoreItemEditor : Editor
{
    // LoadableStoreItem inherited properties
    private SerializedProperty? scriptProp;
    private SerializedProperty? idProp;
    private SerializedProperty? displayNameProp;
    private SerializedProperty? descriptionProp;
    private SerializedProperty? costProp;
    private SerializedProperty? iconProp;
    
    // ElementUpgradeLoadableStoreItem specific property
    private SerializedProperty? elementUpgradeProp;

    private void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");
        elementUpgradeProp = serializedObject.FindProperty("elementUpgrade");
        idProp = serializedObject.FindProperty("id");

        // inherited properties
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionProp = serializedObject.FindProperty("description");
        costProp = serializedObject.FindProperty("cost");
        iconProp = serializedObject.FindProperty("icon");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Compute id from the selected enum value. Fall back to empty string on error.
        string computedId = string.Empty;
        if (elementUpgradeProp != null)
        {
            try
            {
                int enumIndex = elementUpgradeProp.enumValueIndex;
                if (enumIndex >= 0 && enumIndex < elementUpgradeProp.enumNames.Length)
                {
                    string enumName = elementUpgradeProp.enumNames[enumIndex];
                    var upgrade = (UpgradeType)System.Enum.Parse(typeof(UpgradeType), enumName);
                    computedId = UpgradeTypeSerializer.Serialize(upgrade);
                }
            }
            catch (System.Exception)
            {
                computedId = string.Empty;
            }
        }
        
        // Show the Script reference (readonly) to match Unity's default inspector
        if (scriptProp != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            // Fallback: show a non-editable object field pointing to the MonoScript for clarity
            var targetObj = target as ScriptableObject;
            if (targetObj != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(targetObj), typeof(MonoScript), false);
                EditorGUI.EndDisabledGroup();
            }
        }
        
        if (idProp != null)
        {
            // Persist the computed id into the serialized "id" field on the object so it's saved with the asset
            idProp.stringValue = computedId;
            
            // Show the id as read-only in the inspector
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Id", computedId);
            EditorGUI.EndDisabledGroup();
        }
        else
            EditorGUILayout.LabelField("id property not found.");
        
        if (displayNameProp != null)
            EditorGUILayout.PropertyField(displayNameProp);
        else
            EditorGUILayout.LabelField("displayName property not found.");

        if (descriptionProp != null)
            EditorGUILayout.PropertyField(descriptionProp);
        else
            EditorGUILayout.LabelField("description property not found.");

        if (costProp != null)
            EditorGUILayout.PropertyField(costProp);
        else
            EditorGUILayout.LabelField("cost property not found.");

        if (iconProp != null)
            EditorGUILayout.PropertyField(iconProp);
        else
            EditorGUILayout.LabelField("icon property not found.");
        
        if (elementUpgradeProp != null)
            EditorGUILayout.PropertyField(elementUpgradeProp);
        else
            EditorGUILayout.LabelField("elementUpgrade property not found.");

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
