// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting.CSharp;

/// <summary>Configures trusted-local C# worker discovery and host-owned capabilities.</summary>
public sealed record CSharpScriptEngineOptions
{
    /// <summary>Gets an explicit dotnet host path.</summary>
    public string? DotNetHostPath { get; init; }

    /// <summary>Gets an explicit private worker directory.</summary>
    public string? WorkerDirectory { get; init; }

    /// <summary>Gets additional allowlisted imports.</summary>
    public IReadOnlyList<string> AllowedImports { get; init; } = [];

    /// <summary>Gets additional host-owned metadata reference paths.</summary>
    public IReadOnlyList<string> AllowedReferencePaths { get; init; } = [];
}
