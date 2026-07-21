// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Scripting;

public partial class ScriptingEditor
{
    private ShowcaseMonacoEditor? _monaco;

    [Parameter, EditorRequired]
    public ScriptingInput Value { get; set; } = new();

    [Parameter]
    public EventCallback<ScriptingInput> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<ScriptEngineDescriptor> Engines { get; set; } = [];

    [Parameter]
    public long ResetRevision { get; set; }

    [Parameter, EditorRequired]
    public IShowcaseText Text { get; set; } = default!;

    [Parameter]
    public int MaximumSourceCharacters { get; set; } = 4_000;

    [Parameter]
    public int MaximumTimeoutMilliseconds { get; set; } = 2_000;

    [Parameter]
    public int MaximumStatements { get; set; } = 10_000;

    [Parameter]
    public int MaximumRecursionDepth { get; set; } = 64;

    [Parameter]
    public int MaximumMemoryMegabytes { get; set; } = 16;

    private ScriptEngineDescriptor? Selected =>
        Engines.FirstOrDefault(item => item.Id.Value == Value.EngineId);

    private bool SourceLimitExceeded => Value.Source.Length > MaximumSourceCharacters;

    private string SourceLengthText => string.Format(
        CultureInfo.CurrentCulture,
        T(SourceLimitExceeded ? "Sandbox.SourceLengthExceeded" : "Sandbox.SourceLength"),
        Value.Source.Length,
        MaximumSourceCharacters);

    private IReadOnlyList<ShowcaseMonacoCompletion> Completions => Value.EngineId switch
    {
        "javascript" =>
        [
            new("const", "const ${1:name} = ${2:value};", "Declare an immutable local value."),
            new("function", "function ${1:name}(${2:value}) {\n    return ${2:value};\n}", "Insert a function."),
            new("object result", "({ ${1:answer}: ${2:42} });", "Return an object literal.")
        ],
        "csharp" =>
        [
            new("local function", "int ${1:Calculate}(int value) => ${2:value};", "Insert a local function."),
            new("anonymous object", "new { ${1:answer} = ${2:42} }", "Return an anonymous object."),
            new("bindings", "Bindings[\"${1:name}\"]", "Read an explicit worker binding.")
        ],
        "python" =>
        [
            new("function", "def ${1:calculate}(value):\n    return ${2:value}", "Insert a function."),
            new("dictionary", "{\"${1:answer}\": ${2:42}}", "Return a dictionary."),
            new("bindings", "bindings[\"${1:name}\"]", "Read an explicit worker binding.")
        ],
        _ => []
    };

    private string T(string key) => Text.GetText("scripting", key);

    public Task FlushAsync() => _monaco?.FlushAsync() ?? Task.CompletedTask;

    private async Task ChangeEngineAsync(ChangeEventArgs args)
    {
        string currentSource = _monaco is null
            ? Value.Source
            : await _monaco.FlushAndGetValueAsync();
        string next = args.Value?.ToString() ?? ScriptEngineId.JavaScript.Value;
        var sources = new Dictionary<string, string>(Value.Sources, StringComparer.Ordinal)
        {
            [Value.EngineId] = currentSource
        };
        sources.TryGetValue(next, out string? source);

        ScriptEngineDescriptor? descriptor = Engines.FirstOrDefault(item => item.Id.Value == next);
        await UpdateAsync(Value with
        {
            EngineId = next,
            Source = source ?? string.Empty,
            Sources = sources,
            MaxStatements = descriptor?.Capabilities.EnforcesStatementLimit == true ? MaximumStatements : null,
            MaxRecursionDepth = descriptor?.Capabilities.EnforcesRecursionLimit == true ? MaximumRecursionDepth : null,
            MaxMemoryMegabytes = descriptor?.Capabilities.EnforcesMemoryLimit == true ? MaximumMemoryMegabytes : null
        });
    }

    private Task SetSourceAsync(string source)
    {
        var sources = new Dictionary<string, string>(Value.Sources, StringComparer.Ordinal)
        {
            [Value.EngineId] = source
        };
        return UpdateAsync(Value with { Source = source, Sources = sources });
    }

    private static T ReadValue<T>(ChangeEventArgs args, T fallback) =>
        BindConverter.TryConvertTo(args.Value, CultureInfo.InvariantCulture, out T? value) && value is not null
            ? value
            : fallback;

    private Task UpdateAsync(ScriptingInput input) => ValueChanged.InvokeAsync(input);
}
