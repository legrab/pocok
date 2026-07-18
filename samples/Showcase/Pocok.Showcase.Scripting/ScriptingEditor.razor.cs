// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Scripting;

public partial class ScriptingEditor
{
    [Parameter, EditorRequired]
    public ScriptingInput Value { get; set; } = new();

    [Parameter]
    public EventCallback<ScriptingInput> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public ShowcaseCodeAssistCatalog CodeAssist { get; set; } = ShowcaseCodeAssistCatalog.Empty;

    [Parameter, EditorRequired]
    public IShowcaseText Text { get; set; } = default!;

    private static T ReadValue<T>(ChangeEventArgs args, T fallback)
    {
        return BindConverter.TryConvertTo(args.Value, CultureInfo.InvariantCulture, out T? value) && value is not null
            ? value
            : fallback;
    }

    private string T(string key) => Text.GetText("scripting", key);

    private Task SetScriptAsync(string value) => UpdateAsync(Value with { Script = value });

    private Task SetExpectResultAsync(bool value) => UpdateAsync(Value with { ExpectResult = value });

    private Task SetTimeoutAsync(int value) => UpdateAsync(Value with
    {
        TimeoutMilliseconds = Math.Clamp(value, 50, 2_000)
    });

    private Task SetMaxStatementsAsync(int value) => UpdateAsync(Value with
    {
        MaxStatements = Math.Clamp(value, 100, 100_000)
    });

    private Task SetMaxRecursionAsync(int value) => UpdateAsync(Value with
    {
        MaxRecursionDepth = Math.Clamp(value, 8, 128)
    });

    private Task SetMaxMemoryAsync(int value) => UpdateAsync(Value with
    {
        MaxMemoryMegabytes = Math.Clamp(value, 4, 64)
    });

    private Task UpdateAsync(ScriptingInput input) => ValueChanged.InvokeAsync(input);
}
