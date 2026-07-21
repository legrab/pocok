// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Pocok.Scripting.Execution;

namespace Pocok.Scripting.Python;

/// <summary>Runs validated Python through configured CPython 3.14 in a child process.</summary>
public sealed class PythonScriptEngineAdapter : IScriptEngineAdapter
{
    private readonly PythonWorkerClient _client;
    private readonly PythonScriptEngineOptions _options;

    /// <summary>Creates an adapter over explicit or environment-provided CPython configuration.</summary>
    public PythonScriptEngineAdapter(PythonScriptEngineOptions? options = null)
    {
        _options = options ?? new PythonScriptEngineOptions();
        _client = new PythonWorkerClient(_options);
        var (available, code, message) = _client.Probe();
        Descriptor = new ScriptEngineDescriptor(
            ScriptEngineId.Python,
            "Python",
            available,
            new ScriptEngineCapabilities(true, true, false, false, false),
            code,
            message);
        Validator = new PythonScriptValidator(_client, _options);
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
            return ScriptResult.Failed<object?>(new ScriptFailure(
                "scripting.binding.unsupported",
                "Function bindings cannot cross the Python worker boundary."));

        PythonWorkerResponse response = await _client.SendAsync(
            "execute",
            script.Request,
            _options.AllowedImports,
            options,
            cancellationToken).ConfigureAwait(false);

        return response.Success
            ? ScriptResult.Success<object?>(response.Result)
            : ScriptResult.Failed<object?>(new ScriptFailure(
                response.Code ?? "scripting.python.failed",
                response.Message ?? "Python execution failed safely.",
                response.Line,
                response.Column));
    }

    private sealed class PythonScriptValidator(
        PythonWorkerClient client,
        PythonScriptEngineOptions options) : IScriptValidator
    {
        public ScriptEngineId EngineId => ScriptEngineId.Python;

        public async ValueTask<ScriptValidationResult> ValidateAsync(
            ScriptExecutionRequest request,
            ScriptExecutionOptions executionOptions,
            CancellationToken cancellationToken = default)
        {
            if (request.Bindings.Any(static item => item.Function is not null))
                return ScriptValidationResult.From(
                [
                    new ScriptValidationDiagnostic(
                        "scripting.binding.unsupported",
                        "Function bindings cannot cross the Python worker boundary.")
                ]);

            PythonWorkerResponse response = await client.SendAsync(
                "validate",
                request,
                options.AllowedImports,
                executionOptions,
                cancellationToken).ConfigureAwait(false);

            return response.Success
                ? ScriptValidationResult.Valid()
                : ScriptValidationResult.From(
                [
                    new ScriptValidationDiagnostic(
                        response.Code ?? "scripting.python.validation",
                        response.Message ?? "Python validation failed safely.",
                        Line: response.Line,
                        Column: response.Column)
                ]);
        }
    }
}

internal sealed class PythonWorkerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string? _pythonExecutable;
    private readonly string _workerPath;

    public PythonWorkerClient(PythonScriptEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var configured = options.PythonExecutable ??
                         Environment.GetEnvironmentVariable("POCOK_PYTHON_EXECUTABLE");
        _pythonExecutable = !string.IsNullOrWhiteSpace(configured) && File.Exists(configured)
            ? Path.GetFullPath(configured)
            : null;
        var configuredWorkerPath = options.WorkerPath ??
                                   Environment.GetEnvironmentVariable("POCOK_PYTHON_WORKER_PATH");
        _workerPath = Path.GetFullPath(configuredWorkerPath ??
                                       Path.Combine(AppContext.BaseDirectory, "Pocok.Scripting", "PythonWorker",
                                           "pocok_worker.py"));
    }

    public (bool Available, string? Code, string? Message) Probe()
    {
        if (_pythonExecutable is null)
            return (false,
                "scripting.python.executable_unavailable",
                "Configure PythonExecutable or POCOK_PYTHON_EXECUTABLE.");

        if (!File.Exists(_workerPath))
            return (false,
                "scripting.python.worker_missing",
                "The private Python worker is missing.");

        try
        {
            ProcessStartInfo start = CreateStartInfo();
            start.ArgumentList.Add("-I");
            start.ArgumentList.Add("-S");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(
                "import sys; print(sys.implementation.name); " +
                "print(f'{sys.version_info.major}.{sys.version_info.minor}')");

            using var process = new Process { StartInfo = start };
            process.Start();
            process.StandardInput.Close();
            if (!process.WaitForExit(3_000))
            {
                process.Kill(true);
                return (false,
                    "scripting.python.probe_timeout",
                    "CPython probing timed out.");
            }

            var lines = process.StandardOutput.ReadToEnd()
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            _ = process.StandardError.ReadToEnd();

            return process.ExitCode == 0 &&
                   lines.Length >= 2 &&
                   lines[0] == "cpython" &&
                   lines[1] == "3.14"
                ? (true, null, null)
                : (false,
                    "scripting.python.version_unsupported",
                    "Pocok.Scripting.Python requires CPython 3.14.x.");
        }
        catch (Exception)
        {
            return (false,
                "scripting.python.probe_failed",
                "CPython probing failed safely.");
        }
    }

    public async Task<PythonWorkerResponse> SendAsync(
        string operation,
        ScriptExecutionRequest request,
        IReadOnlyList<string> allowedImports,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken)
    {
        if (_pythonExecutable is null || !File.Exists(_workerPath))
            return new PythonWorkerResponse(
                false,
                null,
                "scripting.python.unavailable",
                "Python is unavailable.",
                null,
                null);

        var payload = new
        {
            protocolVersion = 1,
            operation,
            source = request.Source,
            expectResult = request.ExpectResult,
            bindings = request.Bindings.ToDictionary(
                static item => item.Name,
                static item => item.Value,
                StringComparer.Ordinal),
            allowedImports
        };

        ProcessStartInfo start = CreateStartInfo();
        start.ArgumentList.Add("-I");
        start.ArgumentList.Add("-S");
        start.ArgumentList.Add(_workerPath);

        using var process = new Process { StartInfo = start };
        process.Start();
        await process.StandardInput.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOptions).AsMemory(),
            cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.Timeout);

        try
        {
            Task<string> stdout = ReadBoundedAsync(
                process.StandardOutput,
                options.MaxOutputBytes * 2,
                timeoutSource.Token);
            Task<string> stderr = ReadBoundedAsync(
                process.StandardError,
                16 * 1024,
                timeoutSource.Token);

            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            var output = await stdout.ConfigureAwait(false);
            _ = await stderr.ConfigureAwait(false);

            if (process.ExitCode != 0)
                return new PythonWorkerResponse(
                    false,
                    null,
                    "scripting.python.worker_failed",
                    "Python execution failed safely.",
                    null,
                    null);

            return JsonSerializer.Deserialize<PythonWorkerResponse>(output, JsonOptions) ??
                   new PythonWorkerResponse(
                       false,
                       null,
                       "scripting.python.protocol",
                       "Python returned an empty response.",
                       null,
                       null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new PythonWorkerResponse(
                false,
                null,
                "scripting.execution.timeout",
                "Python execution exceeded its configured timeout.",
                null,
                null);
        }
        catch (Exception)
        {
            TryKill(process);
            return new PythonWorkerResponse(
                false,
                null,
                "scripting.python.worker_failed",
                "Python execution failed safely.",
                null,
                null);
        }
    }

    private ProcessStartInfo CreateStartInfo()
    {
        var start = new ProcessStartInfo(_pythonExecutable!)
        {
            WorkingDirectory = Path.GetDirectoryName(_workerPath)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        start.Environment.Clear();
        CopyEnvironmentVariable(start, "SystemRoot");
        CopyEnvironmentVariable(start, "WINDIR");
        CopyEnvironmentVariable(start, "TEMP");
        CopyEnvironmentVariable(start, "TMP");
        CopyEnvironmentVariable(start, "TMPDIR");
        CopyEnvironmentVariable(start, "HOME");
        CopyEnvironmentVariable(start, "USERPROFILE");
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        start.Environment["PYTHONUTF8"] = "1";
        return start;
    }

    private static void CopyEnvironmentVariable(ProcessStartInfo start, string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
            start.Environment[name] = value;
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        var builder = new StringBuilder();

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                return builder.ToString();
            if (builder.Length + read > maximumCharacters)
                throw new InvalidDataException("Python worker output exceeded its bound.");

            builder.Append(buffer, 0, read);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch (Exception)
        {
            // Best-effort cleanup after a failure; the original safe result is retained.
        }
    }
}

internal sealed record PythonWorkerResponse(
    bool Success,
    JsonElement? Result,
    string? Code,
    string? Message,
    int? Line,
    int? Column);
