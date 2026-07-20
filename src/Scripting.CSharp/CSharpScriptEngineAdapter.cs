// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;

namespace Pocok.Scripting.CSharp;

/// <summary>Runs validated C# source in a fresh private worker process.</summary>
public sealed class CSharpScriptEngineAdapter : IScriptEngineAdapter
{
    private const int ProtocolVersion = 1;
    private readonly CSharpWorkerClient _client;
    private readonly CSharpScriptEngineOptions _options;

    /// <summary>Creates an adapter.</summary>
    public CSharpScriptEngineAdapter(CSharpScriptEngineOptions? options = null)
    {
        _options = options ?? new CSharpScriptEngineOptions();
        _client = new CSharpWorkerClient(_options);
        (bool available, string? code, string? message) = _client.Availability;
        Descriptor = new ScriptEngineDescriptor(
            ScriptEngineId.CSharp,
            "C#",
            available,
            new ScriptEngineCapabilities(true, true, false, false, false),
            code,
            message);
        Validator = new CSharpScriptValidator(_client, _options);
    }

    /// <inheritdoc />
    public ScriptEngineDescriptor Descriptor { get; }

    /// <inheritdoc />
    public IScriptValidator Validator { get; }

    /// <inheritdoc />
    public async ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(options);

        if (script.Request.Bindings.Any(static item => item.Function is not null))
        {
            return ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.binding.unsupported",
                "Function bindings cannot cross the C# worker boundary."));
        }

        var bindings = script.Request.Bindings.ToDictionary(
            static item => item.Name,
            static item => item.Value,
            StringComparer.Ordinal);
        CSharpWorkerResponse response = await _client.SendAsync(
            new CSharpWorkerRequest(
                ProtocolVersion,
                "execute",
                script.Request.Source,
                script.Request.ExpectResult,
                bindings,
                _options.AllowedImports,
                _options.AllowedReferencePaths),
            options.Timeout,
            options.MaxOutputBytes * 2,
            cancellationToken).ConfigureAwait(false);

        if (response.Success)
            return ScriptResult.Success<object?>(response.Result);

        IReadOnlyList<ScriptValidationDiagnostic>? diagnostics = response.Diagnostics?
            .Select(ToDiagnostic)
            .ToArray();
        return ScriptResult.Failed<object?>(new ScriptFailure(
            response.Code ?? "scripting.csharp.failed",
            response.Message ?? "C# execution failed safely.",
            response.Line,
            response.Column,
            diagnostics));
    }

    private static ScriptValidationDiagnostic ToDiagnostic(CSharpWorkerDiagnostic item) => new(
        item.Code,
        item.Message,
        item.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)
            ? ScriptValidationSeverity.Warning
            : ScriptValidationSeverity.Error,
        item.Line,
        item.Column);

    private sealed class CSharpScriptValidator(
        CSharpWorkerClient client,
        CSharpScriptEngineOptions options) : IScriptValidator
    {
        public ScriptEngineId EngineId => ScriptEngineId.CSharp;

        public async ValueTask<ScriptValidationResult> ValidateAsync(
            ScriptExecutionRequest request,
            ScriptExecutionOptions executionOptions,
            CancellationToken cancellationToken = default)
        {
            if (request.Bindings.Any(static item => item.Function is not null))
            {
                return ScriptValidationResult.From(
                [
                    new ScriptValidationDiagnostic(
                        "scripting.binding.unsupported",
                        "Function bindings cannot cross the C# worker boundary.")
                ]);
            }

            var bindings = request.Bindings.ToDictionary(
                static item => item.Name,
                static item => item.Value,
                StringComparer.Ordinal);
            CSharpWorkerResponse response = await client.SendAsync(
                new CSharpWorkerRequest(
                    ProtocolVersion,
                    "validate",
                    request.Source,
                    request.ExpectResult,
                    bindings,
                    options.AllowedImports,
                    options.AllowedReferencePaths),
                executionOptions.Timeout,
                128 * 1024,
                cancellationToken).ConfigureAwait(false);

            if (response.Success)
                return ScriptValidationResult.Valid();
            if (response.Diagnostics is { Count: > 0 })
                return ScriptValidationResult.From(response.Diagnostics.Select(ToDiagnostic));

            return ScriptValidationResult.From(
            [
                new ScriptValidationDiagnostic(
                    response.Code ?? "scripting.csharp.validation",
                    response.Message ?? "C# validation failed safely.",
                    Line: response.Line,
                    Column: response.Column)
            ]);
        }
    }
}
