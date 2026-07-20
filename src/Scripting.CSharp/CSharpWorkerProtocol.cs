// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;

namespace Pocok.Scripting.CSharp;

internal sealed record CSharpWorkerRequest(
    int ProtocolVersion,
    string Operation,
    string Source,
    bool ExpectResult,
    IReadOnlyDictionary<string, object?> Bindings,
    IReadOnlyList<string> AllowedImports,
    IReadOnlyList<string> AllowedReferencePaths);

internal sealed record CSharpWorkerResponse(
    bool Success,
    JsonElement? Result,
    string? Code,
    string? Message,
    int? Line,
    int? Column,
    IReadOnlyList<CSharpWorkerDiagnostic>? Diagnostics);

internal sealed record CSharpWorkerDiagnostic(
    string Code,
    string Message,
    string Severity,
    int? Line,
    int? Column);
