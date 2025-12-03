using UnityEngine;

public class BasicScrollViewController<TData, TElementViewModel> : IScrollViewController<TData> where TElementViewModel : MonoBehaviour, IScrollViewElementInitializable<TData>
{
    private Transform _contentTransform;
    private TElementViewModel _elementPrefab;
    
    public BasicScrollViewController(Transform contentTransform, TElementViewModel elementPrefab)
    {
        _contentTransform = contentTransform;
        _elementPrefab = elementPrefab;
    }

    public void AddElement(TData data)
    {
        var newElementInstance = Object.Instantiate(_elementPrefab, _contentTransform);
        newElementInstance.Initialize(data);
    }

    public void Clear()
    {
        int numberOfExistingChildren = _contentTransform.childCount;
        for (int i = numberOfExistingChildren - 1; i >= 0; i--)
        {
            Object.Destroy(_contentTransform.GetChild(i).gameObject);
        }
    }
}