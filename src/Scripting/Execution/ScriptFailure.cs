// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting.Execution;

/// <summary>Describes an expected script execution or import failure.</summary>
public sealed record ScriptFailure(
    string Code,
    string Message,
    int? Line = null,
    int? Column = null,
    Exception? Exception = null);
