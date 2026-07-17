// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization.FileResources;

internal sealed record FileStringLocalizerSettings(
    string RootDirectory,
    string BasePath,
    string ContainingDirectory,
    string BaseFileName,
    IReadOnlyList<LocalizationFileFormat> FormatPrecedence,
    bool WatchForChanges,
    TimeSpan ReloadDebounce,
    int ReloadRetryCount,
    TimeSpan ReloadRetryDelay,
    long MaximumFileSizeBytes,
    bool AllowJsonComments,
    bool AllowTrailingCommas,
    MissingLocalizationFileBehavior MissingFileBehavior,
    TimeProvider TimeProvider,
    Action<FileLocalizationStatus>? StatusChanged)
{
    internal static FileStringLocalizerSettings Create(FileStringLocalizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.RootDirectory))
            throw new ArgumentException("RootDirectory is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.BaseName))
            throw new ArgumentException("BaseName is required.", nameof(options));

        ArgumentNullException.ThrowIfNull(options.FormatPrecedence);
        ArgumentNullException.ThrowIfNull(options.TimeProvider);

        if (Path.IsPathRooted(options.BaseName) ||
            options.BaseName[0] is '/' or '\\' ||
            options.BaseName.Contains(':'))
            throw new ArgumentException("BaseName must be a root-relative path without alternate stream syntax.",
                nameof(options));

        if (options.ReloadDebounce <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "ReloadDebounce must be positive.");

        if (options.ReloadRetryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "ReloadRetryCount cannot be negative.");

        if (options.ReloadRetryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "ReloadRetryDelay cannot be negative.");

        if (options.MaximumFileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumFileSizeBytes must be positive.");

        if (!Enum.IsDefined(options.MissingFileBehavior))
            throw new ArgumentOutOfRangeException(nameof(options), "Unknown missing-file behavior.");

        LocalizationFileFormat[] precedence = options.FormatPrecedence.ToArray();
        if (precedence.Length == 0)
            throw new ArgumentException("FormatPrecedence must contain at least one format.", nameof(options));

        if (precedence.Any(format => !Enum.IsDefined(format)))
            throw new ArgumentOutOfRangeException(nameof(options), "FormatPrecedence contains an unknown format.");

        if (precedence.Distinct().Count() != precedence.Length)
            throw new ArgumentException("FormatPrecedence cannot contain duplicate formats.", nameof(options));

        var root = Path.GetFullPath(options.RootDirectory);
        var normalizedBaseName = options.BaseName
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        if (normalizedBaseName is "." or ".." || normalizedBaseName.EndsWith(Path.DirectorySeparatorChar))
            throw new ArgumentException("BaseName must identify a file base rather than a directory.", nameof(options));

        var basePath = Path.GetFullPath(Path.Combine(root, normalizedBaseName));
        var relative = Path.GetRelativePath(root, basePath);
        if (relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
            throw new ArgumentException("BaseName resolves outside RootDirectory.", nameof(options));

        var containingDirectory = Path.GetDirectoryName(basePath);
        var baseFileName = Path.GetFileName(basePath);
        if (string.IsNullOrEmpty(containingDirectory) || string.IsNullOrWhiteSpace(baseFileName))
            throw new ArgumentException("BaseName must identify a file base inside RootDirectory.", nameof(options));

        if (Path.HasExtension(baseFileName) &&
            (baseFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
             baseFileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("BaseName must not include a resource extension.", nameof(options));

        if (options.WatchForChanges && !Directory.Exists(containingDirectory))
            throw new DirectoryNotFoundException(
                $"The watched localization directory does not exist: {containingDirectory}");

        return new FileStringLocalizerSettings(
            root,
            basePath,
            containingDirectory,
            baseFileName,
            precedence,
            options.WatchForChanges,
            options.ReloadDebounce,
            options.ReloadRetryCount,
            options.ReloadRetryDelay,
            options.MaximumFileSizeBytes,
            options.AllowJsonComments,
            options.AllowTrailingCommas,
            options.MissingFileBehavior,
            options.TimeProvider,
            options.StatusChanged);
    }
}
