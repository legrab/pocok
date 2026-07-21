// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Pocok.Scripting.Execution;

namespace Pocok.Scripting.JavaScript;

/// <summary>Executes validated JavaScript through a fresh restricted Jint engine.</summary>
public sealed class JavaScriptScriptEngineAdapter : IScriptEngineAdapter
{
    private static readonly ScriptEngineCapabilities Capabilities = new(true, true, true, true, true);

    /// <inheritdoc />
    public ScriptEngineDescriptor Descriptor { get; } =
        new(ScriptEngineId.JavaScript, "JavaScript", true, Capabilities);

    /// <inheritdoc />
    public IScriptValidator Validator { get; } = new JavaScriptScriptValidator();

    /// <inheritdoc />
    public ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(options);

        ScriptExecutionRequest request = script.Request;
        try
        {
            var engine = new Engine(jint =>
            {
                jint.Strict = true;
                jint.Interop.Enabled = false;
                jint.Interop.AllowGetType = false;
                jint.Host.StringCompilationAllowed = false;
                jint.LimitRecursion(options.MaxRecursionDepth ?? 128);
                jint.MaxStatements(options.MaxStatements ?? 100_000);
                jint.LimitMemory(options.MaxMemoryBytes ?? 64 * 1024 * 1024);
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
                    case null:
                        engine.SetValue(binding.Name, JsValue.Null);
                        break;
                    case string stringValue:
                        engine.SetValue(binding.Name, stringValue);
                        break;
                    case bool booleanValue:
                        engine.SetValue(binding.Name, booleanValue);
                        break;
                    case char characterValue:
                        engine.SetValue(binding.Name, characterValue.ToString());
                        break;
                    case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                        engine.SetValue(
                            binding.Name,
                            Convert.ToDouble(binding.Value, CultureInfo.InvariantCulture));
                        break;
                    default:
                        return ValueTask.FromResult(ScriptResult.Failed<object?>(new ScriptFailure(
                            "scripting.binding.unsupported",
                            "A binding cannot be represented by JavaScript.")));
                }
            }

            JsValue evaluated = engine.Evaluate(request.Source, request.Identifier);
            var result = evaluated.IsNull() || evaluated.IsUndefined()
                ? null
                : evaluated.ToObject();
            return ValueTask.FromResult(ScriptResult.Success<object?>(result));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return ValueTask.FromResult(ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.execution.timeout",
                "Execution exceeded its configured timeout.")));
        }
        catch (JavaScriptException exception)
        {
            return ValueTask.FromResult(ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.javascript.error",
                "JavaScript execution failed.",
                exception.Location.Start.Line,
                exception.Location.Start.Column)));
        }
        catch (Exception)
        {
            return ValueTask.FromResult(ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.javascript.failed",
                "JavaScript execution failed safely.")));
        }
    }
}
