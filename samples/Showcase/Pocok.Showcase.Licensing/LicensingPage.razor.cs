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

    private LicensingEditor? _editor;
    private LicensingInput _input = new();
    private string _selectedId = string.Empty;
    private IReadOnlyList<ShowcaseProgressEvent> _progress = [];
    private ShowcaseRunResult? _result;
    private GeneratedLicenseOutput? _generatedLicense;
    private string? _generationError;
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
        _generatedLicense = null;
        _generationError = null;
    }

    private Task SetInputAsync(LicensingInput input)
    {
        _input = input;
        _generatedLicense = null;
        _generationError = null;
        return Task.CompletedTask;
    }

    private async Task GenerateLicense()
    {
        if (_editor is not null)
            await _editor.FlushAsync();

        try
        {
            _generatedLicense = LicensingShowcaseSlice.GenerateLicense(_input);
            _generationError = null;
        }
        catch (FormatException exception)
        {
            _generatedLicense = null;
            _generationError = $"{T("Generation.Error")} {exception.Message}";
        }
    }

    private async Task<object?> ResolveInputAsync()
    {
        if (_editor is not null)
            await _editor.FlushAsync();
        return _input;
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
