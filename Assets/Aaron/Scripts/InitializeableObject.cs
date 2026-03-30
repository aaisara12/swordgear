using System;
using UnityEngine;

public abstract class InitializeableObject : ScriptableObject
{
    public void InitializeOnGameStart_Dangerous(PlayerBlob playerBlob)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"Attempted to initialize {name} in edit mode. Initialization is only allowed in play mode. Ignoring.");
            return;
        }
        
        Initialize(playerBlob);
    }

    protected abstract void Initialize(PlayerBlob playerBlob);
    
    protected void ThrowIfNotPlayMode()
    {
        if (!Application.isPlaying)
        {
            throw new Exception("Attempted to access " + name + " in edit mode. This is not allowed because " + name + " is only initialized in play mode. Please ensure that you are only accessing " + name + " at runtime.");
        }
    }
}