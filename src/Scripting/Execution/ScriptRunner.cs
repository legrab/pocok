// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using System.Globalization;
using System.Text.Json;
using Pocok.Conversion;

namespace Pocok.Scripting.Execution;

/// <summary>Validates and runs source through an explicitly registered engine.</summary>
public sealed class ScriptRunner
{
    private readonly ScriptEngineRegistry _registry;
    private readonly IValueConverter _converter;

    /// <summary>Creates a runner over an explicit registry.</summary>
    public ScriptRunner(ScriptEngineRegistry registry, IValueConverter? converter = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _converter = converter ?? ValueConverter.Default;
    }

    /// <summary>Validates and runs a script.</summary>
    public async ValueTask<ScriptResult<object?>> ExecuteAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        options ??= new ScriptExecutionOptions();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Source.Length > options.MaxSourceCharacters)
            return Failure("scripting.source.too_large", $"Source exceeds {options.MaxSourceCharacters} characters.");
        if (request.Bindings is null || request.Bindings.Any(static item => item is null))
            return Failure("scripting.bindings.invalid", "Bindings must be an explicit non-null collection.");
        if (request.Bindings.GroupBy(static item => item.Name, StringComparer.Ordinal).Any(static group => group.Skip(1).Any()))
            return Failure("scripting.binding.duplicate", "Binding names must be unique.");
        if (!_registry.TryGet(request.EngineId, out IScriptEngineAdapter adapter))
            return Failure("scripting.engine.unknown", $"Engine '{request.EngineId}' is not registered.");
        if (!adapter.Descriptor.IsAvailable)
            return Failure(adapter.Descriptor.UnavailableCode ?? "scripting.engine.unavailable",
                adapter.Descriptor.UnavailableMessage ?? $"Engine '{request.EngineId}' is unavailable.");

        ScriptFailure? unsupported = CheckCapabilities(adapter.Descriptor.Capabilities, options);
        if (unsupported is not null) return ScriptResult.Failed<object?>(unsupported);

        ScriptValidationResult validation;
        try
        {
            validation = await adapter.Validator.ValidateAsync(request, options, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("scripting.validation.failed", "Script validation failed without exposing internal details.");
        }
        if (!validation.IsValid)
        {
            ScriptValidationDiagnostic first = validation.Diagnostics.First(static item => item.Severity == ScriptValidationSeverity.Error);
            return ScriptResult.Failed<object?>(new ScriptFailure(first.Code, first.Message, first.Line, first.Column, validation.Diagnostics));
        }

        ScriptResult<object?> raw;
        try
        {
            raw = await adapter.ExecuteAsync(new ValidatedScript(request), options, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("scripting.execution.failed", "The script engine failed without exposing internal details.");
        }

        if (!raw.IsSuccess) return raw;

        object? normalized;
        try
        {
            normalized = Normalize(raw.Value);
            if (request.ExpectResult && normalized is null)
                return Failure("scripting.result.missing", "The script was expected to return a value.");

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
                normalized,
                normalized?.GetType() ?? typeof(object));
            if (bytes.Length > options.MaxOutputBytes)
                return Failure("scripting.output.too_large", $"Serialized output exceeds {options.MaxOutputBytes} bytes.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return Failure("scripting.result.unsupported", "The engine returned a value that cannot cross the bounded script boundary.");
        }

        return ScriptResult.Success<object?>(normalized);
    }

    /// <summary>Runs a script and converts its result through Pocok.Conversion.</summary>
    public async ValueTask<ScriptResult<T>> ExecuteAsync<T>(
        ScriptExecutionRequest request,
        ScriptExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ScriptResult<object?> raw = await ExecuteAsync(request, options, cancellationToken).ConfigureAwait(false);
        if (!raw.IsSuccess) return ScriptResult.Failed<T>(raw.Failure!);
        if (!request.ExpectResult) return ScriptResult.Success<T>();
        if (raw.Value is T typed) return ScriptResult.Success(typed);
        ConversionResult<T> converted = _converter.Convert<T>(raw.Value, new ConversionContext(CultureInfo.InvariantCulture));
        return converted.IsSuccess
            ? ScriptResult.Success(converted.Value)
            : ScriptResult.Failed<T>(new ScriptFailure("scripting.result.conversion",
                converted.Error?.Message ?? $"The result could not be converted to {typeof(T).Name}."));
    }

    private static object? Normalize(object? value)
    {
        if (value is null or string or bool or char or
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return value;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt64(out long integer) => integer,
                JsonValueKind.Number => element.GetDouble(),
                _ => element.Clone()
            };
        }

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType());
        using JsonDocument document = JsonDocument.Parse(bytes);
        return document.RootElement.Clone();
    }

    private static ScriptFailure? CheckCapabilities(ScriptEngineCapabilities capabilities, ScriptExecutionOptions options)
    {
        if (options.MaxStatements.HasValue && !capabilities.EnforcesStatementLimit)
            return new("scripting.limit.unsupported", "The selected engine cannot enforce a statement limit.");
        if (options.MaxRecursionDepth.HasValue && !capabilities.EnforcesRecursionLimit)
            return new("scripting.limit.unsupported", "The selected engine cannot enforce a recursion limit.");
        if (options.MaxMemoryBytes.HasValue && !capabilities.EnforcesMemoryLimit)
            return new("scripting.limit.unsupported", "The selected engine cannot enforce a memory limit.");
        if (!capabilities.EnforcesHardTimeout)
            return new("scripting.limit.unsupported", "The selected engine cannot enforce the mandatory timeout.");
        if (!capabilities.EnforcesCancellation)
            return new("scripting.limit.unsupported", "The selected engine cannot enforce cancellation.");
        return null;
    }

    private static ScriptResult<object?> Failure(string code, string message) =>
        ScriptResult.Failed<object?>(new ScriptFailure(code, message));
}
