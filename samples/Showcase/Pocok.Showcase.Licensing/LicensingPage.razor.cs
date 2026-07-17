// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Licensing.Models;

namespace Pocok.Showcase.Licensing;

public partial class LicensingPage
{
    [Parameter, EditorRequired]
    public ShowcasePageContext Context { get; set; } = default!;

    private LicensingInput _input = new();
    private string _selectedId = string.Empty;
    private IReadOnlyList<ShowcaseProgressEvent> _progress = [];
    private ShowcaseRunResult? _result;
    private bool _running;
    private long _sampleRevision;

    private string SampleResetKey => _sampleRevision.ToString(CultureInfo.InvariantCulture);

    protected override void OnInitialized()
    {
        IShowcaseSample sample = Context.Samples.Single(static item => item.IsDefault);
        SelectSample(sample);
    }

    private string T(string key) => Context.Text.GetText("licensing", key);

    private Task SelectSampleAsync(string id)
    {
        IShowcaseSample sample = Context.Samples.Single(item => item.Id == id);
        SelectSample(sample);
        return Task.CompletedTask;
    }

    private void SelectSample(IShowcaseSample sample)
    {
        _selectedId = sample.Id;
        _input = (LicensingInput)sample.CreateInput();
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
