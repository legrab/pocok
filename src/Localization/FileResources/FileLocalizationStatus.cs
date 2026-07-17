// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization.FileResources;

/// <summary>Describes the latest file-localization load attempt.</summary>
/// <param name="LastAttemptedAt">The time of the latest load attempt.</param>
/// <param name="LastSuccessfulAt">The time of the latest successful snapshot publication.</param>
/// <param name="HasValidSnapshot">Whether lookups have a valid immutable snapshot.</param>
/// <param name="LastError">The latest load or watcher error, or <see langword="null" /> after success.</param>
public sealed record FileLocalizationStatus(
    DateTimeOffset LastAttemptedAt,
    DateTimeOffset? LastSuccessfulAt,
    bool HasValidSnapshot,
    Exception? LastError);
