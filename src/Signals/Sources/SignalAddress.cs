// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals.Sources;

/// <summary>Identifies one signal by source and source-defined ordinal path.</summary>
public sealed record SignalAddress
{
    /// <summary>Creates a signal address without changing path casing or separators.</summary>
    public SignalAddress(SourceId source, string path)
    {
        if (source.IsEmpty)
            throw new ArgumentException("A signal address requires a non-default source identifier.", nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!string.Equals(path, path.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("A signal path cannot have surrounding whitespace.", nameof(path));
        if (path.Contains('\0', StringComparison.Ordinal))
            throw new ArgumentException("A signal path cannot contain a null character.", nameof(path));
        Source = source;
        Path = path;
    }

    /// <summary>Gets the signal source.</summary>
    public SourceId Source { get; }

    /// <summary>Gets the source-defined, case-sensitive path.</summary>
    public string Path { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Source}:{Path}";
    }
}
