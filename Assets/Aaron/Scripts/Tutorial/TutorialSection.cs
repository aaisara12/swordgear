#nullable enable

using System;
using UnityEngine;

namespace Tutorial
{
    public class TutorialSection : MonoBehaviour
    {
        public GameObject? EnterGate;
        public GameObject? ExitGate;

        public void Awake()
        {
            EnterGate.ThrowIfNull(nameof(EnterGate));
            ExitGate.ThrowIfNull(nameof(ExitGate));

            EnterGate.SetActive(false);
            ExitGate.SetActive(true);
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }
            
            EnterGate.ThrowIfNull(nameof(EnterGate));
        
            EnterGate.SetActive(true);
        }
    
        public void CompleteSection()
        {
            ExitGate.ThrowIfNull(nameof(ExitGate));
        
            ExitGate.SetActive(false);
        }
    }
}
