// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Stores an explicit immutable set of script engines.</summary>
public sealed class ScriptEngineRegistry
{
    private readonly IReadOnlyDictionary<ScriptEngineId, IScriptEngineAdapter> _adapters;

    /// <summary>Creates a registry and rejects duplicate identifiers.</summary>
    public ScriptEngineRegistry(IEnumerable<IScriptEngineAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        var map = new Dictionary<ScriptEngineId, IScriptEngineAdapter>();
        foreach (IScriptEngineAdapter adapter in adapters)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            if (adapter.Validator.EngineId != adapter.Descriptor.Id)
                throw new ArgumentException($"Validator and adapter engine IDs differ for {adapter.Descriptor.Id}.", nameof(adapters));
            if (!map.TryAdd(adapter.Descriptor.Id, adapter))
                throw new ArgumentException($"Engine {adapter.Descriptor.Id} is registered more than once.", nameof(adapters));
        }
        _adapters = map;
    }

    /// <summary>Gets descriptors in ordinal ID order.</summary>
    public IReadOnlyList<ScriptEngineDescriptor> Descriptors =>
        _adapters.Values.Select(static item => item.Descriptor).OrderBy(static item => item.Id.Value, StringComparer.Ordinal).ToArray();

    internal bool TryGet(ScriptEngineId id, out IScriptEngineAdapter adapter) => _adapters.TryGetValue(id, out adapter!);
}
