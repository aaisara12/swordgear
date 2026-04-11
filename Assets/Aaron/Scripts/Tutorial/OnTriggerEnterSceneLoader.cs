#nullable enable

using System;
using UnityEngine;

namespace Tutorial
{
    public class OnTriggerEnterSceneLoader : MonoBehaviour
    {
        public SceneReference NextScene = new SceneReference();
        public StringEventChannelSO? LoadNextSceneChannel;
        
        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                LoadNextSceneChannel?.RaiseDataChanged(NextScene.sceneName);
            }
        }
    }
}