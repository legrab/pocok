// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pocok.Showcase.Components;

public partial class ShowcaseBufferedTextArea : IDisposable
{
    private static readonly TimeSpan InputDebounceDelay = TimeSpan.FromMilliseconds(500);
    private readonly BufferedEditorValue _editorValue = new();
    private readonly DebouncedValueCommitter<string> _valueCommitter;

    public ShowcaseBufferedTextArea()
    {
        _valueCommitter = new DebouncedValueCommitter<string>(InputDebounceDelay, CommitValueAsync);
    }

    [Parameter] public string? Id { get; set; }

    [Parameter] public string Value { get; set; } = string.Empty;

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public void Dispose()
    {
        _valueCommitter.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void OnParametersSet()
    {
        if (_editorValue.SetParameter(Value))
            _valueCommitter.CancelPending();
    }

    /// <summary>Flushes pending browser-owned text before an explicit action.</summary>
    public async Task<string> FlushAsync()
    {
        var current = _editorValue.CurrentValue;
        if (_editorValue.HasUncommittedInput)
            await _valueCommitter.FlushAsync(current);
        return current;
    }

    private Task OnInputAsync(ChangeEventArgs args)
    {
        _editorValue.SetInput(args.Value?.ToString() ?? string.Empty);
        return _valueCommitter.ScheduleAsync(_editorValue.CurrentValue);
    }

    private async Task OnBlurAsync(FocusEventArgs _)
    {
        await FlushAsync();
    }

    private async Task CommitValueAsync(string value)
    {
        if (!string.Equals(_editorValue.CurrentValue, value, StringComparison.Ordinal))
            return;

        await ValueChanged.InvokeAsync(value);
        _editorValue.MarkCommitted(value);
    }
}
