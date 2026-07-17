// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Localization;

namespace Pocok.Localization;

/// <summary>
/// Composes standard string localizers with deterministic first-provider precedence.
/// </summary>
public sealed class CompositeStringLocalizer : IStringLocalizer
{
    private readonly IReadOnlyList<IStringLocalizer> _localizers;

    /// <summary>
    /// Initializes a composite over the supplied localizers in precedence order.
    /// </summary>
    public CompositeStringLocalizer(IEnumerable<IStringLocalizer> localizers)
    {
        ArgumentNullException.ThrowIfNull(localizers);

        IStringLocalizer[] materialized = localizers.ToArray();
        if (materialized.Any(localizer => localizer is null))
        {
            throw new ArgumentException("The localizer sequence cannot contain null entries.", nameof(localizers));
        }

        _localizers = materialized;
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            foreach (IStringLocalizer localizer in _localizers)
            {
                LocalizedString candidate = localizer[name];
                if (!candidate.ResourceNotFound)
                {
                    return candidate;
                }
            }

            return Missing(name);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(arguments);

            foreach (IStringLocalizer localizer in _localizers)
            {
                LocalizedString candidate = localizer[name, arguments];
                if (!candidate.ResourceNotFound)
                {
                    return candidate;
                }
            }

            return Missing(name);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (IStringLocalizer localizer in _localizers)
        {
            foreach (LocalizedString candidate in localizer.GetAllStrings(includeParentCultures))
            {
                if (!candidate.ResourceNotFound && seen.Add(candidate.Name))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static LocalizedString Missing(string name) => new(name, name, true);
}
