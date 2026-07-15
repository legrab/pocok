// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Reports one structured discovery or loading event.</summary>
public sealed record ModuleDiagnostic
{
    /// <summary>Initializes a module diagnostic.</summary>
    public ModuleDiagnostic(
        string code,
        string message,
        ModuleDiagnosticSeverity severity,
        Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Severity = severity;
        Exception = exception;
    }

    /// <summary>Gets the stable machine-readable code.</summary>
    public string Code { get; }

    /// <summary>Gets the safe operator-facing message.</summary>
    public string Message { get; }

    /// <summary>Gets the diagnostic severity.</summary>
    public ModuleDiagnosticSeverity Severity { get; }

    /// <summary>Gets optional exception context for local diagnostics.</summary>
    public Exception? Exception { get; }
}
