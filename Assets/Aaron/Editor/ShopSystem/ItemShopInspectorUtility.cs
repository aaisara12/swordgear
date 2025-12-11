#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Shop;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reusable inspector utilities for ItemShopModelInSceneGenerator-derived editors.
/// Provides catalog resolution and drawing of the initial stock list UI + validation.
/// </summary>
public static class ItemShopInspectorUtility
{
    /// <summary>
    /// Resolve an IItemCatalog from a component instance via (in order): overrideCatalog, GetCatalog(), and scanning fields for objects implementing IItemCatalog.
    /// </summary>
    public static IItemCatalog? ResolveCatalog(object? monoTarget, IItemCatalog? overrideCatalog = null)
    {
        if (overrideCatalog != null)
            return overrideCatalog;

        if (monoTarget == null)
            return null;

        // Try calling protected GetCatalog() via reflection
        var getCatalogMethod = monoTarget.GetType().GetMethod("GetCatalog", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (getCatalogMethod != null)
        {
            try
            {
                var result = getCatalogMethod.Invoke(monoTarget, null) as IItemCatalog;
                if (result != null)
                    return result;
            }
            catch
            {
                // ignore
            }
        }

        // Inspect fields for any assigned object implementing IItemCatalog
        var fields = monoTarget.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var f in fields)
        {
            try
            {
                var val = f.GetValue(monoTarget);
                if (val is IItemCatalog ic)
                    return ic;

                if (val is UnityEngine.Object unityObj)
                {
                    if (unityObj is IItemCatalog ic2)
                        return ic2;

                    var so = unityObj as ScriptableObject;
                    if (so != null && so is IItemCatalog ic3)
                        return ic3;
                }
            }
            catch
            {
                // ignore access errors
            }
        }

        return null;
    }

    /// <summary>
    /// Draw the initial stock inspector UI (eventChannel and the _initialStockEntries list) using the provided serializedObject.
    /// This method will call ApplyModifiedProperties() before returning.
    /// </summary>
    public static void DrawInspector(SerializedObject serializedObject, UnityEngine.Object target, SerializedProperty? eventChannelProp = null, IItemCatalog? overrideCatalog = null, string initialStockPropertyName = "_initialStockEntries")
    {
        serializedObject.Update();

        if (eventChannelProp != null)
            EditorGUILayout.PropertyField(eventChannelProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Initial Stock (from Catalog)", EditorStyles.boldLabel);

        IItemCatalog? catalog = ResolveCatalog(target, overrideCatalog);

        if (catalog == null)
        {
            EditorGUILayout.HelpBox("No catalog instance available on this component at edit time. Ensure the concrete component provides a catalog reference or create entries manually by ItemId.", MessageType.Info);
        }
        else
        {
            var items = catalog.GetItems();
            EditorGUILayout.LabelField($"Catalog: {catalog.GetType().Name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Items available: {items.Count}", EditorStyles.miniLabel);
        }

        // Draw the list of entries
        var selectedIds = new List<string>();

        var initialStockProp = serializedObject.FindProperty(initialStockPropertyName);
        if (initialStockProp == null)
        {
            EditorGUILayout.HelpBox("Initial stock list not found on this component.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        for (int i = 0; i < initialStockProp.arraySize; i++)
        {
            var entryProp = initialStockProp.GetArrayElementAtIndex(i);
            var itemIdProp = entryProp.FindPropertyRelative("ItemId");
            var quantityProp = entryProp.FindPropertyRelative("Quantity");

            EditorGUILayout.BeginHorizontal();

            // If we have a catalog, draw a popup showing display names, otherwise a text field for ItemId
            if (catalog != null)
            {
                var items = catalog.GetItems();
                string[] options = new string[items.Count + 1];
                string[] ids = new string[items.Count + 1];
                options[0] = "-- Select Item --";
                ids[0] = string.Empty;
                int currentIndex = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    options[j + 1] = items[j].DisplayName + " (" + items[j].Id + ")";
                    ids[j + 1] = items[j].Id;
                    if (ids[j + 1] == itemIdProp.stringValue)
                        currentIndex = j + 1;
                }

                int newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.MinWidth(150));
                itemIdProp.stringValue = ids[newIndex];
            }
            else
            {
                itemIdProp.stringValue = EditorGUILayout.TextField(itemIdProp.stringValue, GUILayout.MinWidth(150));
            }

            // collect selected ids for validation
            selectedIds.Add(itemIdProp.stringValue);

            EditorGUILayout.PropertyField(quantityProp, GUIContent.none, GUILayout.MaxWidth(60));

            if (GUILayout.Button("-", GUILayout.MaxWidth(24)))
            {
                initialStockProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Item"))
        {
            initialStockProp.arraySize++;
            var newEntry = initialStockProp.GetArrayElementAtIndex(initialStockProp.arraySize - 1);
            newEntry.FindPropertyRelative("ItemId").stringValue = string.Empty;
            newEntry.FindPropertyRelative("Quantity").intValue = 1;
        }

        if (GUILayout.Button("Clear"))
        {
            initialStockProp.arraySize = 0;
        }
        EditorGUILayout.EndHorizontal();

        // Validation: check for duplicates and missing ids
        if (catalog != null)
        {
            var items = catalog.GetItems();
            var availableIds = new HashSet<string>();
            foreach (var it in items) availableIds.Add(it.Id);

            var seen = new HashSet<string>();
            var duplicates = new List<string>();
            var missing = new List<string>();

            foreach (var id in selectedIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!availableIds.Contains(id))
                {
                    if (!missing.Contains(id)) missing.Add(id);
                }

                if (seen.Contains(id))
                {
                    if (!duplicates.Contains(id)) duplicates.Add(id);
                }
                else
                {
                    seen.Add(id);
                }
            }

            if (duplicates.Count > 0)
            {
                EditorGUILayout.HelpBox("Duplicate items selected: " + string.Join(", ", duplicates), MessageType.Warning);
            }

            if (missing.Count > 0)
            {
                EditorGUILayout.HelpBox("Some ItemIds are not present in the current catalog: " + string.Join(", ", missing), MessageType.Error);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

