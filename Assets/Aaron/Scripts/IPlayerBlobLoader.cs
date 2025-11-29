#nullable enable

public interface IPlayerBlobLoader
{
    public bool TryLoadPlayerBlob(out PlayerBlob blob);
}
