// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Pocok.Localization.Console;

internal sealed class DictionaryStringLocalizer(params (string Name, string Value)[] entries) : IStringLocalizer
{
    private readonly Dictionary<string, string> _entries =
        entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);

    public LocalizedString this[string name] => Find(name);

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            LocalizedString result = Find(name);
            return result.ResourceNotFound
                ? result
                : new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, result.Value, arguments),
                    false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _entries.Select(entry => new LocalizedString(entry.Key, entry.Value, false));
    }

    private LocalizedString Find(string name)
    {
        return _entries.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, false)
            : new LocalizedString(name, name, true);
    }
}
