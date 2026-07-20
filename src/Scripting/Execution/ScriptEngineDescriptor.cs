// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Declares the resource limits an engine can enforce.</summary>
public sealed record ScriptEngineCapabilities(
    bool EnforcesHardTimeout,
    bool EnforcesCancellation,
    bool EnforcesStatementLimit,
    bool EnforcesRecursionLimit,
    bool EnforcesMemoryLimit);

/// <summary>Describes one configured engine and its current availability.</summary>
public sealed record ScriptEngineDescriptor(
    ScriptEngineId Id,
    string Language,
    bool IsAvailable,
    ScriptEngineCapabilities Capabilities,
    string? UnavailableCode = null,
    string? UnavailableMessage = null);
