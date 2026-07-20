// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Components;

public partial class ShowcaseCodeAssistEditor : IDisposable
{
    private static readonly TimeSpan InputDebounceDelay = TimeSpan.FromMilliseconds(500);
    private readonly string _id = $"showcase-editor-{Guid.NewGuid():N}";
    private readonly string _suggestionsId = $"showcase-suggestions-{Guid.NewGuid():N}";
    private readonly BufferedEditorValue _editorValue = new();
    private readonly DebouncedValueCommitter<string> _valueCommitter;
    private IReadOnlyList<ShowcaseCodeAssistItem> _suggestions = Array.Empty<ShowcaseCodeAssistItem>();
    private int _selectedIndex;
    private bool _suggestionsOpen;

    public ShowcaseCodeAssistEditor()
    {
        _valueCommitter = new DebouncedValueCommitter<string>(InputDebounceDelay, CommitValueAsync);
    }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Parameter]
    public string Label { get; set; } = "Constrained code editor";

    [Parameter]
    public string ShowSuggestionsLabel { get; set; } = "Show suggestions";

    [Parameter]
    public string HideSuggestionsLabel { get; set; } = "Hide suggestions";

    [Parameter]
    public string ActionLabel { get; set; } = "Apply";

    [Parameter]
    public EventCallback<string> ActionRequested { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public ShowcaseCodeAssistCatalog Catalog { get; set; } = ShowcaseCodeAssistCatalog.Empty;

    [Parameter, EditorRequired]
    public IShowcaseText Text { get; set; } = default!;

    [Parameter, EditorRequired]
    public string ResourceNamespace { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        if (_editorValue.SetParameter(Value))
        {
            _valueCommitter.CancelPending();
            CloseSuggestions();
        }
    }

    /// <summary>Flushes pending browser-owned text before an explicit action.</summary>
    public async Task<string> FlushAsync()
    {
        string current = _editorValue.CurrentValue;
        if (_editorValue.HasUncommittedInput)
            await _valueCommitter.FlushAsync(current);
        return current;
    }

    private async Task ToggleSuggestionsAsync()
    {
        _suggestionsOpen = !_suggestionsOpen;
        if (_suggestionsOpen)
            await RefreshSuggestionsAsync();
        else
            CloseSuggestions();
    }

    private async Task InvokeActionAsync()
    {
        string current = await FlushAsync();
        CloseSuggestions();
        await ActionRequested.InvokeAsync(current);
    }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        _editorValue.SetInput(args.Value?.ToString() ?? string.Empty);
        if (_suggestionsOpen)
            await RefreshSuggestionsAsync();
        await _valueCommitter.ScheduleAsync(_editorValue.CurrentValue);
    }

    private async Task OnBlurAsync(FocusEventArgs _)
    {
        await FlushAsync();
    }

    private async Task RefreshSuggestionsAsync()
    {
        int cursor = await JavaScript.InvokeAsync<int>("pocokShowcase.getCursor", _id);
        _suggestions = ShowcaseCodeAssistFilter.Filter(Catalog, _editorValue.CurrentValue, cursor, 8);
        _selectedIndex = 0;
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (!_suggestionsOpen || _suggestions.Count == 0)
            return;

        switch (args.Key)
        {
            case "ArrowDown":
                _selectedIndex = (_selectedIndex + 1) % _suggestions.Count;
                break;
            case "ArrowUp":
                _selectedIndex = (_selectedIndex - 1 + _suggestions.Count) % _suggestions.Count;
                break;
            case "Enter" when !args.ShiftKey:
            case "Tab":
                await InsertAsync(_suggestions[_selectedIndex]);
                break;
            case "Escape":
                CloseSuggestions();
                break;
        }
    }

    private async Task InsertAsync(ShowcaseCodeAssistItem item)
    {
        string next = await JavaScript.InvokeAsync<string>("pocokShowcase.insertCompletion", _id, item.InsertText);
        _editorValue.SetInput(next);
        CloseSuggestions();
        await _valueCommitter.FlushAsync(next);
    }

    private async Task CommitValueAsync(string value)
    {
        if (!string.Equals(_editorValue.CurrentValue, value, StringComparison.Ordinal))
            return;

        await ValueChanged.InvokeAsync(value);
        _editorValue.MarkCommitted(value);
    }

    private void CloseSuggestions()
    {
        _suggestionsOpen = false;
        _suggestions = Array.Empty<ShowcaseCodeAssistItem>();
        _selectedIndex = 0;
    }

    public void Dispose()
    {
        _valueCommitter.Dispose();
        GC.SuppressFinalize(this);
    }
}
