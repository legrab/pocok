// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Pocok.Localization;

internal sealed class FileResourceSnapshot
{
    internal FileResourceSnapshot(
        FrozenDictionary<string, FrozenDictionary<string, string>> resources,
        FrozenSet<string> sourcePaths)
    {
        Resources = resources;
        SourcePaths = sourcePaths;
    }

    private FrozenDictionary<string, FrozenDictionary<string, string>> Resources { get; }

    internal FrozenSet<string> SourcePaths { get; }

    internal bool TryGetValue(CultureInfo culture, string name, [NotNullWhen(true)] out string? value)
    {
        foreach (var cultureName in EnumerateCultureNames(culture, includeParents: true))
        {
            if (Resources.TryGetValue(cultureName, out FrozenDictionary<string, string>? entries) &&
                entries.TryGetValue(name, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    internal IEnumerable<KeyValuePair<string, string>> Enumerate(
        CultureInfo culture,
        bool includeParents)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cultureName in EnumerateCultureNames(culture, includeParents))
        {
            if (!Resources.TryGetValue(cultureName, out FrozenDictionary<string, string>? entries))
            {
                continue;
            }

            foreach (KeyValuePair<string, string> entry in entries.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (seen.Add(entry.Key))
                {
                    yield return entry;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateCultureNames(CultureInfo culture, bool includeParents)
    {
        if (!includeParents)
        {
            yield return culture.Name;
            yield break;
        }

        CultureInfo current = culture;
        while (true)
        {
            yield return current.Name;
            if (current.Equals(CultureInfo.InvariantCulture))
            {
                yield break;
            }

            current = current.Parent;
        }
    }
}
