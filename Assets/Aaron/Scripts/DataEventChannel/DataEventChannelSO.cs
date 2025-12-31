#nullable enable

using System;
using UnityEngine;

public class DataEventChannelSO<T> : ScriptableObject
{
    public event Action<T>? OnDataChanged;
    
    public void RaiseDataChanged(T newData)
    {
        OnDataChanged?.Invoke(newData);
    }
}