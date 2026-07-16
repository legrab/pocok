// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Represents one ordered observation of a signal value and quality.</summary>
public sealed record SignalSample<T>
{
    /// <summary>Creates a validated signal sample.</summary>
    public SignalSample(
        T? value,
        bool hasValue,
        DateTimeOffset? sourceTimestamp,
        DateTimeOffset observedAt,
        SignalQuality quality,
        long sequence,
        SignalFailure? failure = null)
    {
        if (!Enum.IsDefined(quality))
            throw new ArgumentOutOfRangeException(nameof(quality));
        if (observedAt == default)
            throw new ArgumentException("An observation timestamp is required.", nameof(observedAt));
        if (sourceTimestamp == default(DateTimeOffset))
            throw new ArgumentException("Use null when the source timestamp is unavailable.", nameof(sourceTimestamp));
        if (sequence <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequence), "A sample sequence must be positive.");
        if (!hasValue && value is not null &&
            (!typeof(T).IsValueType || !EqualityComparer<T>.Default.Equals(value, default!)))
            throw new ArgumentException("A sample without a value cannot carry value data.", nameof(value));
        if (quality is SignalQuality.Good or SignalQuality.Stale && !hasValue)
            throw new ArgumentException($"{quality} quality requires a value, including a legitimate null.", nameof(hasValue));
        if (quality is SignalQuality.Unknown or SignalQuality.Disconnected or SignalQuality.Failed && hasValue)
            throw new ArgumentException($"{quality} quality cannot carry a value.", nameof(hasValue));
        if (quality is SignalQuality.Failed && failure is null)
            throw new ArgumentException("Failed quality requires a structured failure.", nameof(failure));
        if (quality is not SignalQuality.Failed && failure is not null)
            throw new ArgumentException("A structured failure is valid only for failed quality.", nameof(failure));

        Value = value;
        HasValue = hasValue;
        SourceTimestamp = sourceTimestamp;
        ObservedAt = observedAt;
        Quality = quality;
        Sequence = sequence;
        Failure = failure;
    }

    /// <summary>Gets the value. Inspect <see cref="HasValue"/> before interpreting it.</summary>
    public T? Value { get; }

    /// <summary>Gets whether the sample contains a value, including a legitimate null.</summary>
    public bool HasValue { get; }

    /// <summary>Gets the optional timestamp supplied by the source.</summary>
    public DateTimeOffset? SourceTimestamp { get; }

    /// <summary>Gets when this sample was observed.</summary>
    public DateTimeOffset ObservedAt { get; }

    /// <summary>Gets the source and runtime quality state.</summary>
    public SignalQuality Quality { get; }

    /// <summary>Gets the positive sequence within the source operation stream.</summary>
    public long Sequence { get; }

    /// <summary>Gets the structured failure for failed quality.</summary>
    public SignalFailure? Failure { get; }
}
