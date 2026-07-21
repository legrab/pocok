// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Validates source for one engine before execution.</summary>
public interface IScriptValidator
{
    /// <summary>Gets the engine validated by this instance.</summary>
    public ScriptEngineId EngineId { get; }

    /// <summary>Validates one request without executing it.</summary>
    public ValueTask<ScriptValidationResult> ValidateAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Executes already validated source for one engine.</summary>
public interface IScriptEngineAdapter
{
    /// <summary>Gets engine identity, availability, and enforceable limits.</summary>
    public ScriptEngineDescriptor Descriptor { get; }

    /// <summary>Gets the fail-closed validator.</summary>
    public IScriptValidator Validator { get; }

    /// <summary>Executes a token created only by <see cref="ScriptRunner" />.</summary>
    public ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents source that passed the selected engine validator.</summary>
public sealed class ValidatedScript
{
    internal ValidatedScript(ScriptExecutionRequest request)
    {
        Request = request;
    }

    /// <summary>Gets the validated request.</summary>
    public ScriptExecutionRequest Request { get; }
}
