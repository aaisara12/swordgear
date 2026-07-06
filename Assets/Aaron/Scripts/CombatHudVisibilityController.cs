#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hides combat HUD elements outside the Arena and listens to <see cref="BoolEventChannelSO"/>
/// broadcasts from MapSceneController, NodeStarter, and DefeatStateController.
/// </summary>
public class CombatHudVisibilityController : MonoBehaviour
{
    private static readonly string[] HudElementNames =
    {
        "UltUI",
        "TotalPointsText",
        "WaveBanner",
        "MinimapRoot",
        "CombatHUDRoot",
        "PlayerHealthBarRoot",
    };

    [SerializeField] private BoolEventChannelSO? visibilityChannel;

    private readonly List<GameObject> _hudElements = new List<GameObject>();

    private void Awake()
    {
        CacheHudElements();
        ApplyVisibility(false);

        if (visibilityChannel == null)
        {
            Debug.LogError("CombatHudVisibilityController: visibilityChannel is null");
            return;
        }

        visibilityChannel.OnDataChanged += ApplyVisibility;
    }

    private void OnDestroy()
    {
        if (visibilityChannel != null)
        {
            visibilityChannel.OnDataChanged -= ApplyVisibility;
        }
    }

    private void CacheHudElements()
    {
        _hudElements.Clear();

        foreach (string elementName in HudElementNames)
        {
            Transform? child = transform.Find(elementName);
            if (child != null)
            {
                _hudElements.Add(child.gameObject);
            }
        }
    }

    private void ApplyVisibility(bool isVisible)
    {
        foreach (GameObject element in _hudElements)
        {
            if (element != null)
            {
                element.SetActive(isVisible);
            }
        }
    }
}
