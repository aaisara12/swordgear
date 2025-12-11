#nullable enable
#if UNITY_EDITOR
using Shop;
using UnityEditor;

[CustomEditor(typeof(ItemShopModelInSceneGenerator), true)]
public class ItemShopModelInSceneGeneratorEditor : Editor
{
    private SerializedProperty? _eventChannelProp;

    private void OnEnable()
    {
        _eventChannelProp = serializedObject.FindProperty("_eventChannel");
    }

    public override void OnInspectorGUI()
    {
        // Delegate all drawing to the shared utility. It will call ApplyModifiedProperties.
        ItemShopInspectorUtility.DrawInspector(serializedObject, target, _eventChannelProp, overrideCatalog: null, initialStockPropertyName: "_initialStockEntries");
    }
}
#endif
