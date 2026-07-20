// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;
using Shouldly;

namespace Pocok.Scripting.CSharp.Tests;

[TestFixture]
public sealed class CSharpAdapterTests
{
    [Test]
    public void MissingWorkerIsTruthfullyUnavailable()
    {
        var adapter = new CSharpScriptEngineAdapter(new CSharpScriptEngineOptions
        {
            DotNetHostPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

        adapter.Descriptor.IsAvailable.ShouldBeFalse();
        adapter.Descriptor.UnavailableCode.ShouldBe("scripting.csharp.dotnet_unavailable");
    }

    [Test]
    public async Task ConfiguredWorkerExecutesInChildProcess()
    {
        CSharpScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore();
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<int> result = await runner.ExecuteAsync<int>(
            new ScriptExecutionRequest(ScriptEngineId.CSharp, "test", "21 + 21")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });

        result.IsSuccess.ShouldBeTrue(result.Failure?.Message);
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task InfiniteLoopKillsTheWorkerAtTheConfiguredTimeout()
    {
        CSharpScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore();
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<object?> result = await runner.ExecuteAsync(
            new ScriptExecutionRequest(ScriptEngineId.CSharp, "timeout", "while (true) { }"),
            new ScriptExecutionOptions { Timeout = TimeSpan.FromMilliseconds(500) });

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("scripting.execution.timeout");
    }

    [Test]
    public void InvalidWorkerManifestIsTruthfullyUnavailable()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Pocok.Scripting.CSharp.Worker.dll"), "not-an-assembly");
            File.WriteAllText(Path.Combine(directory, "worker.sha256"), new string('0', 64) +
                "  Pocok.Scripting.CSharp.Worker.dll");
            string fakeHost = Path.Combine(directory, "dotnet");
            File.WriteAllText(fakeHost, string.Empty);

            var adapter = new CSharpScriptEngineAdapter(new CSharpScriptEngineOptions
            {
                DotNetHostPath = fakeHost,
                WorkerDirectory = directory
            });

            adapter.Descriptor.IsAvailable.ShouldBeFalse();
            adapter.Descriptor.UnavailableCode.ShouldBe("scripting.csharp.worker_hash_invalid");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task ReflectionCapabilityIsRejectedBeforeExecution()
    {
        CSharpScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore();

        ScriptValidationResult result = await adapter.Validator.ValidateAsync(
            new ScriptExecutionRequest(
                ScriptEngineId.CSharp,
                "test",
                "typeof(string).Assembly.FullName")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(item =>
            item.Code == "scripting.csharp.unsafe_denied" ||
            item.Code == "scripting.csharp.capability_denied");
    }

    private static CSharpScriptEngineAdapter CreateAvailableAdapterOrIgnore()
    {
        var adapter = new CSharpScriptEngineAdapter();
        if (!adapter.Descriptor.IsAvailable)
            Assert.Ignore(
                adapter.Descriptor.UnavailableMessage
                ?? adapter.Descriptor.UnavailableCode
                ?? "The C# scripting adapter is unavailable.");
        return adapter;
    }
}
