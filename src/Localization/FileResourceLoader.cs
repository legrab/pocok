// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Pocok.Localization;

internal static class FileResourceLoader
{
    internal static FileResourceSnapshot Load(
        FileStringLocalizerSettings settings,
        FileResourceSnapshot? previous)
    {
        IReadOnlyList<LocalizationFileSource> sources = DiscoverSources(settings);
        var sourcePaths = sources.Select(source => source.Path).ToFrozenSet(PathComparer);

        if (previous is not null &&
            settings.MissingFileBehavior == MissingLocalizationFileBehavior.RetainLastKnownGood)
        {
            var missingPath = previous.SourcePaths.FirstOrDefault(path => !sourcePaths.Contains(path));
            if (missingPath is not null)
            {
                throw new IOException($"Localization source '{missingPath}' disappeared; the last-known-good snapshot was retained.");
            }
        }

        var byCultureAndFormat = new Dictionary<(string Culture, LocalizationFileFormat Format), FrozenDictionary<string, string>>();
        foreach (LocalizationFileSource source in sources)
        {
            EnsureSize(source.Path, settings.MaximumFileSizeBytes);
            FrozenDictionary<string, string> entries = source.Format switch
            {
                LocalizationFileFormat.Json => JsonLocalizationParser.Parse(
                    source.Path,
                    settings.AllowJsonComments,
                    settings.AllowTrailingCommas),
                LocalizationFileFormat.Resx => ResxLocalizationParser.Parse(source.Path),
                _ => throw new InvalidOperationException($"Unsupported localization format: {source.Format}")
            };

            if (!byCultureAndFormat.TryAdd((source.CultureName, source.Format), entries))
            {
                throw new FormatException(
                    $"Multiple localization sources resolve to culture '{source.CultureName}' and format '{source.Format}'.");
            }
        }

        var resources = new Dictionary<string, FrozenDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<string, LocalizationFileSource> cultureGroup in
                 sources.GroupBy(source => source.CultureName, StringComparer.OrdinalIgnoreCase))
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (LocalizationFileFormat format in settings.FormatPrecedence)
            {
                if (!byCultureAndFormat.TryGetValue((cultureGroup.Key, format), out FrozenDictionary<string, string>? entries))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> entry in entries)
                {
                    merged.TryAdd(entry.Key, entry.Value);
                }
            }

            resources.Add(cultureGroup.Key, merged.ToFrozenDictionary(StringComparer.Ordinal));
        }

        return new FileResourceSnapshot(
            resources.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            sourcePaths);
    }

    internal static bool MayBeCandidatePath(FileStringLocalizerSettings settings, string path)
    {
        if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), settings.ContainingDirectory, PathComparison))
        {
            return false;
        }

        return TryCreateSource(settings, Path.GetFullPath(path), out _);
    }

    private static List<LocalizationFileSource> DiscoverSources(FileStringLocalizerSettings settings)
    {
        if (!Directory.Exists(settings.ContainingDirectory))
        {
            return [];
        }

        var sources = new List<LocalizationFileSource>();
        foreach (var path in Directory.EnumerateFiles(settings.ContainingDirectory, "*", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, PathComparer))
        {
            if (TryCreateSource(settings, Path.GetFullPath(path), out LocalizationFileSource? source))
            {
                sources.Add(source);
            }
        }

        return sources;
    }

    private static bool TryCreateSource(
        FileStringLocalizerSettings settings,
        string path,
        [NotNullWhen(true)] out LocalizationFileSource? source)
    {
        var extension = Path.GetExtension(path);
        LocalizationFileFormat format;
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            format = LocalizationFileFormat.Json;
        }
        else if (extension.Equals(".resx", StringComparison.OrdinalIgnoreCase))
        {
            format = LocalizationFileFormat.Resx;
        }
        else
        {
            source = null;
            return false;
        }

        if (!settings.FormatPrecedence.Contains(format))
        {
            source = null;
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(path);
        if (stem.Equals(settings.BaseFileName, PathComparison))
        {
            source = new LocalizationFileSource(path, string.Empty, format);
            return true;
        }

        var prefix = $"{settings.BaseFileName}.";
        if (!stem.StartsWith(prefix, PathComparison))
        {
            source = null;
            return false;
        }

        var cultureName = stem[prefix.Length..];
        if (cultureName.Contains('.'))
        {
            source = null;
            return false;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                source = null;
                return false;
            }

            source = new LocalizationFileSource(path, culture.Name, format);
            return true;
        }
        catch (CultureNotFoundException)
        {
            source = null;
            return false;
        }
    }

    private static void EnsureSize(string path, long maximumFileSizeBytes)
    {
        var length = new FileInfo(path).Length;
        if (length > maximumFileSizeBytes)
        {
            throw new FormatException(
                $"Localization file '{path}' is {length} bytes and exceeds the {maximumFileSizeBytes}-byte limit.");
        }
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
