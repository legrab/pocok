// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Describes an expected script validation or execution failure.</summary>
public sealed record ScriptFailure(
    string Code,
    string Message,
    int? Line = null,
    int? Column = null,
    IReadOnlyList<ScriptValidationDiagnostic>? Diagnostics = null);
