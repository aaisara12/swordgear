#nullable enable

using UnityEngine;

/// <summary>
/// Event channel carrying a <see cref="ComboPerformance"/> snapshot (e.g. for the Stage Complete screen).
/// </summary>
[CreateAssetMenu(fileName = "ComboPerformanceEventChannelSO", menuName = "Scriptable Objects/Event Channels/ComboPerformance")]
public class ComboPerformanceEventChannelSO : DataEventChannelSO<ComboPerformance> { }
