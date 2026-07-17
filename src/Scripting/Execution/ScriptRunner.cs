// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Pocok.Conversion;

namespace Pocok.Scripting.Execution;

/// <summary>Runs JavaScript with explicit scalar/function bindings and execution bounds.</summary>
public sealed class ScriptRunner(IValueConverter? converter = null)
{
    private readonly IValueConverter _converter = converter ?? ValueConverter.Default;
    private readonly Func<Action<Options>, Engine> _engineFactory = static configure => new Engine(configure);

    /// <summary>Runs a script and returns its untyped JavaScript result.</summary>
    public async Task<ScriptResult<object?>> ExecuteAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        options ??= new ScriptExecutionOptions();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Script.Length > options.MaxScriptLength)
            return ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.script.too_large",
                $"The script exceeds the configured source limit of {options.MaxScriptLength} characters."));

        try
        {
            Engine engine = CreateEngine(request, options, cancellationToken);
            JsValue value = await engine.EvaluateAsync(request.Script, request.Identifier, cancellationToken);
            var result = value.IsNull() || value.IsUndefined() ? null : value.ToObject();
            if (request.ExpectResult && result is null)
                return ScriptResult.Failed<object?>(new ScriptFailure("scripting.result.missing",
                    "The script was expected to return a value."));

            return ScriptResult.Success<object?>(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ScriptResult.Failed<object?>(CreateFailure(exception, request.Script));
        }
    }

    /// <summary>Runs a script and converts its result using Pocok's explicit conversion policies.</summary>
    public async Task<ScriptResult<T>> ExecuteAsync<T>(
        ScriptExecutionRequest request,
        ScriptExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ScriptResult<object?> raw = await ExecuteAsync(request, options, cancellationToken);
        if (!raw.IsSuccess) return ScriptResult.Failed<T>(raw.Failure!);
        if (!request.ExpectResult) return ScriptResult.Success<T>();
        if (raw.Value is T typed) return ScriptResult.Success(typed);

        ConversionResult<T> converted = _converter.Convert<T>(
            raw.Value,
            new ConversionContext(CultureInfo.InvariantCulture));
        return converted.IsSuccess
            ? ScriptResult.Success(converted.Value)
            : ScriptResult.Failed<T>(new ScriptFailure("scripting.result.conversion",
                converted.Error?.Message ?? $"The script result could not be converted to {typeof(T).Name}."));
    }

    private Engine CreateEngine(
        ScriptExecutionRequest request,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken)
    {
        Engine engine = _engineFactory(jint =>
        {
            jint.Strict = true;
            jint.Interop.Enabled = false;
            jint.Interop.AllowGetType = false;
            jint.Host.StringCompilationAllowed = false;
            jint.LimitRecursion(options.MaxRecursionDepth);
            jint.MaxStatements(options.MaxStatements);
            jint.LimitMemory(options.MaxMemoryBytes);
            jint.TimeoutInterval(options.Timeout);
            jint.CancellationToken(cancellationToken);
        });

        foreach (ScriptBinding binding in request.Bindings)
        {
            if (binding.Function is not null)
            {
                engine.SetValue(binding.Name, binding.Function);
                continue;
            }

            switch (binding.Value)
            {
                case null: engine.SetValue(binding.Name, JsValue.Null); break;
                case string value: engine.SetValue(binding.Name, value); break;
                case bool value: engine.SetValue(binding.Name, value); break;
                case char value: engine.SetValue(binding.Name, value.ToString()); break;
                case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    engine.SetValue(binding.Name, Convert.ToDouble(binding.Value, CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported binding type {binding.Value.GetType().FullName}.");
            }
        }

        return engine;
    }

    private static ScriptFailure CreateFailure(Exception exception, string script)
    {
        if (exception is JavaScriptException javascript)
        {
            var line = javascript.Location.Start.Line;
            var column = javascript.Location.Start.Column;
            return new ScriptFailure("scripting.javascript.error", BuildMessage(javascript, script, line, column),
                line, column, exception);
        }

        if (exception is TimeoutException)
            return new ScriptFailure("scripting.execution.timeout",
                "Script execution exceeded its configured timeout.", Exception: exception);

        return new ScriptFailure("scripting.execution.failed", exception.Message, Exception: exception);
    }

    private static string BuildMessage(Exception exception, string script, int line, int column)
    {
        var lines = script.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
        var index = Math.Clamp(line - 1, 0, lines.Length - 1);
        return $"{exception.Message} at line {line}, column {column}: {lines[index].Trim()}";
    }
}
