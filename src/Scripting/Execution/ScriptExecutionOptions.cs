// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting.Execution;

/// <summary>Bounds one script execution.</summary>
public sealed record ScriptExecutionOptions
{
    /// <summary>Gets the maximum wall-clock execution interval.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets the maximum number of JavaScript statements.</summary>
    public int MaxStatements { get; init; } = 100_000;

    /// <summary>Gets the maximum JavaScript recursion depth.</summary>
    public int MaxRecursionDepth { get; init; } = 128;

    /// <summary>Gets the maximum UTF-16 source length accepted for one execution.</summary>
    public int MaxScriptLength { get; init; } = 1_000_000;

    /// <summary>Gets the maximum managed memory budget exposed to the JavaScript engine.</summary>
    public long MaxMemoryBytes { get; init; } = 64 * 1024 * 1024;

    internal void Validate()
    {
        if (Timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Timeout));
        if (MaxStatements <= 0) throw new ArgumentOutOfRangeException(nameof(MaxStatements));
        if (MaxRecursionDepth <= 0) throw new ArgumentOutOfRangeException(nameof(MaxRecursionDepth));
        if (MaxScriptLength <= 0) throw new ArgumentOutOfRangeException(nameof(MaxScriptLength));
        if (MaxMemoryBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxMemoryBytes));
    }
}
