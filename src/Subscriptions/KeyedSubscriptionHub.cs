// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Subscriptions;

/// <summary>Provides thread-safe keyed subscriptions over object-valued publications.</summary>
public sealed class KeyedSubscriptionHub<TKey> : IDisposable where TKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TKey, List<IListener>> _listeners;
    private bool _disposed;

    /// <summary>Creates a hub using the supplied key comparer, or the default comparer.</summary>
    public KeyedSubscriptionHub(IEqualityComparer<TKey>? comparer = null)
    {
        _listeners = new Dictionary<TKey, List<IListener>>(comparer);
    }

    /// <summary>Gets a snapshot of keys that currently have one or more subscriptions.</summary>
    public IReadOnlyList<TKey> Keys
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _listeners.Keys.ToArray();
            }
        }
    }

    /// <summary>Adds a typed listener for a key and returns its idempotent removal handle.</summary>
    public IDisposable Subscribe<T>(
        TKey key,
        EventHandler<T?> handler,
        Action<SubscriptionOptions<T>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(handler);
        SubscriptionOptions<T> options = new();
        configure?.Invoke(options);
        Listener<T> listener = new(handler, options);

        lock (_gate)
        {
            ThrowIfDisposed();
            List<IListener> listeners = GetOrCreateListeners(key);
            listeners.Add(listener);
        }

        return new Registration(() => Remove(key, listener));
    }

    /// <summary>Publishes a value to listeners for a key and returns the number of handlers invoked.</summary>
    public int Publish(TKey key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        IListener[] listeners;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_listeners.TryGetValue(key, out List<IListener>? registered))
            {
                return 0;
            }

            listeners = registered.ToArray();
        }

        int delivered = 0;
        foreach (IListener listener in listeners)
        {
            if (listener.Publish(this, value))
            {
                delivered++;
            }
        }

        return delivered;
    }

    /// <summary>Removes all subscriptions and releases the hub.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _listeners.Clear();
        }

        GC.SuppressFinalize(this);
    }

    private List<IListener> GetOrCreateListeners(TKey key)
    {
        if (_listeners.TryGetValue(key, out List<IListener>? listeners))
        {
            return listeners;
        }

        listeners = [];
        _listeners.Add(key, listeners);
        return listeners;
    }

    private void Remove(TKey key, IListener listener)
    {
        lock (_gate)
        {
            if (_disposed || !_listeners.TryGetValue(key, out List<IListener>? listeners))
            {
                return;
            }

            listeners.Remove(listener);
            if (listeners.Count == 0)
            {
                _listeners.Remove(key);
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private interface IListener
    {
        public bool Publish(KeyedSubscriptionHub<TKey> hub, object? value);
    }

    private sealed class Listener<T>(EventHandler<T?> handler, SubscriptionOptions<T> options) : IListener
    {
        public bool Publish(KeyedSubscriptionHub<TKey> hub, object? value)
        {
            if (!options.ObjectFilter(value))
            {
                return false;
            }

            if (!options.TryMap(value, out T? mapped))
            {
                return false;
            }

            if (!options.ValueFilter(mapped))
            {
                return false;
            }

            handler.Invoke(hub, mapped);
            return true;
        }
    }

    private sealed class Registration(Action remove) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                remove();
            }
        }
    }
}
