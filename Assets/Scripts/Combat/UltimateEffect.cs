using UnityEngine;

public abstract class UltimateEffect : ScriptableObject, IUltimate
{
    public abstract void ExecuteUlt(int level, Transform player);
}
