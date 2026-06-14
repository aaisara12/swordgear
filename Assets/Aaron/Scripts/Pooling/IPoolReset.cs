#nullable enable

/// <summary>
/// Optional hook for pooled prefabs whose gameplay state must be reset beyond the default PooledInstance pass.
/// </summary>
public interface IPoolReset
{
    void OnSpawned();
    void OnReleased();
}
