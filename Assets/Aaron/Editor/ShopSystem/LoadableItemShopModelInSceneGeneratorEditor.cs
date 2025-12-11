#nullable enable
#if UNITY_EDITOR
using Shop;
using UnityEditor;


[CustomEditor(typeof(LoadableItemShopModelInSceneGenerator), true)]
public class LoadableItemShopModelInSceneGeneratorEditor : Editor
{
    private SerializedProperty? _catalogProp;
    private SerializedProperty? _eventChannelProp;

    private void OnEnable()
    {
        _catalogProp = serializedObject.FindProperty("catalog");
        _eventChannelProp = serializedObject.FindProperty("_eventChannel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the catalog field on top so designers can assign a catalog directly
        IItemCatalog? overrideCatalog = null;
        if (_catalogProp != null)
        {
            EditorGUILayout.PropertyField(_catalogProp);
            if (_catalogProp.objectReferenceValue != null)
                overrideCatalog = _catalogProp.objectReferenceValue as IItemCatalog;
            
            serializedObject.ApplyModifiedProperties();
        }

        // Delegate the remainder of the inspector to the shared utility which will render the initial stock UI and validation
        ItemShopInspectorUtility.DrawInspector(serializedObject, target, _eventChannelProp, overrideCatalog, initialStockPropertyName: "_initialStockEntries");
    }
}
#endif
