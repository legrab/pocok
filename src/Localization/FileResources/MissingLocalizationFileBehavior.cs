// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization.FileResources;

/// <summary>Defines how a reload handles resource files that were present in the last valid snapshot.</summary>
public enum MissingLocalizationFileBehavior
{
    /// <summary>Reject the reload and retain the complete last-known-good snapshot.</summary>
    RetainLastKnownGood,

    /// <summary>Publish a new snapshot without the missing resources.</summary>
    RemoveMissingResources
}
