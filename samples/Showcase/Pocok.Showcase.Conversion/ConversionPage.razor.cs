// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public partial class ConversionPage
{
    private ConversionEditor? _editor;
    private ConversionInput _input = new();
    private IReadOnlyList<ShowcaseProgressEvent> _progress = [];
    private ShowcaseRunResult? _result;
    private bool _running;
    private long _sampleRevision;
    private string _selectedId = string.Empty;

    [Parameter][EditorRequired] public ShowcasePageContext Context { get; set; } = default!;

    private string SampleResetKey => _sampleRevision.ToString(CultureInfo.InvariantCulture);

    protected override void OnInitialized()
    {
        IShowcaseSample sample = Context.Samples.Single(static item => item.IsDefault);
        SelectSample(sample);
    }

    private string T(string key)
    {
        return Context.Text.GetText("conversion", key);
    }

    private Task SelectSampleAsync(string id)
    {
        IShowcaseSample sample = Context.Samples.Single(item => item.Id == id);
        SelectSample(sample);
        return Task.CompletedTask;
    }

    private void SelectSample(IShowcaseSample sample)
    {
        _selectedId = sample.Id;
        _input = (ConversionInput)sample.CreateInput();
        _sampleRevision = checked(_sampleRevision + 1);
        _progress = [];
        _result = null;
    }

    private Task SetResultAsync(ShowcaseRunResult? result)
    {
        _result = result;
        return Task.CompletedTask;
    }

    private Task SetInputAsync(ConversionInput input)
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
