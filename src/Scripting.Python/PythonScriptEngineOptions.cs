// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Python;

/// <summary>Configures trusted-local CPython discovery and imports.</summary>
public sealed record PythonScriptEngineOptions
{
    /// <summary>Gets the explicit CPython executable.</summary>
    public string? PythonExecutable { get; init; }

    /// <summary>Gets the explicit private worker path.</summary>
    public string? WorkerPath { get; init; }

    /// <summary>Gets allowlisted standard-library imports.</summary>
    public IReadOnlyList<string> AllowedImports { get; init; } = [];
}
