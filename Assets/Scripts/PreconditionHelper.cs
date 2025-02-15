#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

public static class PreconditionHelper
{
    public static void ThrowIfNull([NotNull] this object? nullableObject, string objectName)
    {
        if (nullableObject is null)
        {
            throw new NullReferenceException($"Hey STOP! \"{objectName}\" should NOT be null at this point!");
        }
    }
}
