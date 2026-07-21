// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Scripting;

public partial class ScriptingPage
{
    private ScriptingEditor? _editor;
    private ScriptingInput _input = new();
    private string _selectedId = string.Empty;
    private IReadOnlyList<ShowcaseProgressEvent> _progress = [];
    private ShowcaseRunResult? _result;
    private bool _running;
    private long _sampleRevision;

    [Parameter, EditorRequired]
    public ShowcasePageContext Context { get; set; } = default!;

    [Inject]
    private ScriptEngineRegistry Registry { get; set; } = default!;

    [Inject]
    private ScriptingShowcaseOptions ShowcaseOptions { get; set; } = default!;

    private IReadOnlyList<ScriptEngineDescriptor> Engines => Registry.Descriptors;
    private string SampleResetKey => _sampleRevision.ToString(CultureInfo.InvariantCulture);

    protected override void OnInitialized() =>
        SelectSample(Context.Samples.Single(static item => item.IsDefault));

    private string T(string key) => Context.Text.GetText("scripting", key);

    private Task SetInputAsync(ScriptingInput input)
    {
        _input = input;
        return Task.CompletedTask;
    }

    private async Task<object?> ResolveInputAsync()
    {
        if (_editor is not null)
            await _editor.FlushAsync();
        return _input;
    }

    private Task SelectSampleAsync(string id)
    {
        SelectSample(Context.Samples.Single(item => item.Id == id));
        return Task.CompletedTask;
    }

    private void SelectSample(IShowcaseSample sample)
    {
        _selectedId = sample.Id;
        var input = (ScriptingInput)sample.CreateInput();
        _input = input with
        {
            TimeoutMilliseconds = Math.Min(input.TimeoutMilliseconds, ShowcaseOptions.MaximumTimeoutMilliseconds),
            MaxStatements = input.MaxStatements is null
                ? null
                : Math.Min(input.MaxStatements.Value, ShowcaseOptions.MaximumStatements),
            MaxRecursionDepth = input.MaxRecursionDepth is null
                ? null
                : Math.Min(input.MaxRecursionDepth.Value, ShowcaseOptions.MaximumRecursionDepth),
            MaxMemoryMegabytes = input.MaxMemoryMegabytes is null
                ? null
                : Math.Min(input.MaxMemoryMegabytes.Value, ShowcaseOptions.MaximumMemoryMegabytes)
        };
        _sampleRevision = checked(_sampleRevision + 1);
        _progress = [];
        _result = null;
    }

    private Task SetResultAsync(ShowcaseRunResult? result)
    {
        _result = result;
        return Task.CompletedTask;
    }

    private Task SetRunningAsync(bool running)
    {
        _running = running;
        return Task.CompletedTask;
    }

    private Task SetProgressAsync(IReadOnlyList<ShowcaseProgressEvent> progress)
    {
        _progress = progress;
        return Task.CompletedTask;
    }
}
