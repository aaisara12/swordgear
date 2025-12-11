#nullable enable

using System.Collections.Generic;
using Shop;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LoadableStoreItemCatalog", menuName = "Scriptable Objects/LoadableStoreItemCatalog")]
public class LoadableStoreItemCatalog : ScriptableObject, IItemCatalog
{
    [SerializeField] private string _folderToLoadFrom = string.Empty;
    [SerializeField] private List<LoadableStoreItem> _loadedItems = new List<LoadableStoreItem>();

    public IReadOnlyList<IStoreItem> GetItems() => _loadedItems;

    public bool TryFindItemData(string itemId, out IStoreItem storeItemData)
    {
        throw new System.NotImplementedException();
    }
    
#if UNITY_EDITOR
    public void Load()
    {
        LoadFromFolder(_folderToLoadFrom);
    }
    
    private void LoadFromFolder(string folderPath)
    {
        _loadedItems.Clear();

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("LoadableStoreItemLoader: folderPath is empty.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:LoadableStoreItem", new[] { folderPath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<LoadableStoreItem>(path);
            if (asset != null)
                _loadedItems.Add(asset);
        }

        Debug.Log($"Loaded {_loadedItems.Count} LoadableStoreItem(s) from '{folderPath}'.");
    }
#endif
    
}