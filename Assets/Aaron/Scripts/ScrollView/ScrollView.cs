#nullable enable

using UnityEngine;

public class ScrollView : MonoBehaviour
{
    private class DummyScrollViewController<TData> : IScrollViewController<TData>
    {
        public void AddElement(TData data){}
        public void Clear(){}
    }
    
    [SerializeField] private Transform? _content;

    private bool isInitialized;
    
    public bool TryInitialize<TData, TElementViewModel>(TElementViewModel elementPrefab, out IScrollViewController<TData> scrollViewController) where TElementViewModel : MonoBehaviour, IScrollViewElementInitializable<TData>
    {
        if (isInitialized)
        {
            scrollViewController = new DummyScrollViewController<TData>();
            return false;
        }
        
        _content.ThrowIfNull(nameof(_content));
        
        // Aaron => In the future, we may want to swap this out with a scrollview that pools elements, but for now this will suffice
        scrollViewController = new BasicScrollViewController<TData, TElementViewModel>(_content, elementPrefab);
        isInitialized = true;

        return true;
    }
}
