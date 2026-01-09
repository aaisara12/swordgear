#nullable enable

using UnityEngine;

public class ElementCollectionView : MonoBehaviour
{
    private class DummyElementCollectionViewController<TData> : IElementCollectionViewController<TData>
    {
        public void AddElement(TData data){}
        public void Clear(){}
    }
    
    [SerializeField] private Transform? _content;

    private bool isInitialized;
    
    public bool TryInitialize<TData, TElementViewModel>(TElementViewModel elementPrefab, out IElementCollectionViewController<TData> elementCollectionViewController) where TElementViewModel : MonoBehaviour, IScrollViewElementInitializable<TData>
    {
        if (isInitialized)
        {
            elementCollectionViewController = new DummyElementCollectionViewController<TData>();
            return false;
        }
        
        _content.ThrowIfNull(nameof(_content));
        
        // Aaron => In the future, we may want to swap this out with a scrollview that pools elements, but for now this will suffice
        elementCollectionViewController = new BasicElementCollectionViewController<TData, TElementViewModel>(_content, elementPrefab);
        isInitialized = true;

        return true;
    }
}
