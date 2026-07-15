// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

/// <summary>Describes whether a custom conversion strategy applied, succeeded, or failed.</summary>
public sealed record ConversionStrategyResult
{
    private ConversionStrategyResult(ConversionStrategyStatus status, object? value, ConversionFailure? failure)
    {
        Status = status;
        Value = value;
        Failure = failure;
    }

    /// <summary>Gets the strategy outcome.</summary>
    public ConversionStrategyStatus Status { get; }

    /// <summary>Gets the converted value when successful.</summary>
    public object? Value { get; }

    /// <summary>Gets the failure when the strategy applied but failed.</summary>
    public ConversionFailure? Failure { get; }

    /// <summary>Returns an outcome indicating that the strategy does not own the conversion.</summary>
    public static ConversionStrategyResult NotApplicable()
    {
        return new ConversionStrategyResult(ConversionStrategyStatus.NotApplicable, null, null);
    }

    /// <summary>Returns a successful custom conversion.</summary>
    public static ConversionStrategyResult Success(object? value)
    {
        return new ConversionStrategyResult(ConversionStrategyStatus.Success, value, null);
    }

    /// <summary>Returns a failed custom conversion.</summary>
    public static ConversionStrategyResult Failed(ConversionFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new ConversionStrategyResult(ConversionStrategyStatus.Failed, null, failure);
    }
}

/// <summary>Identifies a custom strategy outcome.</summary>
public enum ConversionStrategyStatus
{
    /// <summary>The strategy does not own the requested conversion.</summary>
    NotApplicable,

    /// <summary>The strategy converted the value.</summary>
    Success,

    /// <summary>The strategy owned the conversion but rejected or failed it.</summary>
    Failed
}

/// <summary>Specifies whether custom strategies run before or after built-in conversions.</summary>
public enum ConversionStrategyPrecedence
{
    /// <summary>Built-ins retain priority; custom strategies run only after an unsupported built-in result.</summary>
    AfterBuiltIns,

    /// <summary>Custom strategies may intentionally replace built-in behavior.</summary>
    BeforeBuiltIns
}
