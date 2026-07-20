// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;

namespace Pocok.Scripting.CSharp.Worker;

public sealed class ScriptGlobals(IReadOnlyDictionary<string, JsonElement> bindings)
{
    public IReadOnlyDictionary<string, JsonElement> Bindings { get; } = bindings;
}
