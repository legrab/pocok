// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Pocok.Scripting.CSharp;

internal sealed class CSharpWorkerClient
{
    private readonly string? _dotnetHost;
    private readonly string _workerDirectory;

    public CSharpWorkerClient(CSharpScriptEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dotnetHost = ResolveDotNetHost(options.DotNetHostPath);
        string? configuredWorkerDirectory = options.WorkerDirectory ??
            Environment.GetEnvironmentVariable("POCOK_CSHARP_WORKER_DIRECTORY");
        _workerDirectory = Path.GetFullPath(configuredWorkerDirectory ??
            Path.Combine(AppContext.BaseDirectory, "Pocok.Scripting", "CSharpWorker"));
        Availability = ValidateAssets(_dotnetHost, _workerDirectory);
    }

    public (bool Available, string? Code, string? Message) Availability { get; }

    public async Task<CSharpWorkerResponse> SendAsync(
        CSharpWorkerRequest request,
        TimeSpan timeout,
        int outputLimit,
        CancellationToken cancellationToken)
    {
        if (!Availability.Available)
        {
            return new CSharpWorkerResponse(
                false,
                null,
                Availability.Code,
                Availability.Message,
                null,
                null,
                null);
        }

        string workerAssembly = Path.Combine(_workerDirectory, "Pocok.Scripting.CSharp.Worker.dll");
        var start = new ProcessStartInfo(_dotnetHost!)
        {
            WorkingDirectory = _workerDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(workerAssembly);
        start.Environment.Clear();
        CopyEnvironmentVariable(start, "SystemRoot");
        CopyEnvironmentVariable(start, "WINDIR");
        CopyEnvironmentVariable(start, "TEMP");
        CopyEnvironmentVariable(start, "TMP");
        CopyEnvironmentVariable(start, "TMPDIR");
        CopyEnvironmentVariable(start, "HOME");
        CopyEnvironmentVariable(start, "USERPROFILE");
        CopyEnvironmentVariable(start, "DOTNET_ROOT");
        CopyEnvironmentVariable(start, "DOTNET_ROOT_X64");
        CopyEnvironmentVariable(start, "DOTNET_ROOT_X86");
        start.Environment["DOTNET_EnableDiagnostics"] = "0";
        start.Environment["DOTNET_NOLOGO"] = "1";

        using var process = new Process { StartInfo = start };
        process.Start();

        string payload = JsonSerializer.Serialize(request);
        await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            Task<string> stdout = ReadBoundedAsync(
                process.StandardOutput,
                outputLimit,
                timeoutSource.Token);
            Task<string> stderr = ReadBoundedAsync(
                process.StandardError,
                16 * 1024,
                timeoutSource.Token);

            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            string output = await stdout.ConfigureAwait(false);
            _ = await stderr.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return new CSharpWorkerResponse(
                    false,
                    null,
                    "scripting.csharp.worker_failed",
                    "The C# worker failed safely.",
                    null,
                    null,
                    null);
            }

            return JsonSerializer.Deserialize<CSharpWorkerResponse>(output) ??
                new CSharpWorkerResponse(
                    false,
                    null,
                    "scripting.csharp.protocol",
                    "The C# worker returned an empty response.",
                    null,
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
            return new CSharpWorkerResponse(
                false,
                null,
                "scripting.execution.timeout",
                "C# execution exceeded its configured timeout.",
                null,
                null,
                null);
        }
        catch (Exception)
        {
            TryKill(process);
            return new CSharpWorkerResponse(
                false,
                null,
                "scripting.csharp.worker_failed",
                "The C# worker failed safely.",
                null,
                null,
                null);
        }
    }

    private static void CopyEnvironmentVariable(ProcessStartInfo start, string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
            start.Environment[name] = value;
    }

    private static string? ResolveDotNetHost(string? configured)
    {
        string? value = configured ?? Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return !string.IsNullOrWhiteSpace(value) && File.Exists(value)
            ? Path.GetFullPath(value)
            : null;
    }

    private static (bool Available, string? Code, string? Message) ValidateAssets(
        string? dotnetHost,
        string workerDirectory)
    {
        if (dotnetHost is null)
        {
            return (false,
                "scripting.csharp.dotnet_unavailable",
                "Configure DotNetHostPath or DOTNET_HOST_PATH.");
        }

        if (!Directory.Exists(workerDirectory))
        {
            return (false,
                "scripting.csharp.worker_missing",
                "Private C# worker assets are missing.");
        }

        string manifestPath = Path.Combine(workerDirectory, "worker.sha256");
        if (!File.Exists(manifestPath))
        {
            return (false,
                "scripting.csharp.worker_manifest_missing",
                "The C# worker manifest is missing.");
        }

        string directoryPrefix = workerDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        int verifiedFiles = 0;

        foreach (string line in File.ReadLines(manifestPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split("  ", 2, StringSplitOptions.None);
            if (parts.Length != 2 || parts[0].Length != 64)
            {
                return (false,
                    "scripting.csharp.worker_manifest_invalid",
                    "The C# worker manifest is invalid.");
            }

            string relative = parts[1]
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            string path = Path.GetFullPath(Path.Combine(workerDirectory, relative));
            if (!path.StartsWith(directoryPrefix, pathComparison) || !File.Exists(path))
            {
                return (false,
                    "scripting.csharp.worker_asset_missing",
                    "A private C# worker asset is missing.");
            }

            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (!hash.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
            {
                return (false,
                    "scripting.csharp.worker_hash_invalid",
                    "A private C# worker asset failed integrity validation.");
            }

            verifiedFiles++;
        }

        if (verifiedFiles == 0 ||
            !File.Exists(Path.Combine(workerDirectory, "Pocok.Scripting.CSharp.Worker.dll")))
        {
            return (false,
                "scripting.csharp.worker_manifest_invalid",
                "The C# worker manifest contains no executable worker assets.");
        }

        return (true, null, null);
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
            int read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                return builder.ToString();
            if (builder.Length + read > maximumCharacters)
                throw new InvalidDataException("Worker output exceeded its bound.");

            builder.Append(buffer, 0, read);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // Best-effort cleanup after a failure; the original safe result is retained.
        }
    }
}
