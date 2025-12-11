using System.Collections.Generic;
using Shop;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadableStoreItemCatalog))]
public class LoadableStoreItemCatalogEditor : Editor
{
    private string folderPath;
    
    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();

        var catalog = target as LoadableStoreItemCatalog;
        if (catalog == null)
        {
            EditorGUILayout.LabelField("Target is not a LoadableStoreItemCatalog.");
            return;
        }
        
        // Input for resources subfolder
        SerializedProperty privateStringProperty = serializedObject.FindProperty("_folderToLoadFrom");
        EditorGUILayout.PropertyField(privateStringProperty);

        // Apply changes back to the target MonoBehaviour
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        
        // Button to load from Resources
        if (GUILayout.Button("Load From Folder"))
        {
            catalog.Load();
            AssetDatabase.SaveAssets();
        }

        IReadOnlyList<IStoreItem> items = catalog.GetItems();
        if (items == null || items.Count == 0)
        {
            EditorGUILayout.LabelField("No items loaded.");
            return;
        }

        // Show each item; if it's a UnityEngine.Object show as ObjectField for reference, otherwise show ToString()
        for (int i = 0; i < items.Count; i++)
        {
            IStoreItem item = items[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
            EditorGUILayout.LabelField(item.Id);
            if (item is Object unityObj)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(unityObj, typeof(Object), true);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            
        }
    }
}