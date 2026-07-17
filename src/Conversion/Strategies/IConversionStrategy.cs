// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion.Strategies;

/// <summary>
///     Provides an explicitly registered conversion extension without accessing converter internals or a service locator.
/// </summary>
public interface IConversionStrategy
{
    /// <summary>
    ///     Attempts a conversion and returns not-applicable when the strategy does not own the source and target pair.
    /// </summary>
    public ConversionStrategyResult TryConvert(object? value, Type targetType, ConversionStrategyContext context);
}
