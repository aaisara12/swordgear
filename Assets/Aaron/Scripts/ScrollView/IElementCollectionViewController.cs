#nullable enable

public interface IElementCollectionViewController<TData>
{
    public void AddElement(TData data);
    public void Clear();
}