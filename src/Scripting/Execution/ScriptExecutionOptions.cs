// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Bounds one script execution.</summary>
public sealed record ScriptExecutionOptions
{
    /// <summary>Gets the maximum wall-clock interval.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets the maximum UTF-16 source length.</summary>
    public int MaxSourceCharacters { get; init; } = 1_000_000;

    /// <summary>Gets the maximum serialized output size.</summary>
    public int MaxOutputBytes { get; init; } = 256 * 1024;

    /// <summary>Gets an optional mandatory statement limit.</summary>
    public int? MaxStatements { get; init; }

    /// <summary>Gets an optional mandatory recursion limit.</summary>
    public int? MaxRecursionDepth { get; init; }

    /// <summary>Gets an optional mandatory memory limit.</summary>
    public long? MaxMemoryBytes { get; init; }

    internal void Validate()
    {
        if (Timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Timeout));
        if (MaxSourceCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(MaxSourceCharacters));
        if (MaxOutputBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxOutputBytes));
        if (MaxStatements is <= 0) throw new ArgumentOutOfRangeException(nameof(MaxStatements));
        if (MaxRecursionDepth is <= 0) throw new ArgumentOutOfRangeException(nameof(MaxRecursionDepth));
        if (MaxMemoryBytes is <= 0) throw new ArgumentOutOfRangeException(nameof(MaxMemoryBytes));
    }
}
