// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization;

/// <summary>Configures one external file-backed localization resource set.</summary>
public sealed record FileStringLocalizerOptions
{
    /// <summary>Gets the root directory that contains the resource base set.</summary>
    public string RootDirectory { get; init; } = string.Empty;

    /// <summary>Gets the root-relative resource base name without culture suffix or extension.</summary>
    public string BaseName { get; init; } = string.Empty;

    /// <summary>Gets formats from highest to lowest precedence for same-culture key conflicts.</summary>
    public IReadOnlyList<LocalizationFileFormat> FormatPrecedence { get; init; } =
        [LocalizationFileFormat.Json, LocalizationFileFormat.Resx];

    /// <summary>Gets whether the containing directory is watched for matching file changes.</summary>
    public bool WatchForChanges { get; init; }

    /// <summary>Gets the quiet period applied to file-system event bursts.</summary>
    public TimeSpan ReloadDebounce { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets the number of retries after the first reload attempt.</summary>
    public int ReloadRetryCount { get; init; } = 3;

    /// <summary>Gets the delay between reload attempts.</summary>
    public TimeSpan ReloadRetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets the maximum accepted size of one resource file.</summary>
    public long MaximumFileSizeBytes { get; init; } = 1_048_576;

    /// <summary>Gets whether JavaScript-style comments are accepted in JSON resources.</summary>
    public bool AllowJsonComments { get; init; }

    /// <summary>Gets whether trailing commas are accepted in JSON resources.</summary>
    public bool AllowTrailingCommas { get; init; }

    /// <summary>Gets how a reload handles a source file that disappeared after a valid load.</summary>
    public MissingLocalizationFileBehavior MissingFileBehavior { get; init; } =
        MissingLocalizationFileBehavior.RetainLastKnownGood;

    /// <summary>Gets the time provider used for retry and debounce delays.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Gets an optional best-effort callback invoked after status is stored.</summary>
    public Action<FileLocalizationStatus>? StatusChanged { get; init; }
}
