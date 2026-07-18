// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Components;

internal sealed class BufferedEditorValue
{
    private string _parameterValue = string.Empty;
    private bool _initialized;

    public string CurrentValue { get; private set; } = string.Empty;

    public string RenderedValue { get; private set; } = string.Empty;

    public int Revision { get; private set; }

    public bool HasUncommittedInput { get; private set; }

    public bool SetParameter(string value)
    {
        if (!_initialized)
        {
            _initialized = true;
            _parameterValue = value;
            CurrentValue = value;
            RenderedValue = value;
            return true;
        }

        if (string.Equals(_parameterValue, value, StringComparison.Ordinal))
            return false;

        _parameterValue = value;
        if (string.Equals(CurrentValue, value, StringComparison.Ordinal))
        {
            HasUncommittedInput = false;
            return false;
        }

        CurrentValue = value;
        RenderedValue = value;
        HasUncommittedInput = false;
        Revision++;
        return true;
    }

    public void SetInput(string value)
    {
        CurrentValue = value;
        HasUncommittedInput = true;
    }

    public void MarkCommitted(string value)
    {
        if (string.Equals(CurrentValue, value, StringComparison.Ordinal))
            HasUncommittedInput = false;
    }
}

internal sealed class DebouncedValueCommitter<T>(
    TimeSpan delay,
    Func<T, Task> commitAsync) : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _pending;
    private bool _disposed;

    public async Task ScheduleAsync(T value)
    {
        var current = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            previous = _pending;
            _pending = current;
        }

        previous?.Cancel();

        try
        {
            await Task.Delay(delay, current.Token);
            await commitAsync(value);
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_pending, current))
                    _pending = null;
            }

            current.Dispose();
        }
    }

    public async Task FlushAsync(T value)
    {
        CancelPending();
        await commitAsync(value);
    }

    public void CancelPending()
    {
        CancellationTokenSource? pending;
        lock (_gate)
        {
            pending = _pending;
            _pending = null;
        }

        pending?.Cancel();
    }

    public void Dispose()
    {
        CancellationTokenSource? pending;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            pending = _pending;
            _pending = null;
        }

        pending?.Cancel();
    }
}
