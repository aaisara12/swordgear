#nullable enable
using System;
using UnityEngine;

public class TriggerEventChannelSO : ScriptableObject
{
    public event Action? OnEventTriggered;

    public void RaiseEventTriggered()
    {
        OnEventTriggered?.Invoke();
    }
}