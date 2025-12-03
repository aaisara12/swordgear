#nullable enable

public interface IScrollViewController<TData>
{
    public void AddElement(TData data);
    public void Clear();
}