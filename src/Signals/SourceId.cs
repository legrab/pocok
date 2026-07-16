// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Identifies a signal source using ordinal, case-sensitive equality.</summary>
public readonly record struct SourceId
{
    private readonly string? _value;

    /// <summary>Creates a source identifier without implicit normalization.</summary>
    public SourceId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("A source identifier cannot have surrounding whitespace.", nameof(value));
        if (value.Contains('\0', StringComparison.Ordinal))
            throw new ArgumentException("A source identifier cannot contain a null character.", nameof(value));
        _value = value;
    }

    /// <summary>Gets the identifier text, or an empty string for the default value.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>Gets whether this is the invalid default identifier.</summary>
    public bool IsEmpty => _value is null;

    /// <inheritdoc />
    public override string ToString() => Value;
}
