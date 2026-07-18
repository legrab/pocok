// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Scripting.Models;

public sealed record ScriptingInput
{
    public string SampleId { get; init; } = "arithmetic";

    public string Script { get; init; } = """
        function add(left, right) { return left + right; }
        add(20, 22);
        """;

    public bool ExpectResult { get; init; } = true;
    public int TimeoutMilliseconds { get; init; } = 1_000;
    public int MaxStatements { get; init; } = 10_000;
    public int MaxRecursionDepth { get; init; } = 64;
    public int MaxMemoryMegabytes { get; init; } = 16;
}

public sealed record ScriptingOutput(
    bool IsSuccess,
    string Headline,
    string? Result,
    string ResultType,
    bool ExpectResult,
    string? FailureCode,
    string? FailureMessage,
    int? FailureLine,
    int? FailureColumn,
    string ScriptPreview,
    string LimitsSummary,
    IReadOnlyList<string> TipKeys);
