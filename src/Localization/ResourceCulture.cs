// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Pocok.Localization;

/// <summary>Resolves a .NET culture tag from a resource file name.</summary>
public static partial class ResourceCulture
{
    /// <summary>Tries to resolve a culture tag in the final resource-file name segment.</summary>
    public static bool TryGetCultureFromFileName(
        string path,
        [NotNullWhen(true)] out CultureInfo? culture)
    {
        ArgumentNullException.ThrowIfNull(path);

        var fileName = path[(path.LastIndexOfAny(['/', '\\']) + 1)..];
        var stem = Path.GetFileNameWithoutExtension(fileName);
        Match match = CultureTagRegex().Match(stem);
        if (!match.Success)
        {
            culture = null;
            return false;
        }

        try
        {
            culture = CultureInfo.GetCultureInfo(match.Groups["culture"].Value);
            return true;
        }
        catch (CultureNotFoundException)
        {
            culture = null;
            return false;
        }
    }

    /// <summary>Returns the resource-file culture, or the caller-provided fallback.</summary>
    public static CultureInfo GetCultureFromFileName(string path, CultureInfo fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return TryGetCultureFromFileName(path, out CultureInfo? culture) ? culture : fallback;
    }

    [GeneratedRegex(@"\.(?<culture>[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*)$", RegexOptions.CultureInvariant)]
    private static partial Regex CultureTagRegex();
}
