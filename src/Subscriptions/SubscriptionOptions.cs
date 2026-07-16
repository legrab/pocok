// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Subscriptions;

/// <summary>Configures filtering and mapping for one typed subscription.</summary>
public sealed class SubscriptionOptions<T>
{
    private static readonly Func<object?, bool> AcceptAllObjects = static _ => true;
    private static readonly Func<object?, T?> MapCompatibleValue = static value => value is T typed ? typed : default;
    private static readonly Func<T?, bool> AcceptAllValues = static _ => true;
    private bool _usesDefaultMapper = true;

    /// <summary>Gets the predicate applied before mapping a published value.</summary>
    public Func<object?, bool> ObjectFilter { get; private set; } = AcceptAllObjects;

    /// <summary>Gets the mapper applied to a published value.</summary>
    public Func<object?, T?> ValueMapper { get; private set; } = MapCompatibleValue;

    /// <summary>Gets the predicate applied after mapping a published value.</summary>
    public Func<T?, bool> ValueFilter { get; private set; } = AcceptAllValues;

    /// <summary>Sets the predicate applied before mapping.</summary>
    public SubscriptionOptions<T> WithObjectFilter(Func<object?, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ObjectFilter = filter;
        return this;
    }

    /// <summary>Sets the mapper applied to each published value.</summary>
    public SubscriptionOptions<T> WithValueMapper(Func<object?, T?> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        _usesDefaultMapper = false;
        ValueMapper = mapper;
        return this;
    }

    /// <summary>Sets the predicate applied after mapping.</summary>
    public SubscriptionOptions<T> WithValueFilter(Func<T?, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValueFilter = filter;
        return this;
    }

    internal bool TryMap(object? value, out T? mapped)
    {
        if (_usesDefaultMapper && value is not null && value is not T)
        {
            mapped = default;
            return false;
        }

        mapped = ValueMapper(value);
        return true;
    }
}
