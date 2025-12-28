#nullable enable
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TriggerEventChannelSO", menuName = "Scriptable Objects/Event Channels/Trigger")]
public class TriggerEventChannelSO : ScriptableObject
{
    public event Action? OnEventTriggered;

    public void RaiseEventTriggered()
    {
        OnEventTriggered?.Invoke();
    }
}