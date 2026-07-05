#nullable enable

using System;

/// <summary>
/// DEPRECATED — branching map node kinds replaced by <see cref="RunStepType"/>.
/// </summary>
[Obsolete("DEPRECATED: Branching map node types replaced by RunStepType. Retained for reference.")]
public enum NodeType
{
    Combat,
    Shop,
    Augment,
    Rest,
    Boss
}
