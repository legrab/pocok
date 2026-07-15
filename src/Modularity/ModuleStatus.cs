// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Describes the outcome of one plugin manifest.</summary>
public enum ModuleStatus
{
    /// <summary>The plugin was filtered before assembly loading.</summary>
    Skipped = 0,

    /// <summary>All module entry points registered successfully.</summary>
    Registered = 1,

    /// <summary>The plugin could not be validated, loaded, or registered.</summary>
    Failed = 2
}
