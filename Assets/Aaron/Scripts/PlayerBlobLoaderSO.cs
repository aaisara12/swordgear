#nullable enable

using UnityEngine;

public abstract class PlayerBlobLoaderSO : ScriptableObject
{
    // aisara => nullable instead of TryGet pattern since PlayerBlob may be large and expensive to allocate default instance
    public abstract bool TryLoad(out PlayerBlob? blob);
}