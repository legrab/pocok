// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pocok.Showcase.Components;

public partial class ShowcaseMonacoEditor
{
    private static readonly TimeSpan InputDebounceDelay = TimeSpan.FromMilliseconds(500);
    private readonly string _id = $"showcase-monaco-{Guid.NewGuid():N}";
    private readonly BufferedEditorValue _editorValue = new();
    private readonly DebouncedValueCommitter<string> _valueCommitter;
    private StandaloneCodeEditor? _editor;
    private ShowcaseBufferedTextArea? _fallbackEditor;
    private string _modelKey = string.Empty;
    private bool _fallback;
    private bool _rendered;

    public ShowcaseMonacoEditor()
    {
        _valueCommitter = new DebouncedValueCommitter<string>(InputDebounceDelay, CommitValueAsync);
    }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Parameter]
    public string Label { get; set; } = "Script source";

    [Parameter]
    public string Language { get; set; } = "plaintext";

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public long ResetRevision { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseMonacoCompletion> Completions { get; set; } = [];

    [Parameter]
    public string FallbackDiagnostic { get; set; } =
        "The enhanced editor is unavailable. A bounded textarea is active instead.";

    protected override void OnParametersSet()
    {
        string nextModelKey = $"{NormalizeLanguage(Language)}:{ResetRevision}";
        if (!string.Equals(_modelKey, nextModelKey, StringComparison.Ordinal))
        {
            _modelKey = nextModelKey;
            _ = _editorValue.SetParameter(Value);
            _valueCommitter.CancelPending();
            _rendered = false;
        }
        else if (_editorValue.SetParameter(Value))
        {
            _valueCommitter.CancelPending();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_fallback || _rendered)
            return;

        try
        {
            bool ready = await JavaScript.InvokeAsync<bool>("pocokMonaco.isReady");
            if (!ready)
            {
                _fallback = true;
                StateHasChanged();
                return;
            }

            await JavaScript.InvokeVoidAsync(
                "pocokMonaco.configure",
                _id,
                NormalizeLanguage(Language),
                Completions);
            _rendered = true;
        }
        catch (JSException)
        {
            _fallback = true;
            StateHasChanged();
        }
        catch (InvalidOperationException)
        {
            _fallback = true;
            StateHasChanged();
        }
    }

    /// <summary>Flushes the latest browser-owned value before an explicit action.</summary>
    public async Task FlushAsync() => _ = await FlushAndGetValueAsync();

    /// <summary>Flushes and returns the latest browser-owned value.</summary>
    public async Task<string> FlushAndGetValueAsync()
    {
        if (_fallback)
            return _fallbackEditor is null ? Value : await _fallbackEditor.FlushAsync();
        if (_editor is null)
            return Value;

        string current = await _editor.GetValue();
        _editorValue.SetInput(current);
        if (_editorValue.HasUncommittedInput)
            await _valueCommitter.FlushAsync(current);
        return current;
    }

    private StandaloneEditorConstructionOptions CreateOptions(StandaloneCodeEditor editor) => new()
    {
        AutomaticLayout = true,
        AriaLabel = Label,
        Language = NormalizeLanguage(Language),
        Value = Value,
        WordWrap = "on",
        ScrollBeyondLastLine = false
    };

    private async Task OnContentChangedAsync()
    {
        if (_editor is null)
            return;

        string current = await _editor.GetValue();
        _editorValue.SetInput(current);
        await _valueCommitter.ScheduleAsync(current);
    }

    private async Task CommitValueAsync(string value)
    {
        if (!string.Equals(_editorValue.CurrentValue, value, StringComparison.Ordinal))
            return;

        await ValueChanged.InvokeAsync(value);
        _editorValue.MarkCommitted(value);
    }

    private static string NormalizeLanguage(string language) => language switch
    {
        "javascript" => "javascript",
        "csharp" => "csharp",
        "python" => "python",
        _ => "plaintext"
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            await FlushAsync();
        }
        catch (Exception)
        {
            // Disposal preserves the last successfully committed value.
        }

        try
        {
            await JavaScript.InvokeVoidAsync("pocokMonaco.dispose", _id);
        }
        catch (Exception)
        {
            // Browser teardown or circuit loss already released the client resources.
        }

        _valueCommitter.Dispose();
        GC.SuppressFinalize(this);
    }
}
