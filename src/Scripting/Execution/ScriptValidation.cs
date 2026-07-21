// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Classifies one validation diagnostic.</summary>
public enum ScriptValidationSeverity
{
    /// <summary>The source cannot execute.</summary>
    Error,

    /// <summary>The source is accepted but deserves attention.</summary>
    Warning
}

/// <summary>Describes one safe parser or policy diagnostic.</summary>
public sealed record ScriptValidationDiagnostic(
    string Code,
    string Message,
    ScriptValidationSeverity Severity = ScriptValidationSeverity.Error,
    int? Line = null,
    int? Column = null);

/// <summary>Contains a fail-closed validation outcome.</summary>
public sealed class ScriptValidationResult
{
    private ScriptValidationResult(IReadOnlyList<ScriptValidationDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>Gets whether validation produced no errors.</summary>
    public bool IsValid => Diagnostics.All(static item => item.Severity != ScriptValidationSeverity.Error);

    /// <summary>Gets bounded safe diagnostics.</summary>
    public IReadOnlyList<ScriptValidationDiagnostic> Diagnostics { get; }

    /// <summary>Creates a successful validation result.</summary>
    public static ScriptValidationResult Valid()
    {
        return new ScriptValidationResult([]);
    }

    /// <summary>Creates a validation result from diagnostics.</summary>
    public static ScriptValidationResult From(IEnumerable<ScriptValidationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new ScriptValidationResult(diagnostics.Take(100).ToArray());
    }
}
