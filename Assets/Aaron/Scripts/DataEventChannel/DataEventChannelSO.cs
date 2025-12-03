#nullable enable

using System;
using UnityEngine;

public class DataEventChannelSO<T> : ScriptableObject
{
    public event Action<T>? OnDataChanged;

    private T? mostRecentData;

    public T? GetMostRecentData => mostRecentData;
    
    public void RaiseDataChanged(T newData)
    {
        mostRecentData = newData;
        OnDataChanged?.Invoke(newData);
    }
}