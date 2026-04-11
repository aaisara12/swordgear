#nullable enable

using System;
using UnityEngine;

public class TutorialSection : MonoBehaviour
{
    public GameObject? EnterGate;
    public GameObject? ExitGate;

    public void Awake()
    {
        EnterGate.ThrowIfNull(nameof(EnterGate));

        EnterGate.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        EnterGate.ThrowIfNull(nameof(EnterGate));
        
        EnterGate.SetActive(true);
    }
}
