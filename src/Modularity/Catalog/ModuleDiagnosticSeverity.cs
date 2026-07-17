// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity.Catalog;

/// <summary>Classifies module discovery and loading diagnostics.</summary>
public enum ModuleDiagnosticSeverity
{
    /// <summary>Informational discovery detail.</summary>
    Information = 0,

    /// <summary>Non-fatal condition that deserves operator attention.</summary>
    Warning = 1,

    /// <summary>Validation or loading failure.</summary>
    Error = 2
}
